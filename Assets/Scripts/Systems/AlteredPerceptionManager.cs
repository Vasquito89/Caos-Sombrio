using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class AlteredPerceptionManager : MonoBehaviour
{
    
    public static event Action OnFakeVictoryStarted;  
    public static event Action OnFakeVictoryBroken;
    public static event Action OnEscapeObjectiveActivated;


    // =========================================================================
    //  PARAMETROS SERIALIZADOS
    // =========================================================================

    [Header("Pre-Collapse Effects")]
    [SerializeField, Range(0f, 1f)] private float maxVignetteIntensity = 0.8f;
    [SerializeField, Range(0f, 1f)] private float maxLensDistortion = 0.5f;
    [SerializeField] private float lensDistortionErraticSpeed = 10f;

    [Header("Post-Faint Disorientation")]
    [SerializeField, Range(0f, 1f)] private float maxChromaticAberration = 1f;
    [SerializeField, Range(0f, 1f)] private float maxBlurAmount = 10f;
    [SerializeField] private float disorientationRecoveryDuration = 4f;

    [Header("Camera Reference")]
    [SerializeField] private Camera playerCamera;

    [Header("Fake Victory - Timing")]
    [SerializeField] private float fakeVictoryHoldDuration = 2.0f;

    [Header("Fake Victory - UI Canvases")]
    // Canvas o panel intermedio de Falsa Victoria
    [SerializeField] private GameObject fakeVictoryCanvas;
    // Canvas o panel final de Victoria Real
    [SerializeField] private GameObject realVictoryCanvas;
    [SerializeField] private Button realVictoryContinueButton;


    [Header("Fake Victory - Building Lights")]
    [SerializeField] private GameObject buildingLightsParent;

    [Header("Fake Victory - Giant Shadow")]
    [SerializeField] private GameObject giantShadowPrefab;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float shadowSpawnDistanceBehind = 2.5f;
    // Prefab de humo para la aparicion de la sombra gigante
    [SerializeField] private GameObject giantShadowSmokePrefab;

    [Header("Fake Victory - Audio")]
    [SerializeField] private AudioSource climaxAudioSource;
    [SerializeField] private AudioClip shadowVoiceSFX;
    [SerializeField] private AudioClip glitchNoiseSFX;

    [Header("Fake Victory - Exit Trigger")]
    [SerializeField] private ExitTrigger mainExitTrigger;


    // =========================================================================
    //  ESTADO INTERNO
    // =========================================================================

    private float lensDistortionTimer = 0f;
    private bool isRecoveringFromFaint = false;
    private float recoveryTimer = 0f;

    private bool fakeVictoryTriggered = false;
    private bool isInFinalChallenge = false;
    private GameObject spawnedShadowInstance = null;
    private bool isGameOverTriggered = false;

    
    public bool FakeVictoryCompleted { get; private set; } = false;


    // =========================================================================
    //  INICIALIZACION
    // =========================================================================

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
        {
            Debug.LogError("[AlteredPerceptionManager] No se encontro Main Camera.");
            return;
        }

        // Suscribirse a los eventos del AnxietySystem
        if (AnxietySystem.Instance != null)
        {
            AnxietySystem.Instance.onPreCollapseTick.AddListener(OnPreCollapseTick);
            AnxietySystem.Instance.onFaintEnded.AddListener(OnFaintEnded);
        }

        // Asegurarse de que los canvases de victoria comiencen desactivados
        if (fakeVictoryCanvas != null) fakeVictoryCanvas.SetActive(false);
        if (realVictoryCanvas != null) realVictoryCanvas.SetActive(false);

        // Suscribirse al evento del trigger de escape
        ExitTrigger.OnPlayerEscaped += HandlePlayerEscaped;

        Debug.Log("[AlteredPerceptionManager] Inicializado. Esperando condiciones de victoria...");

        realVictoryContinueButton.onClick.AddListener(() =>{SceneManager.LoadScene("MenuPrincipal");});
    }

    private void OnDestroy()
    {
        if (AnxietySystem.Instance != null)
        {
            AnxietySystem.Instance.onPreCollapseTick.RemoveListener(OnPreCollapseTick);
            AnxietySystem.Instance.onFaintEnded.RemoveListener(OnFaintEnded);
        }

        ExitTrigger.OnPlayerEscaped -= HandlePlayerEscaped;
    }

    private void Update()
    {
        // Manejar la recuperacion de desorientacion
        if (isRecoveringFromFaint)
        {
            recoveryTimer += Time.deltaTime;
            if (recoveryTimer >= disorientationRecoveryDuration)
            {
                isRecoveringFromFaint = false;
            }
            else
            {
                float recoveryProgress = recoveryTimer / disorientationRecoveryDuration;
                ApplyDisorientationEffects(1f - recoveryProgress);
            }
        }

        // Verificar si la sombra gigante atrapa al jugador durante la fase de escape
        if (isInFinalChallenge && spawnedShadowInstance != null && playerTransform != null && !isGameOverTriggered)
        {
            float distance = Vector3.Distance(spawnedShadowInstance.transform.position, playerTransform.position);
            if (distance < 1.6f)
            {
                CatchPlayer();
            }
        }
    }


    // =========================================================================
    //  PUNTO DE ENTRADA PUBLICO
    // =========================================================================

    
    public void VerifyVictoryCondition()
    {
        if (fakeVictoryTriggered) return;

        fakeVictoryTriggered = true;
        StartCoroutine(FakeVictorySequenceRoutine());
    }


    // =========================================================================
    //  CORRUTINA PRINCIPAL - Secuencia del Climax Final
    // =========================================================================

    private IEnumerator FakeVictorySequenceRoutine()
    {
        // PASO 1: Mostrar la pantalla de Victoria falsa
        Debug.Log("[AlteredPerceptionManager] PASO 1 - Activando Canvas Falso de Victoria.");
        if (fakeVictoryCanvas != null)
        {
            fakeVictoryCanvas.SetActive(true);
        }
        OnFakeVictoryStarted?.Invoke();

        // PASO 2: Esperar exactamente 2 segundos
        yield return new WaitForSeconds(fakeVictoryHoldDuration);

        // PASO 3: Romper la ilusion con glitch
        Debug.Log("[AlteredPerceptionManager] PASO 3 - Desactivando Canvas Falso y aplicando Glitch.");
        if (fakeVictoryCanvas != null)
        {
            fakeVictoryCanvas.SetActive(false);
        }
        OnFakeVictoryBroken?.Invoke();

        if (climaxAudioSource != null && glitchNoiseSFX != null)
        {
            climaxAudioSource.Stop();
            climaxAudioSource.PlayOneShot(glitchNoiseSFX);
        }

        yield return new WaitForSeconds(0.15f);

        // PASO 4: El Apagon
        if (buildingLightsParent != null)
            buildingLightsParent.SetActive(false);

        // PASO 5: El Climax - Panico extremo y spawn de la Sombra Gigante
        isInFinalChallenge = true;
        if (AnxietySystem.Instance != null)
        {
            AnxietySystem.Instance.ActivateFinalChallenge();
        }

        if (climaxAudioSource != null && shadowVoiceSFX != null)
        {
            climaxAudioSource.PlayOneShot(shadowVoiceSFX);
        }

        SpawnGiantShadowBehindPlayer();

        // PASO 6: Activar el trigger de escape y marcar la falsa victoria como completada
        if (mainExitTrigger != null)
        {
            mainExitTrigger.Activate();
        }

        // Gate: solo después de activar el exit trigger, la victoria real queda habilitada
        FakeVictoryCompleted = true;
        Debug.Log("[AlteredPerceptionManager] PASO 6 - FakeVictoryCompleted = true. ExitTrigger activo.");

        OnEscapeObjectiveActivated?.Invoke();
    }


    // =========================================================================
    //  METODOS AUXILIARES
    // =========================================================================

    private void SpawnGiantShadowBehindPlayer()
    {
        if (giantShadowPrefab == null) return;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }

        if (playerTransform == null) return;

        Vector3 behindOffset = -playerTransform.forward * shadowSpawnDistanceBehind;
        Vector3 spawnPosition = playerTransform.position + behindOffset;
        Quaternion spawnRotation = Quaternion.LookRotation(playerTransform.forward, Vector3.up);

        spawnedShadowInstance = Instantiate(giantShadowPrefab, spawnPosition, spawnRotation);

        // Activar la explosion de humo en la aparicion de la Sombra Gigante
        ShadowSmokeEffect.PlayBurstAtPosition(spawnPosition, giantShadowSmokePrefab);
    }

    
    private void CatchPlayer()
    {
        isGameOverTriggered = true;
        Debug.Log("[AlteredPerceptionManager] El jugador fue atrapado por la sombra gigante.");
        
        if (AnxietySystem.Instance != null)
        {
            // Invocar onGameOver del AnxietySystem para que el GameOverUIController lo reciba
            AnxietySystem.Instance.onGameOver?.Invoke();
        }
    }

    
    private void HandlePlayerEscaped()
    {
        Debug.Log("[AlteredPerceptionManager] El jugador ha escapado. Victoria Real!");

        // Liberar cursor para permitir navegar la UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (realVictoryCanvas != null)
        {
            realVictoryCanvas.SetActive(true);
        }
        
        else
        {
            Debug.LogWarning("[AlteredPerceptionManager] No se ha asignado realVictoryCanvas ni realVictorySceneName.");
        }
    }


    // =========================================================================
    //  LISTENERS
    // =========================================================================

    private void OnPreCollapseTick(float progress)
    {
        ApplyVignette(progress * maxVignetteIntensity);
        lensDistortionTimer += Time.deltaTime * lensDistortionErraticSpeed;
    }

    private void OnFaintEnded()
    {
        isRecoveringFromFaint = true;
        recoveryTimer = 0f;
    }

    private void ApplyVignette(float intensity)
    {
        if (playerCamera != null) { }
    }

    private void ApplyDisorientationEffects(float intensity)
    {
        Debug.Log($"[AlteredPerceptionManager] Recuperandose de desorientacion: {intensity:F2}");
    }
}