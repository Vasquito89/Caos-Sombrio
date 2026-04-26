using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class AnxietyHUD : MonoBehaviour
{
    [Header("Display de BPM")]
    [SerializeField] private TextMeshProUGUI bpmValueText;
    [SerializeField] private TextMeshProUGUI bpmLabelText;

    [Header("Barra de Ansiedad")]
    [SerializeField] private Image anxietyBarFill;

    [Header("Vignette (efecto de bordes oscuros)")]
    [SerializeField] private Image vignetteImage;
    [SerializeField, Range(0f, 1f)] private float maxVignetteAlpha = 0.7f;

    [Header("Colores por Nivel de Ansiedad")]
    [SerializeField] private Color calmColor = new Color(0.2f, 0.9f, 0.4f);
    [SerializeField] private Color nervousColor = new Color(0.9f, 0.85f, 0.1f);
    [SerializeField] private Color anxiousColor = new Color(0.95f, 0.4f, 0.1f);
    [SerializeField] private Color panicColor = new Color(0.9f, 0.05f, 0.05f);

    [Header("Animación")]
    [SerializeField] private float uiSmoothSpeed = 5f;
    [SerializeField] private bool pulseBPMText = true;
    [SerializeField] private float pulseAmplitude = 0.15f;

    private float targetAnxietyNorm = 0f;     // Valor objetivo de ansiedad (0-1)
    private float displayedAnxietyNorm = 0f;  // Valor interpolado para UI suave
    private float pulseTimer = 0f;            // Timer para el pulso del BPM
    private Color targetColor;                // Color objetivo según nivel

    private void Start()
    {
        // Conectarse automáticamente al evento del AnxietySystem
        if (AnxietySystem.Instance != null)
        {
            AnxietySystem.Instance.onAnxietyChanged.AddListener(OnAnxietyChanged);
        }
        else
        {
            Debug.LogWarning("[AnxietyHUD] AnxietySystem no encontrado. " +
                             "Asegúrate de que exista un GameManager con AnxietySystem en la escena.");
        }

        // Estado inicial de la vignette: invisible
        if (vignetteImage != null)
        {
            Color c = vignetteImage.color;
            c.a = 0f;
            vignetteImage.color = c;
        }

        // Estado inicial de la barra: vacía
        if (anxietyBarFill != null)
            anxietyBarFill.fillAmount = 0f;

        targetColor = calmColor;
    }

    private void OnDestroy()
    {
        // Desuscribirse del evento al destruir este objeto para evitar memory leaks
        if (AnxietySystem.Instance != null)
            AnxietySystem.Instance.onAnxietyChanged.RemoveListener(OnAnxietyChanged);
    }

    private void Update()
    {
        UpdateDisplayedAnxiety();
        UpdateVignette();
        UpdateBPMText();
        UpdateAnxietyBar();

        if (pulseBPMText)
            UpdateBPMPulse();
    }

    private void OnAnxietyChanged(float normalizedValue)
    {
        targetAnxietyNorm = normalizedValue;

        // Actualizar el color objetivo según el nivel actual
        if (AnxietySystem.Instance != null)
        {
            switch (AnxietySystem.Instance.CurrentLevel)
            {
                case AnxietySystem.AnxietyLevel.Calm:    targetColor = calmColor;    break;
                case AnxietySystem.AnxietyLevel.Nervous: targetColor = nervousColor; break;
                case AnxietySystem.AnxietyLevel.Anxious: targetColor = anxiousColor; break;
                case AnxietySystem.AnxietyLevel.Panic:   targetColor = panicColor;   break;
            }
        }
    }

    private void UpdateDisplayedAnxiety()
    {
        displayedAnxietyNorm = Mathf.Lerp(
            displayedAnxietyNorm,
            targetAnxietyNorm,
            uiSmoothSpeed * Time.deltaTime
        );
    }

    private void UpdateVignette()
    {
        if (vignetteImage == null) return;

        // Calcular el alpha objetivo de la vignette
        float targetAlpha = displayedAnxietyNorm * maxVignetteAlpha;

        // En pánico: añadir un pulso a la vignette (efecto de latido visual)
        if (AnxietySystem.Instance != null && AnxietySystem.Instance.IsInPanic)
        {
            float pulseFactor = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f; // 0 a 1
            targetAlpha += pulseFactor * 0.15f;
        }

        // Interpolar el color de la vignette (también tiñe de rojo en pánico)
        Color vignetteColor = Color.Lerp(Color.black, new Color(0.5f, 0f, 0f), displayedAnxietyNorm);
        vignetteColor.a = Mathf.Clamp01(targetAlpha);
        vignetteImage.color = Color.Lerp(vignetteImage.color, vignetteColor, uiSmoothSpeed * Time.deltaTime);
    }

    private void UpdateBPMText()
    {
        if (bpmValueText == null || AnxietySystem.Instance == null) return;

        int bpm = Mathf.RoundToInt(AnxietySystem.Instance.HeartRateBPM);
        bpmValueText.text = bpm.ToString();

        // Interpolar el color del texto
        bpmValueText.color = Color.Lerp(bpmValueText.color, targetColor, uiSmoothSpeed * Time.deltaTime);
        if (bpmLabelText != null)
            bpmLabelText.color = Color.Lerp(bpmLabelText.color, targetColor, uiSmoothSpeed * Time.deltaTime);
    }

    private void UpdateAnxietyBar()
    {
        if (anxietyBarFill == null) return;

        anxietyBarFill.fillAmount = Mathf.Lerp(
            anxietyBarFill.fillAmount,
            displayedAnxietyNorm,
            uiSmoothSpeed * Time.deltaTime
        );

        // Colorear la barra según el nivel
        anxietyBarFill.color = Color.Lerp(anxietyBarFill.color, targetColor, uiSmoothSpeed * Time.deltaTime);
    }

    private void UpdateBPMPulse()
    {
        if (bpmValueText == null || AnxietySystem.Instance == null) return;

        // Convertir BPM a Hz (latidos por segundo)
        float bps = AnxietySystem.Instance.HeartRateBPM / 60f;

        pulseTimer += Time.deltaTime * bps * Mathf.PI * 2f;

        // Escala del pulso: entre 1 y 1+amplitud, usando rectificación de onda
        float pulse = Mathf.Abs(Mathf.Sin(pulseTimer));
        float scale = 1f + pulse * pulseAmplitude * AnxietySystem.Instance.AnxietyNormalized;

        bpmValueText.transform.localScale = Vector3.one * scale;
    }
}
