using System.Collections;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class ShadowEnemy : MonoBehaviour
{
    public enum ShadowState { Patrol, Chase, Flee, Dismissed }


    [Header("Velocidades de Movimiento")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float chaseSpeed  = 3.5f;
    [SerializeField] private float fleeSpeed   = 5f;


    [Header("Patrullaje")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private bool  randomPatrol       = false;
    [SerializeField] private float waitTimeAtWaypoint = 1.5f;

    [Header("Confinamiento de Zona (NavMesh Area)")]
    // Mascara de areas del NavMesh en las que esta sombra tiene permitido moverse.
    // Usar "Everything" para que use todas las areas (comportamiento por defecto).
    // Para confinar a una habitacion, crear un Area personalizada en el NavMesh
    // (ej. "Apartment_01") y seleccionar SOLO esa area en esta mascara.
    [SerializeField] private LayerMask navMeshAreaMask = -1; // -1 = NavMesh.AllAreas

    [Header("Deteccion del Jugador")]
    [SerializeField] private float detectionRadius   = 6f;
    [SerializeField] private bool  requireLineOfSight = false;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private string playerTag        = "Player";
    [SerializeField] private float loseTargetTime    = 3f;


    [Header("Huida de Linterna")]
    [SerializeField] private bool  fleeFromFlashlight = false;
    [SerializeField] private float fleeDistance        = 10f;
    [SerializeField] private float fleeCooldown        = 2f;


    [Header("Visual")]
    [SerializeField] private Renderer shadowRenderer;
    [SerializeField, Range(0f, 1f)] private float patrolAlpha   = 0.8f;
    [SerializeField, Range(0f, 1f)] private float chaseAlpha    = 1f;
    [SerializeField, Range(0f, 1f)] private float fleeAlpha     = 0.2f;
    // Duracion de la animacion de disipacion al ser eliminada por la luz
    [SerializeField] private float dismissFadeDuration = 1.2f;


    // ─── Referencias internas ─────────────────────────────────────────────────
    private NavMeshAgent    agent;
    private AnxietyStimulus stimulus;
    private Transform       playerTransform;
    private AudioSource     audioEnemy;

    private ShadowState currentState = ShadowState.Patrol;

    // Patrullaje
    private int   currentWaypointIndex = 0;
    private float waypointWaitTimer    = 0f;
    private bool  isWaitingAtWaypoint  = false;

    // Chase
    private float loseTargetTimer = 0f;

    // Flee
    private float fleeCooldownTimer = 0f;

    // Confinamiento dinamico: se pueden sobreescribir los waypoints del prefab
    // al momento del Instantiate mediante InjectPatrolPoints()
    private bool waypointsInjected = false;

    // ─── Propiedades publicas ─────────────────────────────────────────────────
    public ShadowState CurrentState => currentState;
    public bool PlayerDetected      => playerTransform != null &&
                                       Vector3.Distance(transform.position, playerTransform.position) <= detectionRadius;


    // =========================================================================
    //  INICIALIZACION
    // =========================================================================

    private void Awake()
    {
        agent    = GetComponent<NavMeshAgent>();
        stimulus = GetComponent<AnxietyStimulus>();
        audioEnemy = GetComponent<AudioSource>();

        if (stimulus == null)
            Debug.LogWarning($"[ShadowEnemy] '{gameObject.name}' no tiene AnxietyStimulus. " +
                             "El danio psicologico no funcionara. Anadir el componente.");
    }

    private void Start()
    {
        // Buscar al jugador por tag
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError($"[ShadowEnemy] No se encontro un GameObject con tag '{playerTag}'. " +
                           "Asegurate de que el jugador tenga ese tag.");
        }

        // Aplicar la mascara de areas del NavMesh para confinar el movimiento a esta zona.
        // Esto es clave para que una sombra del piso 3 no intente llegar al piso 1.
        if (navMeshAreaMask != -1)
            agent.areaMask = navMeshAreaMask;

        // Iniciar en modo patrulla
        TransitionToState(ShadowState.Patrol);
    }


    // =========================================================================
    //  UPDATE - Maquina de estados
    // =========================================================================

    private void Update()
    {
        // No actualizar la IA mientras esta siendo disipada
        if (currentState == ShadowState.Dismissed) return;

        switch (currentState)
        {
            case ShadowState.Patrol: UpdatePatrol(); break;
            case ShadowState.Chase:  UpdateChase();  break;
            case ShadowState.Flee:   UpdateFlee();   break;
        }
    }

    private void UpdatePatrol()
    {
        // Prioridad 1: detectar al jugador y pasar a Chase
        if (CanDetectPlayer())
        {
            TransitionToState(ShadowState.Chase);
            return;
        }

        // Prioridad 2: verificar si hay waypoints asignados
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        // Si esta esperando en el waypoint, decrementar el timer
        if (isWaitingAtWaypoint)
        {
            waypointWaitTimer -= Time.deltaTime;
            if (waypointWaitTimer <= 0f)
            {
                isWaitingAtWaypoint = false;
                MoveToNextWaypoint();
            }
            return;
        }

        // Verificar si llego al waypoint actual
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            isWaitingAtWaypoint = true;
            waypointWaitTimer   = waitTimeAtWaypoint;
        }
    }

    private void UpdateChase()
    {
        // Revisar si la linterna apunta a la sombra: huir primero
        if (fleeFromFlashlight && IsBeingIlluminated())
        {
            TransitionToState(ShadowState.Flee);
            return;
        }

        if (CanDetectPlayer())
        {
            loseTargetTimer = 0f;
            // Perseguir solo si el jugador esta dentro de la zona accesible del NavMesh
            if (IsDestinationReachable(playerTransform.position))
            {
                agent.SetDestination(playerTransform.position);
            }
            else
            {
                // El jugador salio de la zona de esta sombra: volver a patrullar
                Debug.Log($"[ShadowEnemy] '{gameObject.name}': el jugador salio del area accesible. Volviendo a patrullar.");
                TransitionToState(ShadowState.Patrol);
            }
        }
        else
        {
            loseTargetTimer += Time.deltaTime;
            if (loseTargetTimer >= loseTargetTime)
            {
                TransitionToState(ShadowState.Patrol);
            }
        }
    }

    private void UpdateFlee()
    {
        if (IsBeingIlluminated())
        {
            fleeCooldownTimer = fleeCooldown;

            // Calcular punto de huida: alejarse del jugador
            Vector3 fleeDirection = (transform.position - playerTransform.position).normalized;
            Vector3 fleeTarget    = transform.position + fleeDirection * fleeDistance;

            // Verificar que el punto de huida este en el NavMesh de esta zona
            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, fleeDistance, agent.areaMask))
                agent.SetDestination(hit.position);
        }
        else
        {
            fleeCooldownTimer -= Time.deltaTime;
            if (fleeCooldownTimer <= 0f)
            {
                TransitionToState(ShadowState.Patrol);
            }
        }
    }


    // =========================================================================
    //  TRANSICION DE ESTADOS
    // =========================================================================

    private void TransitionToState(ShadowState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (newState)
        {
            case ShadowState.Patrol:
                agent.speed            = patrolSpeed;
                agent.stoppingDistance = 0.5f;
                loseTargetTimer        = 0f;
                fleeCooldownTimer      = 0f;
                MoveToNextWaypoint();
                SetAlpha(patrolAlpha);
                Debug.Log($"[ShadowEnemy] '{gameObject.name}' -> PATROL");
                break;

            case ShadowState.Chase:
                agent.speed            = chaseSpeed;
                agent.stoppingDistance = 1.5f;
                loseTargetTimer        = 0f;
                SetAlpha(chaseAlpha);
                Debug.Log($"[ShadowEnemy] '{gameObject.name}' -> CHASE");
                break;

            case ShadowState.Flee:
                agent.speed            = fleeSpeed;
                agent.stoppingDistance = 0.5f;
                fleeCooldownTimer      = fleeCooldown;
                SetAlpha(fleeAlpha);
                Debug.Log($"[ShadowEnemy] '{gameObject.name}' -> FLEE");
                break;

            case ShadowState.Dismissed:
                agent.isStopped = true;
                Debug.Log($"[ShadowEnemy] '{gameObject.name}' -> DISMISSED");
                break;
        }
    }


    // =========================================================================
    //  DETECCION DEL JUGADOR
    // =========================================================================

    private bool CanDetectPlayer()
    {
        if (playerTransform == null) return false;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist > detectionRadius) return false;

        if (!requireLineOfSight) return true;

        // Verificar linea de vision con Raycast
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Ray     losRay            = new Ray(transform.position + Vector3.up * 0.5f, directionToPlayer);

        bool blocked = Physics.Raycast(losRay, dist, obstacleLayers, QueryTriggerInteraction.Ignore);
        return !blocked;
    }

    private bool IsBeingIlluminated()
    {
        if (!fleeFromFlashlight || stimulus == null) return false;
        return stimulus.IsBeingRationalized;
    }

    /// <summary>
    /// Verifica si un destino puede alcanzarse desde la posicion actual
    /// usando SOLO las areas del NavMesh asignadas a esta sombra.
    /// Evita que la sombra intente perseguir al jugador a zonas inaccessibles.
    /// </summary>
    private bool IsDestinationReachable(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();
        // Calcular el camino usando la mascara de areas de esta instancia
        bool found = NavMesh.CalculatePath(transform.position, destination, agent.areaMask, path);
        return found && path.status == NavMeshPathStatus.PathComplete;
    }


    // =========================================================================
    //  PATRULLAJE
    // =========================================================================

    private void MoveToNextWaypoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (randomPatrol)
        {
            // Aleatorio pero que no sea el mismo que el actual
            int next = currentWaypointIndex;
            if (patrolPoints.Length > 1)
                while (next == currentWaypointIndex)
                    next = Random.Range(0, patrolPoints.Length);
            currentWaypointIndex = next;
        }
        else
        {
            // Secuencial: avanzar al siguiente, volver al 0 al terminar
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolPoints.Length;
        }

        if (patrolPoints[currentWaypointIndex] != null)
            agent.SetDestination(patrolPoints[currentWaypointIndex].position);
        else
            Debug.LogWarning($"[ShadowEnemy] Waypoint [{currentWaypointIndex}] es null. " +
                             "Verifica que todos los patrolPoints esten asignados.");
    }


    // =========================================================================
    //  EFECTOS VISUALES (ALPHA)
    // =========================================================================

    private void SetAlpha(float alpha)
    {
        if (shadowRenderer == null) return;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        shadowRenderer.GetPropertyBlock(mpb);

        Color currentColor = mpb.GetColor("_BaseColor");
        // Si no estaba seteado en el MPB, leer del material compartido
        if (currentColor == Color.clear)
            currentColor = shadowRenderer.sharedMaterial.GetColor("_BaseColor");

        currentColor.a = alpha;
        mpb.SetColor("_BaseColor", currentColor);
        shadowRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>
    /// Corrutina que reduce el alpha gradualmente hasta 0 y luego destruye el objeto.
    /// Produce el efecto de disipacion de la sombra al encender la luz.
    /// </summary>
    private IEnumerator FadeOutAndDestroy()
    {
        if (shadowRenderer == null)
        {
            Destroy(gameObject);
            yield break;
        }

        // Leer el alpha actual para partir desde ahi
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        shadowRenderer.GetPropertyBlock(mpb);
        Color startColor = mpb.GetColor("_BaseColor");
        if (startColor == Color.clear)
            startColor = shadowRenderer.sharedMaterial.GetColor("_BaseColor");

        float elapsed = 0f;
        while (elapsed < dismissFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t     = elapsed / dismissFadeDuration;
            float alpha = Mathf.Lerp(startColor.a, 0f, t);
            SetAlpha(alpha);
            yield return null;
        }

        Destroy(gameObject);
    }


    // =========================================================================
    //  METODOS PUBLICOS DE CONTROL
    // =========================================================================

    /// <summary>
    /// Inyecta waypoints de forma dinamica al ser instanciada por ShadowSpawnerTrigger.
    /// Permite que cada sombra reciba su set de rutas sin modificar el prefab.
    /// Si ya se inyectaron waypoints, esta llamada no tiene efecto.
    /// </summary>
    public void InjectPatrolPoints(Transform[] newWaypoints)
    {
        if (waypointsInjected)
        {
            Debug.LogWarning($"[ShadowEnemy] '{gameObject.name}': InjectPatrolPoints() llamado mas de una vez. Ignorando.");
            return;
        }

        patrolPoints     = newWaypoints;
        waypointsInjected = true;
        currentWaypointIndex = 0;

        // Si ya estamos en Patrol, ir al primer waypoint inmediatamente
        if (currentState == ShadowState.Patrol && patrolPoints.Length > 0)
        {
            if (patrolPoints[0] != null)
                agent.SetDestination(patrolPoints[0].position);
        }

        Debug.Log($"[ShadowEnemy] '{gameObject.name}': {newWaypoints.Length} waypoints inyectados.");
    }

    /// <summary>
    /// Disuelta la sombra con una animacion de desvanecimiento y la destruye.
    /// Conectar a LightSwitchInteractable para eliminar la sombra al encender la luz.
    /// Equivale al antiguo 'DesactivarSombra()' mencionado en el GDD.
    /// </summary>
    public void DismissShadow()
    {
        if (currentState == ShadowState.Dismissed) return;

        TransitionToState(ShadowState.Dismissed);
        StartCoroutine(FadeOutAndDestroy());
        Debug.Log($"[ShadowEnemy] '{gameObject.name}' disipandose por la luz...");
    }

    /// <summary>
    /// Alias en espaniol para compatibilidad con la nomenclatura del GDD.
    /// Llama internamente a DismissShadow().
    /// </summary>
    public void DesactivarSombra() => DismissShadow();

    /// <summary>
    /// Fuerza una transicion de estado desde sistemas externos (eventos de climax, etc).
    /// </summary>
    public void ForceState(ShadowState state)
    {
        TransitionToState(state);
    }

    /// <summary>
    /// Teleporta la sombra a una nueva posicion valida del NavMesh y vuelve a patrullar.
    /// </summary>
    public void Teleport(Vector3 position)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, agent.areaMask))
        {
            agent.Warp(hit.position);
            TransitionToState(ShadowState.Patrol);
            Debug.Log($"[ShadowEnemy] '{gameObject.name}' teletransportado a {hit.position}.");
        }
        else
        {
            Debug.LogWarning($"[ShadowEnemy] No se encontro un punto valido del NavMesh cerca de {position}.");
        }
    }


    // =========================================================================
    //  GIZMOS DE EDITOR
    // =========================================================================

    private void OnDrawGizmosSelected()
    {
        // Radio de deteccion
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Lineas hacia los waypoints
        if (patrolPoints != null)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.8f);
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                Gizmos.DrawSphere(patrolPoints[i].position, 0.15f);
                if (i > 0 && patrolPoints[i - 1] != null)
                    Gizmos.DrawLine(patrolPoints[i - 1].position, patrolPoints[i].position);
            }
            // Conectar el ultimo con el primero (loop visual)
            if (patrolPoints.Length > 1 && patrolPoints[0] != null && patrolPoints[patrolPoints.Length - 1] != null)
                Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position, patrolPoints[0].position);
        }

        // Indicador del estado actual
        switch (currentState)
        {
            case ShadowState.Patrol:    Gizmos.color = Color.blue;   break;
            case ShadowState.Chase:     Gizmos.color = Color.red;    break;
            case ShadowState.Flee:      Gizmos.color = Color.yellow; break;
            case ShadowState.Dismissed: Gizmos.color = Color.gray;   break;
        }
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}