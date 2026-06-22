using System.Collections;
using UnityEngine;
using UnityEngine.AI;


/// <summary>
/// Spawner basado en Trigger que instancia un ShadowEnemy cuando el jugador
/// cruza un umbral (pasillo, puerta, entrada a un departamento).
/// 
/// Configuracion en Unity:
///   - Este GameObject necesita un BoxCollider con "Is Trigger" = true.
///   - Asignar shadowPrefab y spawnPoint en el Inspector.
///   - El prefab debe tener el componente ShadowEnemy y un NavMeshAgent.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShadowSpawnerTrigger : MonoBehaviour
{
    // =========================================================================
    //  CONFIGURACION EN INSPECTOR
    // =========================================================================

    [Header("Prefab a Instanciar")]
    // Prefab del ShadowEnemy que se instanciara al cruzar el umbral
    [SerializeField] private GameObject shadowPrefab;

    [Header("Punto de Aparicion")]
    // Transform exacto donde aparecera la sombra (posicion + rotacion)
    // Si no se asigna, se usa la posicion de este GameObject + spawnOffset
    [SerializeField] private Transform spawnPoint;

    [Header("Configuracion de Spawneo")]
    // Offset adicional respecto al spawnPoint (o a este Transform si spawnPoint es null)
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    // Si es true, la sombra solo se spawnea una vez por sesion de juego
    [SerializeField] private bool spawnOnlyOnce = false;
    // Retardo en segundos antes de que la sombra aparezca al cruzar el trigger
    [SerializeField] private float spawnDelay = 0.5f;

    [Header("Waypoints del Piso/Habitacion")]
    // Waypoints exclusivos de esta zona. Se inyectan al ShadowEnemy tras el spawn
    // para confinarlo a esta area del edificio sin necesidad de configurarlo en el prefab.
    [SerializeField] private Transform[] roomWaypoints;

    [Header("Efecto de Humo al Spawn")]
    // Prefab opcional con ShadowSmokeEffect. Si no se asigna, el humo se crea por codigo.
    [SerializeField] private GameObject smokePrefab;
    // Si es false, no se genera humo en este spawner
    [SerializeField] private bool enableSmokeEffect = true;

    [Header("Debug")]
    // Dibuja un icono en la escena para identificar los spawners facilmente
    [SerializeField] private bool showGizmos = true;


    // =========================================================================
    //  ESTADO INTERNO
    // =========================================================================

    // Bandera que evita spawns duplicados cuando spawnOnlyOnce es true
    private bool hasSpawned = false;

    // Referencia a la sombra actualmente viva de este spawner
    // Permite destruirla o interactuar con ella desde otros sistemas
    private GameObject spawnedShadowInstance = null;


    // =========================================================================
    //  INICIALIZACION
    // =========================================================================

    private void Awake()
    {
        // Asegurarse de que el Collider sea un Trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[ShadowSpawnerTrigger] El Collider de '{gameObject.name}' no era Trigger. Se corrigio automaticamente.");
        }
    }


    // =========================================================================
    //  DETECCION DEL JUGADOR
    // =========================================================================

    private void OnTriggerEnter(Collider other)
    {
        // Solo reaccionar al jugador
        if (!other.CompareTag("Player")) return;

        // Si ya se spawnea y la limitacion esta activa, ignorar
        if (spawnOnlyOnce && hasSpawned)
        {
            Debug.Log($"[ShadowSpawnerTrigger] '{gameObject.name}': ya se spawnee una sombra. Ignorando.");
            return;
        }

        // Iniciar el spawn con el retardo configurado
        StartCoroutine(SpawnWithDelay());
    }


    // =========================================================================
    //  LOGICA DE SPAWNEO
    // =========================================================================

    /// <summary>
    /// Espera el spawnDelay configurado y luego instancia la sombra.
    /// El retardo permite que el jugador ya este dentro antes de que aparezca.
    /// </summary>
    private IEnumerator SpawnWithDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        SpawnShadow();
    }

    /// <summary>
    /// Instancia el shadowPrefab en la posicion del spawnPoint y le inyecta
    /// los waypoints de la habitacion para confinar su patrullaje.
    /// </summary>
    private void SpawnShadow()
    {
        if (shadowPrefab == null)
        {
            Debug.LogError($"[ShadowSpawnerTrigger] '{gameObject.name}': shadowPrefab no esta asignado en el Inspector.");
            return;
        }

        // Calcular posicion y rotacion finales del spawn
        Vector3    spawnPosition = GetSpawnPosition();
        Quaternion spawnRotation = GetSpawnRotation();

        // Verificar que el punto de spawn este en el NavMesh antes de instanciar
        // Evita que la sombra quede flotando o atrapada fuera de la superficie navegable
        if (!NavMesh.SamplePosition(spawnPosition, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
        {
            Debug.LogError($"[ShadowSpawnerTrigger] '{gameObject.name}': La posicion de spawn " +
                           $"{spawnPosition} no esta en el NavMesh. Reposiciona el SpawnPoint.");
            return;
        }

        // Instanciar el prefab de la sombra en la posicion valida del NavMesh
        spawnedShadowInstance = Instantiate(shadowPrefab, navHit.position, spawnRotation);
        spawnedShadowInstance.name = $"ShadowEnemy_{gameObject.name}";

        // Inyectar los waypoints de esta habitacion al ShadowEnemy recien creado
        // Esto permite que el prefab no tenga waypoints hardcodeados
        InjectRoomWaypoints(spawnedShadowInstance);

        // Disparar el efecto de humo en el punto exacto donde aparece la sombra
        if (enableSmokeEffect)
            ShadowSmokeEffect.PlayBurstAtPosition(navHit.position, smokePrefab);

        hasSpawned = true;
        Debug.Log($"[ShadowSpawnerTrigger] Sombra spawneada en {navHit.position} por '{gameObject.name}'.");
    }

    /// <summary>
    /// Transfiere los waypoints de esta habitacion al ShadowEnemy instanciado.
    /// Llama al metodo publico InjectPatrolPoints() del ShadowEnemy.
    /// </summary>
    private void InjectRoomWaypoints(GameObject shadowObj)
    {
        if (roomWaypoints == null || roomWaypoints.Length == 0)
        {
            Debug.LogWarning($"[ShadowSpawnerTrigger] '{gameObject.name}': No hay roomWaypoints asignados. " +
                             "La sombra usara los waypoints de su propio prefab (si tiene).");
            return;
        }

        ShadowEnemy shadowScript = shadowObj.GetComponent<ShadowEnemy>();
        if (shadowScript == null)
        {
            Debug.LogError($"[ShadowSpawnerTrigger] El prefab '{shadowPrefab.name}' no tiene componente ShadowEnemy.");
            return;
        }

        // Inyectar los waypoints de esta zona al script de IA
        shadowScript.InjectPatrolPoints(roomWaypoints);
        Debug.Log($"[ShadowSpawnerTrigger] Inyectados {roomWaypoints.Length} waypoints a '{shadowObj.name}'.");
    }


    // =========================================================================
    //  METODOS AUXILIARES
    // =========================================================================

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null)
            return spawnPoint.position + spawnOffset;
        return transform.position + spawnOffset;
    }

    private Quaternion GetSpawnRotation()
    {
        if (spawnPoint != null)
            return spawnPoint.rotation;
        return Quaternion.identity;
    }


    // =========================================================================
    //  METODOS PUBLICOS DE CONTROL
    // =========================================================================

    /// <summary>
    /// Permite al spawner volver a crear una sombra en la proxima entrada del jugador.
    /// Util para reactivar la zona si la sombra fue eliminada por la luz.
    /// </summary>
    public void ResetSpawner()
    {
        hasSpawned = false;
        spawnedShadowInstance = null;
        Debug.Log($"[ShadowSpawnerTrigger] '{gameObject.name}' reseteado. Listo para un nuevo spawn.");
    }

    /// <summary>
    /// Devuelve la instancia viva de la sombra de este spawner.
    /// Puede ser null si aun no se ha spawneado o si fue destruida.
    /// </summary>
    public GameObject GetSpawnedInstance() => spawnedShadowInstance;

    /// <summary>
    /// Elimina la sombra activa con efecto de disipacion.
    /// Pensado para conectarse al evento de interruptor de luz de esta habitacion.
    /// </summary>
    public void DismissActiveShadow()
    {
        if (spawnedShadowInstance == null)
        {
            Debug.Log($"[ShadowSpawnerTrigger] '{gameObject.name}': No hay sombra activa para disolver.");
            return;
        }

        ShadowEnemy shadowScript = spawnedShadowInstance.GetComponent<ShadowEnemy>();
        if (shadowScript != null)
        {
            // Usar el metodo de disipacion elegante del ShadowEnemy
            shadowScript.DismissShadow();
        }
        else
        {
            // Fallback: destruir directamente si no tiene el script
            Destroy(spawnedShadowInstance);
        }

        // Detener el humo continuo si la sombra lo tenia adjunto
        ShadowSmokeEffect smoke = spawnedShadowInstance != null
            ? spawnedShadowInstance.GetComponentInChildren<ShadowSmokeEffect>()
            : null;
        if (smoke != null)
            smoke.StopSmoke();

        spawnedShadowInstance = null;

        ResetSpawner();
    }


    // =========================================================================
    //  GIZMOS DE EDITOR
    // =========================================================================

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Icono del spawner en la escena
        Gizmos.color = hasSpawned ? new Color(1f, 0.3f, 0f, 0.5f) : new Color(0.2f, 0.8f, 0.2f, 0.5f);

        // Marcar el punto de spawn
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position + spawnOffset;
        Gizmos.DrawSphere(pos, 0.3f);
        Gizmos.DrawWireCube(transform.position, transform.localScale);

        // Linea desde el spawner hasta el punto de spawn
        if (spawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }

        // Mostrar waypoints de la habitacion
        if (roomWaypoints != null)
        {
            Gizmos.color = new Color(0.5f, 0.3f, 1f, 0.8f);
            for (int i = 0; i < roomWaypoints.Length; i++)
            {
                if (roomWaypoints[i] == null) continue;
                Gizmos.DrawSphere(roomWaypoints[i].position, 0.2f);
                if (i > 0 && roomWaypoints[i - 1] != null)
                    Gizmos.DrawLine(roomWaypoints[i - 1].position, roomWaypoints[i].position);
            }
        }
    }
}