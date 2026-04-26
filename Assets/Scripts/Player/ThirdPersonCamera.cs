using UnityEngine;


public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Referencia al Pivote")]
    [SerializeField] private Transform pivot;

    [Header("Distancia y Posición")]
    [SerializeField] private float distance = 4.0f;
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private LayerMask collisionLayers;

    [Header("Sensibilidad del Mouse")]
    [SerializeField] private float sensitivityX = 200f;
    [SerializeField] private float sensitivityY = 150f;

    [Header("Límites de Inclinación Vertical")]
    [SerializeField] private float minVerticalAngle = -20f;
    [SerializeField] private float maxVerticalAngle = 60f;

    [Header("Suavizado")]
    [SerializeField] private float smoothSpeed = 12f;

    private float currentYaw;   // Rotación acumulada en el eje Y (horizontal)
    private float currentPitch; // Rotación acumulada en el eje X (vertical)

    private void Start()
    {
        if (pivot == null)
        {
            Debug.LogError("[ThirdPersonCamera] No se asignó el 'pivot' en el Inspector. " +
                           "Crea un GameObject vacío 'CameraPivot' como hijo del Player " +
                           "y asígnalo al campo 'pivot' de este script.");
            enabled = false; // Desactivar el script para evitar errores en cascada
            return;
        }

        // Inicializar la rotación de la cámara con la rotación actual del pivot
        // para evitar un salto brusco al inicio del juego
        Vector3 initialAngles = pivot.eulerAngles;
        currentYaw   = initialAngles.y;
        currentPitch = initialAngles.x;
    }


    private void LateUpdate()
    {
        HandleRotation();
        HandlePosition();
    }

    private void HandleRotation()
    {
        // Leer el delta del mouse (cuánto se movió en este frame)
        float mouseX = Input.GetAxis("Mouse X") * sensitivityX * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivityY * Time.deltaTime;

        // Acumular rotación horizontal (sin límite: puede girar 360°)
        currentYaw += mouseX;

        // Acumular rotación vertical e invertirla para que sea intuitiva:
        // mover el mouse hacia arriba → la cámara sube (pitch negativo)
        currentPitch -= mouseY;

        // Limitar el ángulo vertical para no voltear la cámara
        currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);

        // Aplicar la rotación al pivot (que es hijo del Player)
        // La rotación horizontal (yaw) la ponemos como rotación global del pivot
        // para que no se acumule con la rotación del Player
        pivot.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    private void HandlePosition()
    {
        // Posición ideal: detrás del pivot (en la dirección -forward del pivot)
        // a la distancia configurada
        Vector3 desiredPosition = pivot.position - pivot.forward * distance;

        // ── Detección de colisión de cámara ──────────────────────────────
        // Disparamos un SphereCast desde el pivot hacia la posición ideal.
        // Si choca con algo, usamos el punto de impacto como posición de la cámara.
        float actualDistance = distance;

        RaycastHit hit;
        Vector3 directionFromPivot = (desiredPosition - pivot.position).normalized;

        if (Physics.SphereCast(
                pivot.position,
                collisionRadius,
                directionFromPivot,
                out hit,
                distance,
                collisionLayers,
                QueryTriggerInteraction.Ignore))
        {
            // La cámara se acerca al punto de impacto (con un pequeño offset para evitar
            // que quede exactamente en la superficie)
            actualDistance = hit.distance - collisionRadius;
            actualDistance = Mathf.Max(actualDistance, 0.5f); // Distancia mínima de seguridad
        }

        // Calcular la posición final con la distancia corregida
        Vector3 finalPosition = pivot.position - pivot.forward * actualDistance;

        // ── Suavizado de movimiento ───────────────────────────────────────
        // Usar Lerp para suavizar el movimiento de la cámara. Esto evita
        // saltos bruscos cuando la colisión entra/sale de juego.
        transform.position = Vector3.Lerp(
            transform.position,
            finalPosition,
            smoothSpeed * Time.deltaTime
        );

        // La cámara siempre mira hacia el pivot (el personaje)
        transform.LookAt(pivot.position);
    }

    private void OnDrawGizmosSelected()
    {
        if (pivot == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pivot.position, transform.position);
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
        Gizmos.DrawWireSphere(pivot.position, 0.1f);
    }
}
