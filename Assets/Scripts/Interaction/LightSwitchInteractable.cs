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


    private bool lightsAreOn; // Estado actual de las luces

    private void Start()
    {
        lightsAreOn = startOn;
        ApplyLightState();

        // Auto-detectar AnxietySystem si no fue asignado
        if (anxietySystem == null)
            anxietySystem = FindFirstObjectByType<AnxietySystem>();

        // Auto-detectar RoomLightFlickers en la habitación si no fueron asignados
        if (roomLightFlickers == null || roomLightFlickers.Length == 0)
        {
            roomLightFlickers = GetComponentsInChildren<RoomLightFlicker>();
        }
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

        // Notificar al sistema de ansiedad (si está disponible)
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
