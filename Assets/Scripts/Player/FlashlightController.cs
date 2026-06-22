using UnityEngine;

public class FlashlightController : MonoBehaviour
{

    [Header("Componente de Luz")]
    [SerializeField] private Light flashlightLight;
    [SerializeField] private float baseIntensity = 2.5f;

    [Header("Sistema de Batería")]
    [SerializeField, Range(0f, 100f)] private float batteryLevel = 100f;
    [SerializeField] private float batteryConsumptionRate = 5f; // % por segundo

    [Header("Raycast de Racionalización")]
    [SerializeField] private float rationalizationRange = 8f;
    [SerializeField] private LayerMask stimulusLayers;

    [Header("Parpadeo por Ansiedad")]
    [SerializeField] private bool flickerWithAnxiety = true;
    [SerializeField] private float minFlickerSpeed = 2f;
    [SerializeField] private float maxFlickerSpeed = 25f;
    [SerializeField] private float flickerAmplitude = 0.8f;

    [Header("Reducción de Ansiedad pasiva")]
    [SerializeField] private float passiveAnxietyReduction = 1.5f;


    private bool isOn = false;           // Estado actual de la linterna
    private float flickerTimer = 0f;     // Temporizador para el parpadeo (basado en seno)
    private Camera playerCamera;         // Referencia a la cámara para el Raycast

    // — Evento de cambio de batería —
    public System.Action<float> onBatteryChanged;

    private void Start()
    {
        // Auto-detectar la cámara
        playerCamera = Camera.main;

        // La linterna empieza apagada: el jugador debe recogerla primero
        if (flashlightLight != null)
            flashlightLight.enabled = false;
        else
            Debug.LogError("[FlashlightController] No se asignó el componente Light en el Inspector.");
    }


    private void Update()
    {
        HandleToggleInput();

        if (isOn)
        {
            // Consumir batería mientras está encendida
            ConsumeBattery();

            HandleRationalizationRaycast();
            HandlePassiveAnxietyReduction();

            if (flickerWithAnxiety)
                HandleFlicker();
        }
    }

    private void ConsumeBattery()
    {
        batteryLevel -= batteryConsumptionRate * Time.deltaTime;

        if (batteryLevel <= 0f)
        {
            batteryLevel = 0f;
            TurnOff();
            Debug.Log("[FlashlightController] ¡Batería agotada!");
        }

        // Notificar cambio de batería
        onBatteryChanged?.Invoke(batteryLevel);
    }

    private void HandleToggleInput()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isOn = !isOn;

            if (flashlightLight != null)
                flashlightLight.enabled = isOn;

            Debug.Log($"[FlashlightController] Linterna {(isOn ? "encendida" : "apagada")}.");
        }
    }

    private void HandleRationalizationRaycast()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, rationalizationRange, stimulusLayers))
        {
            AnxietyStimulus stimulus = hit.collider.GetComponent<AnxietyStimulus>();
            if (stimulus != null)
            {
                // Llamar a Rationalize() cada frame que la linterna apunte al estímulo
                // AnxietyStimulus procesa esto como reducción de ansiedad por segundo
                stimulus.Rationalize();
            }
        }
    }

    private void HandlePassiveAnxietyReduction()
    {
        if (AnxietySystem.Instance != null)
            AnxietySystem.Instance.ModifyAnxiety(-passiveAnxietyReduction * Time.deltaTime);
    }

    private void HandleFlicker()
    {
        if (flashlightLight == null || AnxietySystem.Instance == null) return;

        float anxietyNorm = AnxietySystem.Instance.AnxietyNormalized;

        // Escalar la velocidad de parpadeo según la ansiedad
        float currentFlickerSpeed = Mathf.Lerp(minFlickerSpeed, maxFlickerSpeed, anxietyNorm);

        // Avanzar el temporizador del parpadeo
        flickerTimer += Time.deltaTime * currentFlickerSpeed;

        // Usar una combinación de senos para un parpadeo orgánico (no perfecto)
        float flicker = Mathf.Sin(flickerTimer) * Mathf.Sin(flickerTimer * 2.7f);

        // Escalar la amplitud del parpadeo con la ansiedad: a más pánico, más parpadeo
        float scaledAmplitude = flickerAmplitude * anxietyNorm;

        // Aplicar la intensidad final
        flashlightLight.intensity = baseIntensity + flicker * scaledAmplitude;

        // Asegurarse de que nunca quede en negativo
        flashlightLight.intensity = Mathf.Max(0f, flashlightLight.intensity);
    }


    public void TurnOn()
    {
        isOn = true;
        if (flashlightLight != null)
            flashlightLight.enabled = true;
    }

    public void TurnOff()
    {
        isOn = false;
        if (flashlightLight != null)
            flashlightLight.enabled = false;
    }

    public bool IsOn => isOn;

    public float BatteryLevel => batteryLevel;

    public void ChargeBattery(float amount)
    {
        batteryLevel = Mathf.Clamp(batteryLevel + amount, 0f, 100f);
        onBatteryChanged?.Invoke(batteryLevel);
        Debug.Log($"[FlashlightController] Batería recargada. Nivel actual: {batteryLevel:F1}%");
    }
}
