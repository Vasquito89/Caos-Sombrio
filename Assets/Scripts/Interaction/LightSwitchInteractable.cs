using UnityEngine;


public class LightSwitchInteractable : MonoBehaviour, IInteractable
{

    [Header("Luces Controladas")]
    [SerializeField] private Light[] controlledLights;
    [SerializeField] private bool startOn = true;

    [Header("Integración con Sistema de Ansiedad")]
    [SerializeField] private AnxietySystem anxietySystem;
    [SerializeField] private float anxietyOnLightsOff = 10f;
    [SerializeField] private float anxietyOnLightsOn = -5f;


    private bool lightsAreOn; // Estado actual de las luces

    private void Start()
    {
        lightsAreOn = startOn;
        ApplyLightState();

        // Auto-detectar AnxietySystem si no fue asignado
        if (anxietySystem == null)
            anxietySystem = FindFirstObjectByType<AnxietySystem>();
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

    public void SetLightsState(bool on)
    {
        lightsAreOn = on;
        ApplyLightState();
    }
}
