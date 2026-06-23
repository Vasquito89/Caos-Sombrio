using System;
using UnityEngine;



[RequireComponent(typeof(Collider))]
public class ExitTrigger : MonoBehaviour
{
    // =========================================================================
    //  EVENTO - Victoria real por escape
    // =========================================================================

    
    public static event Action OnPlayerEscaped;


    // =========================================================================
    //  CONFIGURACION
    // =========================================================================

    [Header("Exit Trigger Configuration")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject exitIndicator;


    // =========================================================================
    //  ESTADO INTERNO
    // =========================================================================

    // El trigger comienza desactivado. Solo AlteredPerceptionManager puede activarlo.
    private bool isActive = false;

    // Referencia cacheada al AlteredPerceptionManager para el gate check
    private AlteredPerceptionManager _apmCache;


    // =========================================================================
    //  INICIALIZACION
    // =========================================================================

    private void Awake()
    {
        // El trigger inicia completamente inactivo: el collider no dispara eventos
        GetComponent<Collider>().enabled = false;

        // Ocultar el indicador visual de salida hasta que se active
        if (exitIndicator != null)
            exitIndicator.SetActive(false);
    }

    private void Start()
    {
        // Cachear referencia una sola vez
        _apmCache = FindAnyObjectByType<AlteredPerceptionManager>();

        if (_apmCache == null)
            Debug.LogWarning("[ExitTrigger] No se encontro AlteredPerceptionManager en la escena. " +
                             "La victoria real no podra verificarse.");
    }


    // =========================================================================
    //  METODO PUBLICO DE ACTIVACION
    // =========================================================================

    
    public void Activate()
    {
        if (isActive) return;

        isActive = true;
        GetComponent<Collider>().enabled = true;

        // Mostrar indicador visual de salida (por ejemplo, una flecha o resplandor en la puerta)
        if (exitIndicator != null)
            exitIndicator.SetActive(true);

        Debug.Log("[ExitTrigger] Trigger de escape ACTIVADO. La puerta principal es la salvacion.");
    }


    // =========================================================================
    //  DETECCION DE CRUCE DEL JUGADOR
    // =========================================================================

    private void OnTriggerEnter(Collider other)
    {
        // Solo reaccionar si el trigger esta activo y el colisionador es el jugador
        if (!isActive) return;
        if (!other.CompareTag(playerTag)) return;

        // GATE: verificar que la falsa victoria haya completado su secuencia
        if (_apmCache == null || !_apmCache.FakeVictoryCompleted)
        {
            Debug.LogWarning("[ExitTrigger] El jugador cruzo la puerta pero FakeVictoryCompleted es false. " +
                             "La victoria real NO se otorga.");
            return;
        }

        Debug.Log("[ExitTrigger] El jugador cruzo la puerta de escape. VICTORIA REAL!");

        // Disparar el evento de victoria real
        OnPlayerEscaped?.Invoke();

        // Desactivar el trigger para evitar multiples disparos
        isActive = false;
        GetComponent<Collider>().enabled = false;
    }
}