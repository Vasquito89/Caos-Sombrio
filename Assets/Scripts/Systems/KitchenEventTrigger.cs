using UnityEngine;


public class KitchenEventTrigger : MonoBehaviour
{
    [Header("Prefabs de Poltergeist")]
    [SerializeField] private GameObject[] poltergeistObjects; // Sillas, puertas, etc.
    [SerializeField] private GameObject giantShadowPrefab;

    [Header("Configuración de Fuerza")]
    [SerializeField] private float poltergeistForceMin = 10f;
    [SerializeField] private float poltergeistForceMax = 25f;
    [SerializeField] private Vector3 forceDirection = Vector3.up;

    [Header("Sonidos")]
    [SerializeField] private AudioClip glassBreakSound;
    [SerializeField] private AudioClip screamSound;
    [SerializeField] private float soundVolume = 1f;

    [Header("Sombra Gigante")]
    [SerializeField] private Vector3 giantShadowSpawnOffset = Vector3.zero;
    [SerializeField] private float giantShadowAnxietyIncrease = 30f;

    private bool hasTriggered = false;
    private AudioSource audioSource;


    private void Start()
    {
        // Crear una AudioSource si no existe
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            TriggerKitchenEvent();
        }
    }

    private void TriggerKitchenEvent()
    {
        Debug.Log("[KitchenEventTrigger] ¡Evento de cocina activado!");

        // — Aplicar Rigidbody.AddForce a los objetos de poltergeist —
        if (poltergeistObjects != null && poltergeistObjects.Length > 0)
        {
            foreach (GameObject obj in poltergeistObjects)
            {
                if (obj != null)
                {
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                    {
                        float forceAmount = Random.Range(poltergeistForceMin, poltergeistForceMax);
                        Vector3 randomForce = forceDirection.normalized * forceAmount;

                        // Añadir variación aleatoria para que no sea predecible
                        randomForce += Random.insideUnitSphere * forceAmount * 0.3f;

                        rb.AddForce(randomForce, ForceMode.Impulse);
                        Debug.Log($"[KitchenEventTrigger] Fuerza aplicada a {obj.name}: {forceAmount:F1}");
                    }
                }
            }
        }

        // — Reproducir sonidos —
        if (audioSource != null)
        {
            // Reproducir sonido de vidrios rotos
            if (glassBreakSound != null)
                audioSource.PlayOneShot(glassBreakSound, soundVolume);

            // Reproducir grito (con un pequeño delay)
            if (screamSound != null)
                Invoke(nameof(PlayScream), 0.3f);
        }

        // — Spawnear la sombra gigante —
        if (giantShadowPrefab != null)
        {
            Vector3 spawnPosition = transform.position + giantShadowSpawnOffset;
            GameObject giantShadow = Instantiate(giantShadowPrefab, spawnPosition, Quaternion.identity);
            Debug.Log($"[KitchenEventTrigger] Sombra gigante spawneada en {spawnPosition}.");
        }

        // — Aumentar la ansiedad del jugador —
        if (AnxietySystem.Instance != null)
        {
            AnxietySystem.Instance.ModifyAnxiety(giantShadowAnxietyIncrease);
            Debug.Log($"[KitchenEventTrigger] Ansiedad aumentada por {giantShadowAnxietyIncrease}.");
        }
    }

    private void PlayScream()
    {
        if (audioSource != null && screamSound != null)
            audioSource.PlayOneShot(screamSound, soundVolume);
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        Debug.Log("[KitchenEventTrigger] Evento de cocina resetado.");
    }
}
