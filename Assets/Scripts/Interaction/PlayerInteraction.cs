using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{

    [Header("Raycast")]
    [SerializeField] private float interactionRange = 2.5f;
    [SerializeField] private LayerMask interactableLayers;

    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private InteractionUI interactionUI;


    private IInteractable currentInteractable; // El objeto interactuable actualmente en foco
    private RaycastHit hitInfo;                // Información del Raycast actual


    private void Start()
    {
        // Auto-detectar la cámara si no fue asignada
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                Debug.LogError("[PlayerInteraction] No se encontró la cámara principal.");
        }

        // Auto-detectar la UI si no fue asignada
        if (interactionUI == null)
        {
            interactionUI = FindFirstObjectByType<InteractionUI>();
            if (interactionUI == null)
                Debug.LogWarning("[PlayerInteraction] No se encontró InteractionUI en la escena. " +
                                 "Los prompts de interacción no se mostrarán.");
        }
    }

    private void Update()
    {
        CheckForInteractable();

        // Si hay un objeto en foco y el jugador presiona E, interactuar
        if (currentInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            currentInteractable.Interact(gameObject);
        }
    }

    private void CheckForInteractable()
    {
        // Crear el rayo desde el centro exacto de la pantalla de la cámara
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out hitInfo, interactionRange, interactableLayers, QueryTriggerInteraction.Ignore))
        {
            // El rayo golpeó algo en la capa Interactable
            // Intentar obtener el componente IInteractable del objeto golpeado
            IInteractable interactable = hitInfo.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // Nuevo objeto en foco o el mismo que antes
                currentInteractable = interactable;

                // Actualizar la UI con el prompt del objeto actual
                if (interactionUI != null)
                    interactionUI.ShowPrompt(currentInteractable.GetInteractionPrompt());
            }
            else
            {
                // El objeto golpeado no es interactuable (sin el componente IInteractable)
                ClearFocus();
            }
        }
        else
        {
            // El rayo no golpeó nada dentro del rango
            ClearFocus();
        }
    }

    private void ClearFocus()
    {
        currentInteractable = null;

        if (interactionUI != null)
            interactionUI.HidePrompt();
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;

        Gizmos.color = currentInteractable != null ? Color.green : Color.yellow;
        Gizmos.DrawRay(playerCamera.transform.position,
                       playerCamera.transform.forward * interactionRange);

        if (currentInteractable != null)
            Gizmos.DrawWireSphere(hitInfo.point, 0.1f);
    }
}
