using UnityEngine;

public class PlayerFaintController : MonoBehaviour
{
    private CharacterController characterController;
    private Animator animator;

    private bool isFainted = false;

    private void Awake()
    {
        // Auto-detectar componentes si no fueron asignados
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // Suscribirse a los eventos del AnxietySystem
        if (AnxietySystem.Instance != null)
        {
            AnxietySystem.Instance.onFaintStarted.AddListener(OnFaintStarted);
            AnxietySystem.Instance.onFaintEnded.AddListener(OnFaintEnded);
        }
        else
        {
            Debug.LogError("[PlayerFaintController] AnxietySystem no encontrado.");
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse para evitar memory leaks
        if (AnxietySystem.Instance != null)
        {
            AnxietySystem.Instance.onFaintStarted.RemoveListener(OnFaintStarted);
            AnxietySystem.Instance.onFaintEnded.RemoveListener(OnFaintEnded);
        }
    }

    public void OnFaintStarted()
    {
        if (isFainted) return;

        isFainted = true;

        // Desactivamos el movimiento para que el jugador no patine mientras cae
        if (characterController != null)
            characterController.enabled = false;

        // Disparar el trigger de desmayo
        if (animator != null)
        {
            animator.SetTrigger("Faint");
            Debug.Log("[PlayerFaintController] Trigger 'Faint' activado.");
        }
    }

    public void OnFaintEnded()
    {
        if (!isFainted) return;

        // Disparar el trigger de levantarse
        if (animator != null)
        {
            animator.SetTrigger("GetUp");
            Debug.Log("[PlayerFaintController] Trigger 'GetUp' activado.");
        }

        // Esperar a que la animación termine para devolver el control
        StartCoroutine(ReactivateCharacterControllerAfterAnimation());
    }

    private System.Collections.IEnumerator ReactivateCharacterControllerAfterAnimation()
    {
        // Ajustá este tiempo a lo que dure tu animación de GetUp
        yield return new WaitForSeconds(2.5f);

        if (characterController != null)
        {
            characterController.enabled = true;
            Debug.Log("[PlayerFaintController] CharacterController reactivado.");

        }

        if (animator != null)
        {
            animator.ResetTrigger("GetUp");
            animator.Play("Empty"); // fuerza el retorno al Blend Tree
            animator.SetFloat("Horizontal", 0f);
            animator.SetFloat("Vertical", 0f);
        }

        isFainted = false;
    }

    public bool IsFainted => isFainted;
}
