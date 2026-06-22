using System;
using UnityEngine;


/// <summary>
/// Trigger de escape ubicado en la puerta principal del edificio.
/// Permanece inactivo durante la fase normal del juego.
/// Solo se activa en el Paso 6 de la secuencia del Climax Final,
/// cuando el jugador necesita escapar como nueva condicion de victoria real.
/// Requiere un Collider con "Is Trigger" activado en el mismo GameObject.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExitTrigger : MonoBehaviour
{
    // =========================================================================
    //  EVENTO - Victoria real por escape
    // =========================================================================

    /// <summary>
    /// Disparado cuando el jugador cruza el trigger de escape exitosamente.
    /// Los suscriptores deben activar la pantalla de victoria real y detener el juego.
    /// </summary>
    public static event Action OnPlayerEscaped;


    // =========================================================================
    //  CONFIGURACION
    // =========================================================================

    [Header("Exit Trigger Configuration")]
    // Tag del jugador para filtrar las colisiones del trigger
    [SerializeField] private string playerTag = "Player";

    // Indicador visual/sonoro que se activa cuando el trigger se habilita (opcional)
    [SerializeField] private GameObject exitIndicator;


    // =========================================================================
    //  ESTADO INTERNO
    // =========================================================================

    // El trigger comienza desactivado. Solo AlteredPerceptionManager puede activarlo.
    private bool isActive = false;


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


    // =========================================================================
    //  METODO PUBLICO DE ACTIVACION
    // =========================================================================

    /// <summary>
    /// Activa el trigger de escape. Llamado por AlteredPerceptionManager en el Paso 6.
    /// Una vez activo, el primer jugador que lo cruce gana la partida.
    /// </summary>
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

        Debug.Log("[ExitTrigger] El jugador cruzo la puerta de escape. VICTORIA REAL!");

        // Disparar el evento de victoria real
        OnPlayerEscaped?.Invoke();

        // Desactivar el trigger para evitar multiples disparos
        isActive = false;
        GetComponent<Collider>().enabled = false;
    }
}