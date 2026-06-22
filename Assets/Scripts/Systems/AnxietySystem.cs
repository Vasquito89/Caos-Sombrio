using System;
using UnityEngine;
using UnityEngine.Events;


public class AnxietySystem : MonoBehaviour
{
    public static AnxietySystem Instance { get; private set; }


    [Header("Valores de Ansiedad")]
    [SerializeField, Range(0f, 100f)] private float startingAnxiety = 10f;
    [SerializeField] private float naturalDecayRate = 2f;
    [SerializeField, Range(0f, 30f)] private float minAnxiety = 5f;
    [SerializeField, Range(70f, 100f)] private float maxAnxiety = 100f;

    [Header("Mapeo a BPM")]
    [SerializeField] private float minBPM = 60f;
    [SerializeField] private float maxBPM = 180f;

    [Header("Umbrales de Estado")]
    [SerializeField, Range(10f, 50f)] private float nervousThreshold = 30f;
    [SerializeField, Range(40f, 75f)] private float anxiousThreshold = 60f;
    [SerializeField, Range(70f, 100f)] private float panicThreshold = 85f;

    [Header("Parámetros de Desmayo")]
    [SerializeField] private float preCollapseGracePeriod = 4f;
    [SerializeField] private float ragdollDurationMin = 3f;
    [SerializeField] private float ragdollDurationMax = 5f;
    [SerializeField] private float recoveredAnxietyFraction = 0.33f; // Un tercio del máximo
    [SerializeField] private float immunityCooldown = 5f;

    [Header("Desafío Final - Modo Escape")]
    // Multiplicador que se aplica a la ganancia de ansiedad por segundo durante el clímax final
    [SerializeField] private float finalChallengePanicMultiplier = 5f;

    [Header("Eventos UnityEvent (Inspector)")]
    public UnityEvent<float> onAnxietyChanged;
    public UnityEvent onPanicStart;
    public UnityEvent onPanicEnd;
    public UnityEvent<float> onPreCollapseTick;
    public UnityEvent onFaintStarted;
    public UnityEvent onFaintEnded;
    public UnityEvent onGameOver;

    // --- Eventos System.Action para desacoplamiento total con otros sistemas ---
    // Permite suscripción directa sin necesidad de pasar por el Inspector

    /// <summary>
    /// Disparado cuando el juego entra en la fase de Desafío Final (escape).
    /// Los suscriptores deben activar comportamientos de pánico extremo.
    /// </summary>
    public static event Action OnFinalChallengeStarted;

    /// <summary>
    /// Disparado en cada frame cuando se acumula el multiplicador de pánico final.
    /// Útil para que la HUD refleje la ganancia acelerada de ansiedad.
    /// </summary>
    public static event Action<float> OnFinalChallengeTick;


    private float currentAnxiety;      // Valor actual de ansiedad (0-100)
    private bool isInPanic = false;    // ¿Está el jugador actualmente en pánico?
    private AnxietyLevel currentLevel; // Nivel de ansiedad actual (enum)

    // --- Parámetros de Desmayo ---
    private int faintingCount = 0;     // Contador de desmayos (máx 3)
    private float collapseTimer = 0f;  // Temporizador para el período de gracia
    private bool isPreCollapse = false; // ¿Está en la ventana de pre-desmayo?
    private float immunityTimer = 0f;  // Temporizador de inmunidad post-desmayo

    // --- Estado del Desafío Final ---
    // Bandera que indica si el jugador está en la fase de escape del clímax final
    private bool isInFinalChallenge = false;

    public enum AnxietyLevel { Calm, Nervous, Anxious, Panic }

    // --- Propiedades públicas de solo lectura ---
    public float AnxietyNormalized => currentAnxiety / maxAnxiety;
    public float AnxietyValue      => currentAnxiety;
    public float HeartRateBPM      => Mathf.Lerp(minBPM, maxBPM, AnxietyNormalized);
    public AnxietyLevel CurrentLevel => currentLevel;
    public bool IsInPanic          => isInPanic;
    public bool IsPreCollapse      => isPreCollapse;
    public int FaintingCount       => faintingCount;
    public bool IsInFinalChallenge => isInFinalChallenge;


