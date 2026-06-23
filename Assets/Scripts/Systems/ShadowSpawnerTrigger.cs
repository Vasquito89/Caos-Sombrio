using System.Collections;
using UnityEngine;
using UnityEngine.AI;



[RequireComponent(typeof(Collider))]
public class ShadowSpawnerTrigger : MonoBehaviour
{
    // =========================================================================
    //  CONFIGURACION EN INSPECTOR
    // =========================================================================

    [Header("Prefab a Instanciar")]
    [SerializeField] private GameObject shadowPrefab;

    [Header("Punto de Aparicion")]
    [SerializeField] private Transform spawnPoint;

    [Header("Configuracion de Spawneo")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [SerializeField] private bool spawnOnlyOnce = false;
    [SerializeField] private float spawnDelay = 0.5f;

    [Header("Waypoints del Piso/Habitacion")]
    [SerializeField] private Transform[] roomWaypoints;

    [Header("Efecto de Humo al Spawn")]
    [SerializeField] private GameObject smokePrefab;
    [SerializeField] private bool enableSmokeEffect = true;

    [Header("Debug")]
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
        Debug.Log("Spawned instance =" + spawnedShadowInstance);
        // 1. Solo reaccionar al jugador
        if (!other.CompareTag("Player")) return;

        // 2. Si ya hay una sombra viva controlada por este spawner, NO hacer nada
        Debug.Log($"Spawned instance = {spawnedShadowInstance}");
        Debug.Log($"Has spawned = {hasSpawned}");
        if (spawnedShadowInstance != null) return;

        // 3. Si está configurado para un solo uso y ya cumplió su ciclo, ignorar
        if (spawnOnlyOnce && hasSpawned)
        {
            Debug.Log($"[ShadowSpawnerTrigger] '{gameObject.name}': ya se spawneo una sombra en esta sesion. Ignorando.");
            return;
        }

        // Iniciar el spawn de manera segura
        StartCoroutine(SpawnWithDelay());
    }


    // =========================================================================
    //  LOGICA DE SPAWNEO
    // =========================================================================

   
    private IEnumerator SpawnWithDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        SpawnShadow();
    }

    
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

    
    public void ResetSpawner()
    {
        hasSpawned = false;
        spawnedShadowInstance = null;
        Debug.Log($"[ShadowSpawnerTrigger] '{gameObject.name}' reseteado. Listo para un nuevo spawn.");
    }

    
    public GameObject GetSpawnedInstance() => spawnedShadowInstance;

    
    public void DismissActiveShadow()
    {
        Debug.Log(spawnedShadowInstance == null ? $"[ShadowSpawnerTrigger] '{gameObject.name}': No hay sombra activa para disolver." : $"[ShadowSpawnerTrigger] '{gameObject.name}': Disolviendo sombra activa.");
        Debug.Log(hasSpawned ? $"[ShadowSpawnerTrigger] '{gameObject.name}': hasSpawned = true." : $"[ShadowSpawnerTrigger] '{gameObject.name}': hasSpawned = false.");

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

        spawnedShadowInstance = null;
        hasSpawned = false;
        Debug.Log($"[ShadowSpawnerTrigger] Sombra enviada a disolver. Spawner reseteado con exito.");
    

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