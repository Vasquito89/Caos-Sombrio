using UnityEngine;


public class FlashlightPickup : MonoBehaviour, IInteractable
{

    [Header("Referencia a la Linterna del Jugador")]
    [SerializeField] private Light playerFlashlightLight;
    [SerializeField] private bool startOn = true;

    [Header("Rotación Decorativa")]
    [SerializeField] private bool rotateInScene = true;
    [SerializeField] private float rotationSpeed = 45f;


    private void Update()
    {
        // Rotación decorativa para que el objeto sea más visible en la escena
        if (rotateInScene)
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
 
    public string GetInteractionPrompt()
    {
        return "Recoger Linterna";
    }

    public void Interact(GameObject interactor)
    {
        // Activar el Light de la linterna del jugador
        if (playerFlashlightLight != null)
        {
            playerFlashlightLight.gameObject.SetActive(true);
            playerFlashlightLight.enabled = startOn;
            Debug.Log("[FlashlightPickup] Linterna recogida y activada en el jugador.");
        }
        else
        {
            Debug.LogWarning("[FlashlightPickup] No se asignó el Light de la linterna del jugador " +
                             "en el Inspector. La interacción no tiene efecto visual.");
        }

        // Destruir este objeto pickup de la escena
        Destroy(gameObject);
    }
}