    private void Awake()
    {
        // Implementación del Singleton: solo puede existir una instancia
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[AnxietySystem] Ya existe una instancia. Destruyendo duplicado.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        currentAnxiety = Mathf.Clamp(startingAnxiety, minAnxiety, maxAnxiety);
        currentLevel   = GetAnxietyLevel();
        faintingCount  = 0;
        isPreCollapse  = false;
        collapseTimer  = 0f;

        // Notificar el estado inicial
        onAnxietyChanged?.Invoke(AnxietyNormalized);
    }

    private void Update()
    {
        // Actualizar temporizador de inmunidad post-desmayo
        if (immunityTimer > 0f)
            immunityTimer -= Time.deltaTime;

        // Durante el Desafío Final, aplicar presión de pánico constante
        if (isInFinalChallenge)
            ApplyFinalChallengePanic();

        ApplyNaturalDecay();
        UpdateAnxietyLevel();
        UpdatePreCollapseState();
    }

    // =========================================================================
    //  MÉTODOS PÚBLICOS
    // =========================================================================

    public void ModifyAnxiety(float delta)
    {
        currentAnxiety = Mathf.Clamp(currentAnxiety + delta, minAnxiety, maxAnxiety);
        onAnxietyChanged?.Invoke(AnxietyNormalized);
    }

    public void SetAnxiety(float value)
    {
        currentAnxiety = Mathf.Clamp(value, minAnxiety, maxAnxiety);
        onAnxietyChanged?.Invoke(AnxietyNormalized);
    }

    /// <summary>
    /// Activa la fase del Desafío Final. Fuerza el pánico extremo de forma instantánea
    /// multiplicando la ganancia de ansiedad por el modificador configurado.
    /// Invocado desde AlteredPerceptionManager en el Paso 5 de la secuencia.
    /// </summary>
    public void ActivateFinalChallenge()
    {
        if (isInFinalChallenge) return; // Evitar activaciones duplicadas

        isInFinalChallenge = true;

        // Forzar ansiedad al máximo de forma instantánea para iniciar el pánico visual
        SetAnxiety(maxAnxiety);

        Debug.Log("[AnxietySystem] ¡DESAFÍO FINAL ACTIVADO! Pánico extremo en curso.");

        // Notificar a todos los suscriptores (HUD, efectos de cámara, IA)
        OnFinalChallengeStarted?.Invoke();
    }

    public bool IsImmune()
    {
        return immunityTimer > 0f;
    }

    // =========================================================================
    //  LÓGICA INTERNA
    // =========================================================================

    /// <summary>
    /// Aplica la presión de pánico continua durante la fase de escape.
    /// La ansiedad sube permanentemente, contrarrestando el decaimiento natural.
    /// </summary>
    private void ApplyFinalChallengePanic()
    {
        // La ganancia de ansiedad por segundo es el multiplicador aplicado al decaimiento natural
        float panicGain = naturalDecayRate * finalChallengePanicMultiplier * Time.deltaTime;
        currentAnxiety  = Mathf.Clamp(currentAnxiety + panicGain, minAnxiety, maxAnxiety);
        onAnxietyChanged?.Invoke(AnxietyNormalized);

        // Notificar el tick del desafío final para efectos de HUD
        OnFinalChallengeTick?.Invoke(AnxietyNormalized);
    }

    private void ApplyNaturalDecay()
    {
        // El decaimiento natural se suspende durante el desafío final para que la ansiedad no baje
        if (isInFinalChallenge) return;

        if (currentAnxiety > minAnxiety)
        {
            currentAnxiety -= naturalDecayRate * Time.deltaTime;
            currentAnxiety  = Mathf.Max(currentAnxiety, minAnxiety);
            onAnxietyChanged?.Invoke(AnxietyNormalized);
        }
    }

