using UnityEngine;


[RequireComponent(typeof(Collider))]
public class BatteryItem : MonoBehaviour, IInteractable
{
    [Header("Batería")]
    [SerializeField] private float batteryChargeAmount = 50f; // Porcentaje de carga
    [SerializeField] private float interactionCooldown = 0.5f;

    private bool canInteract = true;


    private void Start()
    {
        // Asegurarse de que el collider sea un trigger
        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;
    }

    public string GetInteractionPrompt()
    {
        return $"Recargar Linterna ({batteryChargeAmount:F0}%)";
    }

    public void Interact(GameObject interactor)
    {
        if (!canInteract) return;

        // Buscar el FlashlightController en el jugador
        FlashlightController flashlight = interactor.GetComponentInChildren<FlashlightController>();

        if (flashlight != null)
        {
            // Recargar la linterna
            flashlight.ChargeBattery(batteryChargeAmount);

            // Prevenir interacción múltiple rápida
            canInteract = false;
            Invoke(nameof(ResetInteraction), interactionCooldown);

            // Destruir el ítem de batería
            Destroy(gameObject);
            Debug.Log("[BatteryItem] Batería consumida y destruida.");
        }
        else
        {
            Debug.LogWarning("[BatteryItem] No se encontró FlashlightController en el jugador.");
        }
    }

    private void ResetInteraction()
    {
        canInteract = true;
    }
}
