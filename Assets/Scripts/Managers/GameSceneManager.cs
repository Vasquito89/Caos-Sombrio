using UnityEngine;
using UnityEngine.SceneManagement;


public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    [Header("Configuracion de Escenas")]
    [SerializeField] private string mainLevelSceneName = "Nivel1";
    [SerializeField] private string mainMenuSceneName = "MenuPrincipal";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void RestartLevel()
    {
        Debug.Log("[GameSceneManager] Reiniciando el nivel...");
        
        // Cargar la escena del nivel principal
        SceneManager.LoadScene(mainLevelSceneName);
        

    }


    public void GoToMainMenu()
    {
        Debug.Log("[GameSceneManager] Cargando Menu Principal...");
        SceneManager.LoadScene(mainMenuSceneName);
    }


    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[GameSceneManager] Intentando cargar una escena con nombre vacio o nulo.");
            return;
        }
        Debug.Log($"[GameSceneManager] Cargando escena: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }


    public void QuitGame()
    {
        Debug.Log("[GameSceneManager] Saliendo del juego...");
        Application.Quit();
        

    }
}