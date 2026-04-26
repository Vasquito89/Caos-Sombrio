using UnityEngine;


public class AnxietyStimulus : MonoBehaviour
{
    [Header("Efecto sobre la Ansiedad")]
    [SerializeField] private float anxietyPerSecond = 8f;
    [SerializeField] private float rationalizationPerSecond = 15f;
    [SerializeField] private bool isActive = true;

    [Header("Comportamiento al Racionalizar")]
    [SerializeField] private bool deactivateOnRationalize = false;
    [SerializeField] private float reactivationDelay = 8f;

    [Header("Visual")]
    [SerializeField] private Renderer stimulusRenderer;
    [SerializeField] private Color normalColor = new Color(0.2f, 0f, 0.3f, 0.6f);
    [SerializeField] private Color rationalizedColor = new Color(0.8f, 0.8f, 1f, 0.3f);


    private bool playerIsInRange = false;     // ¿El jugador está dentro del trigger?
    private bool isBeingRationalized = false; // ¿La linterna apunta a este objeto ahora?
    private float reactivationTimer = 0f;     // Temporizador de reactivación

    private void Start()
    {
        // Aplicar color inicial
        if (stimulusRenderer != null)
            stimulusRenderer.material.color = normalColor;
    }


    private void Update()
    {
        // Manejar el temporizador de reactivación
        if (!isActive && reactivationDelay > 0f && !deactivateOnRationalize)
        {
            reactivationTimer -= Time.deltaTime;
            if (reactivationTimer <= 0f)
            {
                isActive = true;
                Debug.Log($"[AnxietyStimulus] '{gameObject.name}' reactivado.");
            }
        }

        // Aplicar efecto de ansiedad si el jugador está en rango y el estímulo está activo
        if (playerIsInRange && isActive && AnxietySystem.Instance != null)
        {
            if (isBeingRationalized)
            {
                // La linterna apunta aquí: bajar ansiedad (racionalización activa)
                AnxietySystem.Instance.ModifyAnxiety(-rationalizationPerSecond * Time.deltaTime);
            }
            else
            {
                // Sin linterna: subir ansiedad (amenaza activa)
                AnxietySystem.Instance.ModifyAnxiety(anxietyPerSecond * Time.deltaTime);
            }
        }

        // Resetear el estado de racionalización cada frame
        // (FlashlightController lo vuelve a activar si sigue apuntando)
        isBeingRationalized = false;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInRange = true;
            Debug.Log($"[AnxietyStimulus] Jugador entró en el rango de '{gameObject.name}'.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInRange = false;
            Debug.Log($"[AnxietyStimulus] Jugador salió del rango de '{gameObject.name}'.");
        }
    }


    public void Rationalize()
    {
        if (!isActive) return;

        isBeingRationalized = true;

        // Cambiar color visual para indicar que está siendo racionalizado
        if (stimulusRenderer != null)
            stimulusRenderer.material.color = rationalizedColor;

        // Si está configurado para desactivarse al ser alumbrado
        if (deactivateOnRationalize)
        {
            isActive = false;
            Debug.Log($"[AnxietyStimulus] '{gameObject.name}' desactivado por racionalización.");
        }
        else if (reactivationDelay > 0f)
        {
            // Desactivar temporalmente e iniciar temporizador de reactivación
            isActive = false;
            reactivationTimer = reactivationDelay;

            // Restaurar color normal cuando se reactivará
            if (stimulusRenderer != null)
                stimulusRenderer.material.color = normalColor;
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }

    public bool PlayerIsInRange => playerIsInRange;

    public bool IsActive => isActive;

    public bool IsBeingRationalized => isBeingRationalized;
}
