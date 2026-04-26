using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float rotationSmoothTime = 10f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float groundCheckDistance = 0.25f;
    [SerializeField] private float groundCheckOffset = 0.05f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform cameraTransform;


    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;         // ¿El personaje está tocando el suelo?
    private Animator playerAnimator;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        // Auto-detectar la cámara principal si no fue asignada
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
                Debug.LogError("[PlayerMovement] No se encontró una cámara principal. " +
                               "Asegúrate de que la Main Camera tenga el tag 'MainCamera'.");
        }

        playerAnimator = GetComponentInChildren<Animator>();

        // Bloquear y ocultar el cursor del mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private void Update()
    {
        CheckGrounded();
        HandleMovementAndRotation();
        HandleAnimations();
    }


    private void CheckGrounded()
    {
        // Origen del chequeo: levemente por encima de los pies
        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;

        isGrounded = Physics.CheckSphere(
            origin,
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (isGrounded && velocity.y < 0)
        {
            // Forzar una leve velocidad hacia abajo para mantener al personaje pegado al suelo
            velocity.y = -2f;
        }
    }

    private void HandleMovementAndRotation()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical   = Input.GetAxis("Vertical");

        // Calcular la dirección hacia la que mira la cámara
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            if (camForward != Vector3.zero)
            {
                // El personaje rota suavemente hacia la dirección de la cámara
                Quaternion targetRotation = Quaternion.LookRotation(camForward);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSmoothTime * Time.deltaTime
                );
            }
        }

        // El movimiento se calcula relativo al personaje (que ahora mira hacia la cámara).
        // Así, presionar hacia los lados ejecuta el strafe físico también.
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        
        // Evitar caminar más rápido en diagonal
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }
        
        // Aplicar movimiento horizontal
        controller.Move(moveDirection * walkSpeed * Time.deltaTime);

        // Aplicar gravedad
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleAnimations()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        if (playerAnimator != null)
        {
            // Usamos la magnitud del movimiento (va de 0 a 1)
            // Esto hace que si te movés para cualquier lado, el tipo camine
            float moveSpeed = moveDirection.magnitude;

            playerAnimator.SetFloat("Vertical", moveSpeed, 0.1f, Time.deltaTime);
        }
    }


    public bool IsGrounded => isGrounded;

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, groundCheckDistance);
    }
}
