using UnityEngine;
using UnityEngine.UI;


public class GameOverUIController : MonoBehaviour
{
    [Header("UI Componentes")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Botones de Control")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    private void Start()
    {
        // El panel de GameOver debe iniciar desactivado
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Suscribirse a los clicks de los botones
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        // Suscribirse al evento de GameOver global en AnxietySystem
        if (AnxietySystem.Instance != null)
        {
            AnxietySystem.Instance.onGameOver.AddListener(ShowGameOverScreen);
        }
        else
        {
            Debug.LogWarning("[GameOverUIController] AnxietySystem no encontrado. Asegurate de que exista en la escena.");
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse del evento al destruir para evitar fugas de memoria
        if (AnxietySystem.Instance != null)
        {
            AnxietySystem.Instance.onGameOver.RemoveListener(ShowGameOverScreen);
        }
    }

    
    public void ShowGameOverScreen()
    {
        Debug.Log("[GameOverUIController] Activando pantalla de GameOver.");
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // Bloquear cursor para que el jugador pueda interactuar con la UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Detener el tiempo de juego si es necesario (opcional)
        Time.timeScale = 0f;
    }

    
    private void OnRetryClicked()
    {
        // Reanudar el tiempo de juego si se habia pausado
        Time.timeScale = 1f;

        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.RestartLevel();
        }
        else
        {
            // Fallback de carga directa
            UnityEngine.SceneManagement.SceneManager.LoadScene("Nivel1");
        }
    }

    
    private void OnMainMenuClicked()
    {
        // Reanudar el tiempo de juego si se habia pausado
        Time.timeScale = 1f;

        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.GoToMainMenu();
        }
        else
        {
            // Fallback de carga directa
            UnityEngine.SceneManagement.SceneManager.LoadScene("MenuPrincipal");
        }
    }
}