using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class ShadowEnemy : MonoBehaviour
{
    public enum ShadowState { Patrol, Chase, Flee }


    [Header("Velocidades de Movimiento")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float fleeSpeed = 5f;


    [Header("Patrullaje")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private bool randomPatrol = false;
    [SerializeField] private float waitTimeAtWaypoint = 1.5f;

    [Header("Detección del Jugador")]
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private bool requireLineOfSight = false;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float loseTargetTime = 3f;


    [Header("Huida de Linterna")]
    [SerializeField] private bool fleeFromFlashlight = false;
    [SerializeField] private float fleeDistance = 10f;
    [SerializeField] private float fleeCooldown = 2f;


    [Header("Visual")]
    [SerializeField] private Renderer shadowRenderer;
    [SerializeField, Range(0f, 1f)] private float patrolAlpha = 0.8f;
    [SerializeField, Range(0f, 1f)] private float chaseAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float fleeAlpha = 0.2f;

    private NavMeshAgent    agent;           // Componente de navegación
    private AnxietyStimulus stimulus;        // Componente de daño psicológico
    private Transform       playerTransform; // Transform del jugador (se busca en Start)
    private AudioSource audioEnemy;

    private ShadowState currentState = ShadowState.Patrol;

    // — Patrullaje —
    private int   currentWaypointIndex = 0;
    private float waypointWaitTimer    = 0f;
    private bool  isWaitingAtWaypoint  = false;

    // — Chase —
    private float loseTargetTimer = 0f; // Cuenta cuánto tiempo lleva sin ver al jugador

    // — Flee —
    private float fleeCooldownTimer = 0f; // Cuenta el cooldown post-linterna

    public ShadowState CurrentState => currentState;

    public bool PlayerDetected => playerTransform != null &&
                                  Vector3.Distance(transform.position, playerTransform.position)
                                  <= detectionRadius;


    private void Awake()
    {
        agent    = GetComponent<NavMeshAgent>();
        stimulus = GetComponent<AnxietyStimulus>();

        if (stimulus == null)
            Debug.LogWarning($"[ShadowEnemy] '{gameObject.name}' no tiene AnxietyStimulus. " +
                             "El daño psicológico no funcionará. Añadir el componente.");
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
            Debug.LogError($"[ShadowEnemy] No se encontró un GameObject con tag '{playerTag}'. " +
                           "Asegúrate de que el jugador tenga el tag 'Player'.");
        }

        // Configurar el agente para estado inicial
        TransitionToState(ShadowState.Patrol);

        audioEnemy = GetComponent<AudioSource>();
        if (audioEnemy != null)
        {
            // Si la velocidad del agente es mayor a casi cero, está caminando
            if (agent.velocity.magnitude > 0.1f)
            {
                if (!audioEnemy.isPlaying) audioEnemy.Play();
            }
            else
            {
                audioEnemy.Stop();
            }
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Cada estado tiene su propia lógica de evaluación
        switch (currentState)
        {
            case ShadowState.Patrol: UpdatePatrol(); break;
            case ShadowState.Chase:  UpdateChase();  break;
            case ShadowState.Flee:   UpdateFlee();   break;
        }
    }

    private void UpdatePatrol()
    {
        // ¿Detectamos al jugador? → CHASE
        if (CanDetectPlayer())
        {
            TransitionToState(ShadowState.Chase);
            return;
        }

        // Sin waypoints: la sombra se queda quieta (válido para sombras estáticas)
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        // Esperar en el waypoint actual si toca
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

        // Verificar si llegamos al waypoint actual
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // Llegamos: iniciar espera antes de ir al siguiente
            isWaitingAtWaypoint = true;
            waypointWaitTimer   = waitTimeAtWaypoint;
        }
    }

    private void UpdateChase()
    {
        if (fleeFromFlashlight && IsBeingIlluminated())
        {
            TransitionToState(ShadowState.Flee);
            return;
        }

        if (CanDetectPlayer())
        {
            loseTargetTimer = 0f;
            // VALIDACIÓN DE SEGURIDAD PARA UNITY 6
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.SetDestination(playerTransform.position);
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
        // Si la linterna sigue apuntando, refrescar el cooldown
        if (IsBeingIlluminated())
        {
            fleeCooldownTimer = fleeCooldown;

            // Calcular punto de huida: alejarse del jugador
            Vector3 fleeDirection = (transform.position - playerTransform.position).normalized;
            Vector3 fleeTarget    = transform.position + fleeDirection * fleeDistance;

            // Verificar que el punto de huida esté en el NavMesh
            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
        else
        {
            // La linterna ya no apunta: contar el cooldown
            fleeCooldownTimer -= Time.deltaTime;
            if (fleeCooldownTimer <= 0f)
            {
                // Volver a patrullar (no a Chase, para dar respiro al jugador)
                TransitionToState(ShadowState.Patrol);
            }
        }
    }

    private void TransitionToState(ShadowState newState)
    {
        if (currentState == newState) return; // Evitar transiciones redundantes

        currentState = newState;

        switch (newState)
        {
            case ShadowState.Patrol:
                agent.speed        = patrolSpeed;
                agent.stoppingDistance = 0.5f;
                loseTargetTimer    = 0f;
                fleeCooldownTimer  = 0f;
                // Ir al primer (o siguiente) waypoint
                MoveToNextWaypoint();
                SetAlpha(patrolAlpha);
                Debug.Log($"[ShadowEnemy] '{gameObject.name}' → PATROL");
                break;

            case ShadowState.Chase:
                agent.speed        = chaseSpeed;
                agent.stoppingDistance = 1.5f; // Quedarse a cierta distancia (no atravesar al jugador)
                loseTargetTimer    = 0f;
                SetAlpha(chaseAlpha);
                Debug.Log($"[ShadowEnemy] '{gameObject.name}' → CHASE");
                break;

            case ShadowState.Flee:
                agent.speed        = fleeSpeed;
                agent.stoppingDistance = 0.5f;
                fleeCooldownTimer  = fleeCooldown;
                SetAlpha(fleeAlpha);
                Debug.Log($"[ShadowEnemy] '{gameObject.name}' → FLEE");
                break;
        }
    }

    private bool CanDetectPlayer()
    {
        if (playerTransform == null) return false;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist > detectionRadius) return false;

        // Si no se requiere LOS, la proximidad es suficiente
        if (!requireLineOfSight) return true;

        // Verificar línea de visión con Raycast
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Ray     losRay            = new Ray(transform.position + Vector3.up * 0.5f, directionToPlayer);

        // Si el Raycast NO choca con obstáculos antes de llegar al jugador → hay LOS
        bool blocked = Physics.Raycast(losRay, dist, obstacleLayers, QueryTriggerInteraction.Ignore);
        return !blocked;
    }


    private bool IsBeingIlluminated()
    {
        // Solo aplica si fleeFromFlashlight está activado Y tenemos el stimulus
        if (!fleeFromFlashlight || stimulus == null) return false;

        // AnxietyStimulus.isBeingRationalized se activa en FlashlightController.cs
        // No podemos leer el campo privado directamente, pero podemos añadir
        // una propiedad pública que lo exponga. Ver nota abajo.
        return stimulus.IsBeingRationalized;
    }

    private void MoveToNextWaypoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        // Seleccionar el índice del siguiente waypoint
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

        // Verificar que el waypoint no sea null antes de navegar
        if (patrolPoints[currentWaypointIndex] != null)
            agent.SetDestination(patrolPoints[currentWaypointIndex].position);
        else
            Debug.LogWarning($"[ShadowEnemy] Waypoint [{currentWaypointIndex}] es null. " +
                             "Verifica que todos los patrolPoints estén asignados en el Inspector.");
    }

    private void SetAlpha(float alpha)
    {
        if (shadowRenderer == null) return;

        // Intentar modificar el alpha del color del material
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        shadowRenderer.GetPropertyBlock(mpb);

        Color currentColor = mpb.GetColor("_BaseColor");
        if (currentColor == Color.clear) // No estaba seteado aún
            currentColor = shadowRenderer.sharedMaterial.GetColor("_BaseColor");

        currentColor.a = alpha;
        mpb.SetColor("_BaseColor", currentColor);
        shadowRenderer.SetPropertyBlock(mpb);
    }


    public void ForceState(ShadowState state)
    {
        TransitionToState(state);
    }

    public void Teleport(Vector3 position)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            TransitionToState(ShadowState.Patrol);
            Debug.Log($"[ShadowEnemy] '{gameObject.name}' teletransportado a {hit.position}.");
        }
        else
        {
            Debug.LogWarning($"[ShadowEnemy] No se encontró un punto válido del NavMesh cerca de {position}.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Radio de detección
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Líneas hacia los waypoints
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
            // Conectar el último con el primero (loop visual)
            if (patrolPoints.Length > 1 && patrolPoints[0] != null && patrolPoints[patrolPoints.Length - 1] != null)
                Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position, patrolPoints[0].position);
        }

        // Mostrar estado actual en color
        switch (currentState)
        {
            case ShadowState.Patrol: Gizmos.color = Color.blue;   break;
            case ShadowState.Chase:  Gizmos.color = Color.red;    break;
            case ShadowState.Flee:   Gizmos.color = Color.yellow; break;
        }
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