    private void UpdateAnxietyLevel()
    {
        AnxietyLevel newLevel = GetAnxietyLevel();

        if (newLevel != currentLevel)
        {
            // Detectar transición hacia/desde el pánico
            if (newLevel == AnxietyLevel.Panic && !isInPanic)
            {
                isInPanic = true;
                onPanicStart?.Invoke();
                Debug.Log("[AnxietySystem] ¡PÁNICO activado!");
            }
            else if (newLevel != AnxietyLevel.Panic && isInPanic)
            {
                isInPanic = false;
                onPanicEnd?.Invoke();
                Debug.Log("[AnxietySystem] Pánico disminuido.");
            }

            currentLevel = newLevel;
            Debug.Log($"[AnxietySystem] Nivel de ansiedad: {currentLevel} | BPM: {HeartRateBPM:F0}");
        }
    }

    private void UpdatePreCollapseState()
    {
        // Si la ansiedad está en el máximo (Nivel 3 / Pánico)
        if (currentAnxiety >= panicThreshold)
        {
            if (!isPreCollapse)
            {
                isPreCollapse = true;
                collapseTimer = 0f;
                Debug.Log("[AnxietySystem] Iniciando ventana de pre-colapso de 4 segundos...");
            }

            collapseTimer += Time.deltaTime;

            // Disparar evento cada frame con progreso (0 a 1)
            float progress = collapseTimer / preCollapseGracePeriod;
            onPreCollapseTick?.Invoke(progress);

            // Si han pasado 4 segundos, desmayarse
            // (La condición de ansiedad baja se verifica en el else de abajo)
            if (collapseTimer >= preCollapseGracePeriod)
            {
                Faint();
            }
        }
        else if (isPreCollapse)
        {
            // La ansiedad bajó del máximo mientras estábamos en pre-colapso
            isPreCollapse = false;
            collapseTimer = 0f;
            Debug.Log("[AnxietySystem] Pre-colapso cancelado: ansiedad reducida.");
        }
    }

    private AnxietyLevel GetAnxietyLevel()
    {
        if (currentAnxiety >= panicThreshold)   return AnxietyLevel.Panic;
        if (currentAnxiety >= anxiousThreshold) return AnxietyLevel.Anxious;
        if (currentAnxiety >= nervousThreshold) return AnxietyLevel.Nervous;
        return AnxietyLevel.Calm;
    }

    private void Faint()
    {
        // Resetear el temporizador y la bandera de pre-colapso
        isPreCollapse = false;
        collapseTimer = 0f;

        faintingCount++;
        Debug.Log($"[AnxietySystem] ¡DESMAYO #{faintingCount}!");

        if (faintingCount < 3)
        {
            // Iniciar la corrutina de Ragdoll
            StartCoroutine(FaintRagdollRoutine());
        }
        else
        {
            // Tercer desmayo: Game Over definitivo (también aplica durante el escape)
            Debug.Log("[AnxietySystem] ¡GAME OVER! El jugador se ha desmayado 3 veces.");
            onGameOver?.Invoke();
        }
    }

    private System.Collections.IEnumerator FaintRagdollRoutine()
    {
        // 1. Disparar evento de inicio del desmayo
        onFaintStarted?.Invoke();

        // 2. Esperar el siguiente frame para permitir que los listeners se preparen
        yield return null;

        // 3. Esperar el tiempo de ragdoll (aleatorio entre min y max)
        float ragdollDuration = UnityEngine.Random.Range(ragdollDurationMin, ragdollDurationMax);
        yield return new WaitForSeconds(ragdollDuration);

        // 4. Restablecer la ansiedad a un tercio del máximo (si no estamos en el desafío final)
        if (!isInFinalChallenge)
        {
            float recoveredAnxiety = maxAnxiety * recoveredAnxietyFraction;
            SetAnxiety(recoveredAnxiety);
        }

        // 5. Establecer cooldown de inmunidad
        immunityTimer = immunityCooldown;

        // 6. Disparar evento de fin del desmayo
        onFaintEnded?.Invoke();

        Debug.Log($"[AnxietySystem] Recuperación completada. Inmunidad por {immunityCooldown}s.");
    }


    private void OnGUI()
    {
        // Solo visible en el Editor en modo Play para debugging
#if UNITY_EDITOR
        GUI.Label(new Rect(10, 10, 400, 20),
            $"Ansiedad: {currentAnxiety:F1} | BPM: {HeartRateBPM:F0} | Estado: {currentLevel} | FinalChallenge: {isInFinalChallenge}");
#endif
    }
}
