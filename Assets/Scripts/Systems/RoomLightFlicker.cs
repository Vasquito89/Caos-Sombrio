using UnityEngine;


[RequireComponent(typeof(Light))]
public class RoomLightFlicker : MonoBehaviour
{
    [Header("Intensidad")]
    [SerializeField] private float baseIntensity = 0f;
    [SerializeField, Range(0.01f, 2f)] private float minIntensity = 0.3f;

    [Header("Parpadeo")]
    [SerializeField] private float nervousFlickerSpeed = 1.5f;
    [SerializeField] private float anxiousFlickerSpeed = 6f;
    [SerializeField] private float panicFlickerSpeed = 20f;
    [SerializeField, Range(0f, 1f)] private float flickerAmplitude = 0.5f;

    [Header("Cambio de Color")]
    [SerializeField] private bool enableColorShift = true;
    [SerializeField] private Color calmColor = new Color(1f, 0.95f, 0.8f);  // Blanco cálido
    [SerializeField] private Color panicColor = new Color(0.8f, 0.2f, 0.1f); // Rojo oscuro

    [Header("Apagones (solo en Pánico)")]
    [SerializeField] private bool canTurnOff = false;
    [SerializeField, Range(0f, 1f)] private float blackoutChance = 0.08f;
    [SerializeField] private float blackoutMinDuration = 0.1f;
    [SerializeField] private float blackoutMaxDuration = 0.6f;

    [Header("Suavizado")]
    [SerializeField] private float recoverySpeed = 3f;


    private Light roomLight;                          // Referencia al componente Light
    private float resolvedBaseIntensity;              // Intensidad base resuelta (puede venir del Light o del campo)
    private float flickerTimer = 0f;                 // Temporizador del seno de parpadeo
    private float currentFlickerSpeed = 0f;          // Velocidad de parpadeo actual (interpolada)
    private AnxietySystem.AnxietyLevel currentLevel; // Nivel de ansiedad recibido del sistema

    private bool isInBlackout = false;   // ¿La luz está en medio de un apagón?
    private float blackoutTimer = 0f;    // Tiempo restante del apagón actual
    private bool isDisabledBySwitch = false; // ¿El LightSwitch apagó esta luz manualmente?


    private void Awake()
    {
        roomLight = GetComponent<Light>();

        
        if (baseIntensity <= 0f)
            resolvedBaseIntensity = roomLight.intensity;
        else
            resolvedBaseIntensity = baseIntensity;

        // Guardar color base del Light si no se usa enableColorShift
        if (enableColorShift)
            roomLight.color = calmColor;

        currentLevel = AnxietySystem.AnxietyLevel.Calm;
    }

    private void OnEnable()
    {
        // Suscribirse al evento de cambio de ansiedad cuando el script se activa
        if (AnxietySystem.Instance != null)
            AnxietySystem.Instance.onAnxietyChanged.AddListener(OnAnxietyChanged);
        else
            // Si el AnxietySystem no existe todavía, reintentamos en Start()
            Debug.LogWarning($"[RoomLightFlicker] '{gameObject.name}': AnxietySystem no encontrado en OnEnable. " +
                             "Asegúrate de que el GameManager esté en la escena antes que este objeto.");
    }

    private void Start()
    {
        // Segundo intento de suscripción (por si el AnxietySystem cargó después)
        if (AnxietySystem.Instance != null)
        {
            // Remover primero para evitar doble suscripción
            AnxietySystem.Instance.onAnxietyChanged.RemoveListener(OnAnxietyChanged);
            AnxietySystem.Instance.onAnxietyChanged.AddListener(OnAnxietyChanged);

            // Sincronizar el estado inicial con el AnxietySystem
            currentLevel = AnxietySystem.Instance.CurrentLevel;
        }
    }

    private void OnDisable()
    {
        // Desuscribirse al desactivar para evitar memory leaks
        if (AnxietySystem.Instance != null)
            AnxietySystem.Instance.onAnxietyChanged.RemoveListener(OnAnxietyChanged);
    }

    private void Update()
    {
        // Si el interruptor apagó esta luz, no hacer nada
        if (isDisabledBySwitch) return;

        // Si está en apagón, manejar el temporizador
        if (isInBlackout)
        {
            HandleBlackout();
            return; // No ejecutar parpadeo durante el apagón
        }

        switch (currentLevel)
        {
            case AnxietySystem.AnxietyLevel.Calm:
                HandleCalm();
                break;

            case AnxietySystem.AnxietyLevel.Nervous:
                HandleFlicker(nervousFlickerSpeed, flickerAmplitude * 0.3f);
                break;

            case AnxietySystem.AnxietyLevel.Anxious:
                HandleFlicker(anxiousFlickerSpeed, flickerAmplitude * 0.7f);
                TryTriggerBlackout(); // En estado Anxious, baja probabilidad de apagón
                break;

            case AnxietySystem.AnxietyLevel.Panic:
                HandleFlicker(panicFlickerSpeed, flickerAmplitude);
                TryTriggerBlackout(); // En Panic, mayor probabilidad
                break;
        }

        // Actualizar el color si está habilitado
        if (enableColorShift)
            UpdateColor();
    }

