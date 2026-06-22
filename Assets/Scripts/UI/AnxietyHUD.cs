using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


/// <summary>
/// Controla todos los elementos visuales del HUD relacionados con la ansiedad del jugador.
/// Escucha los eventos del AnxietySystem y de AlteredPerceptionManager para reaccionar
/// a los estados normales de ansiedad y a la secuencia del Climax Final.
/// </summary>
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
    [SerializeField] private Color calmColor   = new Color(0.2f, 0.9f, 0.4f);
    [SerializeField] private Color nervousColor = new Color(0.9f, 0.85f, 0.1f);
    [SerializeField] private Color anxiousColor = new Color(0.95f, 0.4f, 0.1f);
    [SerializeField] private Color panicColor   = new Color(0.9f, 0.05f, 0.05f);

    [Header("Animacion")]
    [SerializeField] private float uiSmoothSpeed = 5f;
    [SerializeField] private bool  pulseBPMText  = true;
    [SerializeField] private float pulseAmplitude = 0.15f;

    // ─── Climax Final - Falsa Victoria ───────────────────────────────────────

    [Header("Climax Final - Pantalla de Victoria")]
    // Panel raiz de la pantalla de victoria que se muestra durante la falsa victoria.
    // Este panel sera destruido/desactivado cuando se rompa la ilusion.
    [SerializeField] private GameObject victoryScreenPanel;
    // Fuente de audio que reproduce la musica pacifica de victoria.
    // Se detiene cuando se dispara OnFakeVictoryBroken.
    [SerializeField] private AudioSource victoryPeacefulAudio;

    [Header("Climax Final - Efectos de Glitch")]
    // Imagen de overlay que simula el ruido estatico sobre toda la pantalla.
    // Debe ser un Image que cubra todo el Canvas con una textura de ruido.
    [SerializeField] private Image staticNoiseOverlay;
    // Duracion del efecto de parpadeo estatico al romper la victoria.
    [SerializeField] private float staticFlickerDuration = 1.5f;
    // Velocidad del parpadeo del estatico (veces por segundo).
    [SerializeField] private float staticFlickerFrequency = 20f;

    [Header("Climax Final - Bordes de Panico")]
    // Image de overlay que cubre los bordes de la pantalla con rojo pulsante.
    // Diferente a la vignette normal: es un efecto agresivo de panico extremo.
    [SerializeField] private Image panicEdgeOverlay;
    // Velocidad del parpadeo del borde rojo de panico.
    [SerializeField] private float panicEdgePulseSpeed = 8f;
    // Alpha maximo del borde rojo de panico.
    [SerializeField, Range(0f, 1f)] private float maxPanicEdgeAlpha = 0.85f;

    [Header("Climax Final - Aberracion Cromatica")]
    // Referencia al Material del shader de aberracion cromatica aplicado a un RawImage de overlay.
    // Si se usa URP Post-Processing, este campo puede dejarse vacio y manejarse via Volume.
    [SerializeField] private Material chromaticAberrationMaterial;
    // Nombre de la propiedad del shader que controla la intensidad de la aberracion.
    [SerializeField] private string chromaticIntensityProperty = "_ChromaticIntensity";
    // Intensidad maxima de la aberracion cromatica durante el panico final (0-1).
    [SerializeField, Range(0f, 1f)] private float maxChromaticIntensity = 0.9f;


    // ─── Estado interno ───────────────────────────────────────────────────────

    private float targetAnxietyNorm  = 0f;   // Valor objetivo de ansiedad (0-1)
    private float displayedAnxietyNorm = 0f; // Valor interpolado para UI suave
    private float pulseTimer = 0f;           // Timer para el pulso del BPM
    private Color targetColor;               // Color objetivo segun nivel

    // Bandera que indica si estamos en la fase de panico extremo del climax final.
    // Activa los efectos visuales especiales de la HUD.
    private bool isFinalChallengeActive = false;

    // Corrutina del efecto de estatico para poder detenerla si es necesario
    private Coroutine staticFlickerCoroutine;


    // =========================================================================
    //  INICIALIZACION Y SUBSCRIPCIONES
    // =========================================================================

    private void Start()
    {
        // Conectarse al evento de ansiedad del AnxietySystem
        if (AnxietySystem.Instance != null)
        {
            AnxietySystem.Instance.onAnxietyChanged.AddListener(OnAnxietyChanged);
        }
        else
        {
            Debug.LogWarning("[AnxietyHUD] AnxietySystem no encontrado. " +
                             "Asegurate de que exista un GameManager con AnxietySystem en la escena.");
        }

        // Suscribirse a los eventos System.Action del AlteredPerceptionManager
        // para reaccionar a la secuencia del Climax Final sin acoplamiento directo
        AlteredPerceptionManager.OnFakeVictoryStarted  += HandleFakeVictoryStarted;
        AlteredPerceptionManager.OnFakeVictoryBroken   += HandleFakeVictoryBroken;
        AlteredPerceptionManager.OnEscapeObjectiveActivated += HandleEscapeObjectiveActivated;

        // Estado inicial de la vignette: invisible
        if (vignetteImage != null)
        {
            Color c = vignetteImage.color;
            c.a = 0f;
            vignetteImage.color = c;
        }

        // Estado inicial del overlay de panico: invisible
        if (panicEdgeOverlay != null)
        {
            Color c = panicEdgeOverlay.color;
            c.a = 0f;
            panicEdgeOverlay.color = c;
        }

        // Estado inicial del overlay de estatico: invisible
        if (staticNoiseOverlay != null)
            staticNoiseOverlay.gameObject.SetActive(false);

        // Estado inicial de la pantalla de victoria: oculta
        if (victoryScreenPanel != null)
            victoryScreenPanel.SetActive(false);

        // Estado inicial de la barra: vacia
        if (anxietyBarFill != null)
            anxietyBarFill.fillAmount = 0f;

        targetColor = calmColor;
    }

    private void OnDestroy()
    {
        // Desuscribirse de todos los eventos al destruir este objeto
        if (AnxietySystem.Instance != null)
            AnxietySystem.Instance.onAnxietyChanged.RemoveListener(OnAnxietyChanged);

        AlteredPerceptionManager.OnFakeVictoryStarted  -= HandleFakeVictoryStarted;
        AlteredPerceptionManager.OnFakeVictoryBroken   -= HandleFakeVictoryBroken;
        AlteredPerceptionManager.OnEscapeObjectiveActivated -= HandleEscapeObjectiveActivated;
    }


    // =========================================================================
    //  UPDATE - Logica de animacion continua
    // =========================================================================

    private void Update()
    {
        UpdateDisplayedAnxiety();
        UpdateVignette();
        UpdateBPMText();
        UpdateAnxietyBar();

        if (pulseBPMText)
            UpdateBPMPulse();

        // Actualizar el efecto de borde de panico durante el climax final
        if (isFinalChallengeActive)
            UpdatePanicEdgeEffect();
    }


    // =========================================================================
    //  HANDLERS DE EVENTOS DEL ANXIETY SYSTEM
    // =========================================================================

    private void OnAnxietyChanged(float normalizedValue)
    {
        targetAnxietyNorm = normalizedValue;

        // Actualizar el color objetivo segun el nivel actual
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


    // =========================================================================
    //  HANDLERS DE EVENTOS DEL CLIMAX FINAL (AlteredPerceptionManager)
    // =========================================================================

    /// <summary>
    /// Responde al Paso 1 de la secuencia: muestra la pantalla de victoria normal.
    /// El jugador cree que gano. Audio pacifico encendido.
    /// </summary>
    private void HandleFakeVictoryStarted()
    {
        Debug.Log("[AnxietyHUD] Falsa Victoria iniciada. Mostrando pantalla de Victoria.");

        // Mostrar el panel de victoria
        if (victoryScreenPanel != null)
            victoryScreenPanel.SetActive(true);

        // Reproducir la musica pacifica de victoria
        if (victoryPeacefulAudio != null)
        {
            victoryPeacefulAudio.Stop();
            victoryPeacefulAudio.Play();
        }
    }

    /// <summary>
    /// Responde al Paso 3 de la secuencia: destruye/distorsiona la pantalla de victoria
    /// y activa los efectos visuales de panico: estatico, aberracion cromatica y borde rojo.
    /// </summary>
    private void HandleFakeVictoryBroken()
    {
        Debug.Log("[AnxietyHUD] Victoria rota! Aplicando efectos de glitch y panico.");

        // Detener el audio pacifico de victoria
        if (victoryPeacefulAudio != null)
            victoryPeacefulAudio.Stop();

        // Destruir/ocultar la pantalla de victoria con un efecto de distorsion
        // En lugar de solo desactivar, se puede animar con un Animator o via codigo
        if (victoryScreenPanel != null)
            victoryScreenPanel.SetActive(false);

        // Activar el parpadeo de estatico en el Canvas
        if (staticFlickerCoroutine != null)
            StopCoroutine(staticFlickerCoroutine);
        staticFlickerCoroutine = StartCoroutine(StaticFlickerRoutine());

        // Activar aberracion cromatica masiva
        ApplyMaxChromaticAberration();

        // La fase de panico extremo de la HUD comienza aqui
        isFinalChallengeActive = true;
    }

    /// <summary>
    /// Responde al Paso 6: el objetivo cambio a escapar.
    /// Puede actualizar textos de objetivo en la HUD, etc.
    /// </summary>
    private void HandleEscapeObjectiveActivated()
    {
        Debug.Log("[AnxietyHUD] Objetivo actualizado: ESCAPAR DEL EDIFICIO.");
        // Aqui se puede actualizar un texto de objetivo en pantalla,
        // activar un indicador de flecha hacia la salida, etc.
    }


    // =========================================================================
    //  EFECTOS VISUALES DEL CLIMAX FINAL
    // =========================================================================

    /// <summary>
    /// Corrutina que aplica un parpadeo erratico del overlay de ruido estatico
    /// durante la ruptura de la falsa victoria.
    /// </summary>
    private IEnumerator StaticFlickerRoutine()
    {
        if (staticNoiseOverlay == null) yield break;

        staticNoiseOverlay.gameObject.SetActive(true);
        float elapsed = 0f;
        float flickerInterval = 1f / staticFlickerFrequency;

        while (elapsed < staticFlickerDuration)
        {
            // Alternar visibilidad del overlay a alta frecuencia para simular estatico
            staticNoiseOverlay.gameObject.SetActive(!staticNoiseOverlay.gameObject.activeSelf);

            // Variar levemente el alpha para un efecto mas organico
            float randomAlpha = UnityEngine.Random.Range(0.3f, 0.9f);
            Color c = staticNoiseOverlay.color;
            c.a = randomAlpha;
            staticNoiseOverlay.color = c;

            elapsed += flickerInterval;
            yield return new WaitForSeconds(flickerInterval);
        }

        // Al terminar el parpadeo, dejar el overlay desactivado
        staticNoiseOverlay.gameObject.SetActive(false);
        staticFlickerCoroutine = null;
    }

    /// <summary>
    /// Aplica la aberracion cromatica al maximo instantaneamente.
    /// Si el Material fue asignado, modifica la propiedad del shader directamente.
    /// </summary>
    private void ApplyMaxChromaticAberration()
    {
        if (chromaticAberrationMaterial != null &&
            chromaticAberrationMaterial.HasProperty(chromaticIntensityProperty))
        {
            chromaticAberrationMaterial.SetFloat(chromaticIntensityProperty, maxChromaticIntensity);
            Debug.Log($"[AnxietyHUD] Aberracion cromatica aplicada al maximo: {maxChromaticIntensity}");
        }
        else
        {
            // Si no hay material asignado, el efecto puede manejarse via URP Post-Processing Volume
            Debug.Log("[AnxietyHUD] chromaticAberrationMaterial no asignado. Manejar via Volume si se usa URP.");
        }
    }

    /// <summary>
    /// Actualiza el efecto de borde rojo pulsante durante la fase de panico extremo.
    /// Se llama cada frame cuando isFinalChallengeActive es true.
    /// </summary>
    private void UpdatePanicEdgeEffect()
    {
        if (panicEdgeOverlay == null) return;

        // Pulso sinusoidal agresivo para el borde rojo: frecuencia alta, alpha fuerte
        float pulse = (Mathf.Sin(Time.time * panicEdgePulseSpeed) + 1f) * 0.5f;
        float targetAlpha = pulse * maxPanicEdgeAlpha;

        Color edgeColor = panicEdgeOverlay.color;
        edgeColor.a = Mathf.Lerp(edgeColor.a, targetAlpha, Time.deltaTime * 15f);
        panicEdgeOverlay.color = edgeColor;
    }


    // =========================================================================
    //  METODOS DE UPDATE DE LA HUD NORMAL
    // =========================================================================

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

        // En panico: anadir un pulso a la vignette (efecto de latido visual)
        if (AnxietySystem.Instance != null && AnxietySystem.Instance.IsInPanic)
        {
            float pulseFactor = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
            targetAlpha += pulseFactor * 0.15f;
        }

        // Interpolar el color de la vignette (tambien tine de rojo en panico)
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

        // Colorear la barra segun el nivel
        anxietyBarFill.color = Color.Lerp(anxietyBarFill.color, targetColor, uiSmoothSpeed * Time.deltaTime);
    }

    private void UpdateBPMPulse()
    {
        if (bpmValueText == null || AnxietySystem.Instance == null) return;

        // Convertir BPM a Hz (latidos por segundo)
        float bps = AnxietySystem.Instance.HeartRateBPM / 60f;

        pulseTimer += Time.deltaTime * bps * Mathf.PI * 2f;

        // Escala del pulso: entre 1 y 1+amplitud, usando rectificacion de onda
        float pulse = Mathf.Abs(Mathf.Sin(pulseTimer));
        float scale = 1f + pulse * pulseAmplitude * AnxietySystem.Instance.AnxietyNormalized;

        bpmValueText.transform.localScale = Vector3.one * scale;
    }
}