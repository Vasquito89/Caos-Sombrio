using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawn Point")]
    [SerializeField] private Transform spawnPoint;

    private void Awake()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] No se asignó el prefab del Player.");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("[PlayerSpawner] No se asignó el Spawn Point.");
            return;
        }

        // Instanciar el Player
        GameObject playerInstance = Instantiate(
            playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        playerInstance.name = "Player"; // opcional, por prolijidad
    }
}