    private void OnAnxietyChanged(float normalizedValue)
    {
        if (AnxietySystem.Instance != null)
            currentLevel = AnxietySystem.Instance.CurrentLevel;
    }

    private void HandleCalm()
    {
        // Recuperar la intensidad base suavemente
        roomLight.intensity = Mathf.Lerp(
            roomLight.intensity,
            resolvedBaseIntensity,
            recoverySpeed * Time.deltaTime
        );

        // Resetear el timer de parpadeo para evitar saltos al volver a Nervous
        flickerTimer = 0f;
    }

    private void HandleFlicker(float speed, float amplitude)
    {
        // Avanzar el temporizador
        flickerTimer += Time.deltaTime * speed;

        // Combinar dos senos con frecuencias distintas:
        // El primero da el ritmo base, el segundo agrega irregularidad
        float flicker = Mathf.Sin(flickerTimer) * Mathf.Sin(flickerTimer * 1.7f + 0.5f);

        // Mapear de [-1, 1] a [0, 1] para que nunca haya valores negativos
        float flickerNorm = (flicker + 1f) * 0.5f;

        // Calcular la variación real de intensidad
        float intensityRange  = resolvedBaseIntensity - minIntensity;
        float targetIntensity = resolvedBaseIntensity - (flickerNorm * intensityRange * amplitude);

        roomLight.intensity = Mathf.Max(targetIntensity, minIntensity);
    }

    private void TryTriggerBlackout()
    {
        if (!canTurnOff || isInBlackout) return;

        // Factor de escala: Panic tiene el doble de probabilidad que Anxious
        float probabilityScale = (currentLevel == AnxietySystem.AnxietyLevel.Panic) ? 1f : 0.5f;

        // blackoutChance es por segundo, multiplicar por deltaTime para normalizar
        if (Random.value < blackoutChance * probabilityScale * Time.deltaTime)
        {
            StartBlackout();
        }
    }

    private void StartBlackout()
    {
        isInBlackout = true;
        blackoutTimer = Random.Range(blackoutMinDuration, blackoutMaxDuration);
        roomLight.enabled = false;

        Debug.Log($"[RoomLightFlicker] '{gameObject.name}' — Apagón iniciado ({blackoutTimer:F2}s).");
    }

    private void HandleBlackout()
    {
        blackoutTimer -= Time.deltaTime;

        if (blackoutTimer <= 0f)
        {
            isInBlackout    = false;
            roomLight.enabled = true;

            // Restaurar a la intensidad base para evitar un flash brusco
            roomLight.intensity = resolvedBaseIntensity;

            Debug.Log($"[RoomLightFlicker] '{gameObject.name}' — Luz restaurada.");
        }
    }

    private void UpdateColor()
    {
        if (AnxietySystem.Instance == null) return;

        float t = AnxietySystem.Instance.AnxietyNormalized;
        roomLight.color = Color.Lerp(calmColor, panicColor, t);
    }

    public void NotifyLightSwitchState(bool lightIsOn)
    {
        isDisabledBySwitch = !lightIsOn;

        // Si el switch encendió la luz, restaurar la intensidad base
        if (lightIsOn)
        {
            roomLight.enabled   = true;
            roomLight.intensity = resolvedBaseIntensity;
            isInBlackout        = false;
        }
    }

    public void ForceBlackout(float duration)
    {
        if (!roomLight.enabled && isInBlackout) return; // Ya está en apagón

        isInBlackout    = true;
        blackoutTimer   = duration;
        roomLight.enabled = false;

        Debug.Log($"[RoomLightFlicker] '{gameObject.name}' — Apagón forzado ({duration:F2}s).");
    }

    public void StabilizeLight()
    {
        // Estabilizar la luz al 100% de intensidad sin parpadeo
        isDisabledBySwitch = true;
        isInBlackout = false;

        if (roomLight != null)
        {
            roomLight.enabled = true;
            roomLight.intensity = resolvedBaseIntensity;
        }

        Debug.Log($"[RoomLightFlicker] '{gameObject.name}' — Luz estabilizada al 100%.");
    }

    private void OnDrawGizmosSelected()
    {
        // Dibujar un ícono de "nivel de ansiedad" en la luz para identificarla en la escena
        Gizmos.color = canTurnOff ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        // Línea hacia abajo indicando el rango de la luz
        Light l = GetComponent<Light>();
        if (l != null)
        {
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, l.range * 0.1f);
        }
    }
}
