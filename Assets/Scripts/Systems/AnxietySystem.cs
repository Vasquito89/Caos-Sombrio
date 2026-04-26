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

    [Header("Eventos")]
    public UnityEvent<float> onAnxietyChanged;
    public UnityEvent onPanicStart;
    public UnityEvent onPanicEnd;

    private float currentAnxiety;      // Valor actual de ansiedad (0-100)
    private bool isInPanic = false;    // ¿Está el jugador actualmente en pánico?
    private AnxietyLevel currentLevel; // Nivel de ansiedad actual (enum)

    public enum AnxietyLevel { Calm, Nervous, Anxious, Panic }

    public float AnxietyNormalized => currentAnxiety / maxAnxiety;

    public float AnxietyValue => currentAnxiety;

    public float HeartRateBPM => Mathf.Lerp(minBPM, maxBPM, AnxietyNormalized);

    public AnxietyLevel CurrentLevel => currentLevel;

    public bool IsInPanic => isInPanic;


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

        // Notificar el estado inicial
        onAnxietyChanged?.Invoke(AnxietyNormalized);
    }

    private void Update()
    {
        ApplyNaturalDecay();
        UpdateAnxietyLevel();
    }

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

    private void ApplyNaturalDecay()
    {
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

    private AnxietyLevel GetAnxietyLevel()
    {
        if (currentAnxiety >= panicThreshold)   return AnxietyLevel.Panic;
        if (currentAnxiety >= anxiousThreshold) return AnxietyLevel.Anxious;
        if (currentAnxiety >= nervousThreshold) return AnxietyLevel.Nervous;
        return AnxietyLevel.Calm;
    }


    private void OnGUI()
    {
        // Solo visible en el Editor en modo Play para debugging
#if UNITY_EDITOR
        GUI.Label(new Rect(10, 10, 300, 20),
            $"Ansiedad: {currentAnxiety:F1} | BPM: {HeartRateBPM:F0} | Estado: {currentLevel}");
#endif
    }
}
