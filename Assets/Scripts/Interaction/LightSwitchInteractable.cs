using UnityEngine;


public class LightSwitchInteractable : MonoBehaviour, IInteractable
{

    [Header("Luces Controladas")]
    [SerializeField] private Light[] controlledLights;
    [SerializeField] private bool startOn = true;

    [Header("Luces Parpadantes de la Habitación")]
    [SerializeField] private RoomLightFlicker[] roomLightFlickers;

    [Header("Integración con Sistema de Ansiedad")]
    [SerializeField] private AnxietySystem anxietySystem;
    [SerializeField] private float anxietyOnLightsOff = 10f;
    [SerializeField] private float anxietyOnLightsOn = -5f;

    [Header("Rango de Detección de Sombras")]
    [SerializeField] private float shadowDetectionRadius = 20f;

    // ─── Puzzle de Victoria ────────────────────────────────────────────────────
    [Header("Puzzle de Victoria - 5 Luces")]
    // Marcar como true en el Inspector para los 5 interruptores que participan del puzzle.
    [SerializeField] private bool isPartOfVictoryPuzzle = false;

    // Contador estático compartido por todos los interruptores de la escena.
    private static int litSwitchCount = 0;
    // Total de interruptores necesarios para disparar la victoria.
    private static int totalVictorySwitches = 0;
    // Referencia cacheada para evitar FindAnyObjectByType repetidos.
    private static AlteredPerceptionManager _apmCache;

    private bool lightsAreOn; // Estado actual de las luces


    private void Awake()
    {
        // Contar cuantos interruptores participan del puzzle al iniciar la escena
        if (isPartOfVictoryPuzzle)
            totalVictorySwitches++;
    }

    private void Start()
    {
        lightsAreOn = startOn;
        ApplyLightState();

        // Inicializar el contador si esta luz arranca encendida y es parte del puzzle
        if (isPartOfVictoryPuzzle && startOn)
            litSwitchCount++;

        // Auto-detectar AnxietySystem si no fue asignado
        if (anxietySystem == null)
            anxietySystem = FindFirstObjectByType<AnxietySystem>();

        // Auto-detectar RoomLightFlickers en la habitación si no fueron asignados
        if (roomLightFlickers == null || roomLightFlickers.Length == 0)
        {
            roomLightFlickers = GetComponentsInChildren<RoomLightFlicker>();
        }
    }

    private void OnDestroy()
    {
        // Limpiar los estáticos al destruir la escena para evitar valores sucios en reinicios
        if (isPartOfVictoryPuzzle)
            totalVictorySwitches--;
    }

    public string GetInteractionPrompt()
    {
        return lightsAreOn ? "Apagar Luces" : "Encender Luces";
    }

    public void Interact(GameObject interactor)
    {
        // Invertir el estado de las luces
        lightsAreOn = !lightsAreOn;
        ApplyLightState();

        // Actualizar el contador del puzzle si este interruptor participa
        if (isPartOfVictoryPuzzle)
        {
            if (lightsAreOn)
                litSwitchCount++;
            else
                litSwitchCount--;

            Debug.Log($"[LightSwitchInteractable] Puzzle: {litSwitchCount}/{totalVictorySwitches} luces encendidas.");

            // Verificar condicion de victoria al encender (no al apagar)
            if (lightsAreOn && litSwitchCount >= totalVictorySwitches)
                TriggerVictoryCheck();
        }

        // Notificar al sistema de ansiedad (si estǭ disponible)
        if (anxietySystem != null)
        {
            float anxietyDelta = lightsAreOn ? anxietyOnLightsOn : anxietyOnLightsOff;
            anxietySystem.ModifyAnxiety(anxietyDelta);
        }

        // Si encendemos las luces, estabilizar el parpadeo y disipar sombras
        if (lightsAreOn)
        {
            StabilizeRoomLights();
            DisableShadowsInRadius();
        }

        Debug.Log($"[LightSwitchInteractable] Luces {(lightsAreOn ? "encendidas" : "apagadas")}.");
    }

    private void TriggerVictoryCheck()
    {
        Debug.Log("[LightSwitchInteractable] ¡TODAS LAS LUCES ENCENDIDAS! Llamando a VerifyVictoryCondition...");

        // Cachear la referencia para no buscarla cada vez
        if (_apmCache == null)
            _apmCache = FindAnyObjectByType<AlteredPerceptionManager>();

        _apmCache?.VerifyVictoryCondition();
    }

    private void ApplyLightState()
    {
        if (controlledLights == null) return;

        foreach (Light light in controlledLights)
        {
            if (light != null)
                light.enabled = lightsAreOn;
        }
    }

    private void StabilizeRoomLights()
    {
        // Estabilizar todas las luces parpadantes al 100% de intensidad
        if (roomLightFlickers == null || roomLightFlickers.Length == 0) return;

        foreach (RoomLightFlicker flicker in roomLightFlickers)
        {
            if (flicker != null)
                flicker.StabilizeLight();
        }

        Debug.Log("[LightSwitchInteractable] Luces de la habitación estabilizadas.");
    }

    private void DisableShadowsInRadius()
    {
        // Buscar todas las sombras dentro del radio de detección
        Collider[] shadowColliders = Physics.OverlapSphere(transform.position, shadowDetectionRadius);

        foreach (Collider collider in shadowColliders)
        {
            ShadowEnemy shadowEnemy = collider.GetComponent<ShadowEnemy>();
            if (shadowEnemy != null)
            {
                shadowEnemy.DismissShadow();
            }
        }

        Debug.Log("[LightSwitchInteractable] Sombras disipadas en la habitación.");
    }

    public void SetLightsState(bool on)
    {
        lightsAreOn = on;
        ApplyLightState();
    }
}
