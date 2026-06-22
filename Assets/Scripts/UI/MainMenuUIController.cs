using UnityEngine;

/// <summary>
/// Controla la interfaz del Menu Principal, gestionando la activacion y desactivacion
/// de los paneles de Opciones, Instructivo y Creditos (para mostrar los datos del examen).
/// </summary>
public class MainMenuUIController : MonoBehaviour
{
    [Header("Paneles de la Interfaz")]
    // Referencia al Panel de Opciones (ej. configuracion de audio o graficos)
    [SerializeField] private GameObject optionsPanel;
    
    // Referencia al Panel de Instructivo / Tutorial
    [SerializeField] private GameObject instructivePanel;
    
    // Referencia al Panel de Creditos (donde se muestra la informacion de los integrantes)
    [SerializeField] private GameObject creditsPanel;

    private void Start()
    {
        // Asegurarse de que los paneles comiencen desactivados
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (instructivePanel != null) instructivePanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    /// <summary>
    /// Activa el panel de creditos y desactiva los demas paneles abiertos.
    /// Aqui se mostrara: "Damian Exequiel Garnica" y "Surenio Producciones".
    /// </summary>
    public void OpenCreditsPanel()
    {
        Debug.Log("[MainMenuUIController] Mostrando Panel de Creditos.");
        if (creditsPanel != null) creditsPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (instructivePanel != null) instructivePanel.SetActive(false);
    }

    /// <summary>
    /// Activa el panel de opciones y desactiva los demas.
    /// </summary>
    public void OpenOptionsPanel()
    {
        Debug.Log("[MainMenuUIController] Mostrando Panel de Opciones.");
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (instructivePanel != null) instructivePanel.SetActive(false);
    }

    /// <summary>
    /// Activa el panel de instructivo y desactiva los demas.
    /// </summary>
    public void OpenInstructivePanel()
    {
        Debug.Log("[MainMenuUIController] Mostrando Panel de Instructivo.");
        if (instructivePanel != null) instructivePanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    /// <summary>
    /// Cierra todos los paneles secundarios, volviendo a la vista principal del menu.
    /// </summary>
    public void CloseAllPanels()
    {
        Debug.Log("[MainMenuUIController] Cerrando todos los paneles.");
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (instructivePanel != null) instructivePanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    // =========================================================================
    //  ATAJOS PARA BOTONES DIRECTOS (Delega en GameSceneManager)
    // =========================================================================

    public void PlayGame()
    {
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.RestartLevel();
        }
        else
        {
            // Fallback directo si no se ha inicializado el Singleton en escena todavia
            UnityEngine.SceneManagement.SceneManager.LoadScene("Nivel1");
        }
    }

    public void QuitGame()
    {
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.QuitGame();
        }
        else
        {
            Application.Quit();
        }
    }
}