using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona la navegacion entre escenas y el estado global del juego (reinicios, salida).
/// Implementa el patron Singleton para ser accesible desde cualquier controlador de UI.
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    [Header("Configuracion de Escenas")]
    [SerializeField] private string mainLevelSceneName = "Nivel1";
    [SerializeField] private string mainMenuSceneName = "menu principal";

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

    /// <summary>
    /// Vuelve a cargar el nivel principal ("Nivel1") y resetea el AnxietySystem.
    /// </summary>
    public void RestartLevel()
    {
        Debug.Log("[GameSceneManager] Reiniciando el nivel...");
        
        // Cargar la escena del nivel principal
        SceneManager.LoadScene(mainLevelSceneName);
        
        // Nota: Al recargar la escena, Unity recrea los objetos de la escena.
        // Si el AnxietySystem se destruye con la escena, se reiniciara de forma natural.
        // Si fuera persistente (DontDestroyOnLoad), tendriamos que llamar a un metodo de reset explicito.
    }

    /// <summary>
    /// Carga la escena del Menu Principal.
    /// </summary>
    public void GoToMainMenu()
    {
        Debug.Log("[GameSceneManager] Cargando Menu Principal...");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Metodo generico para cargar cualquier escena por su nombre.
    /// Util para configurar en los eventos OnClick de botones desde el Inspector.
    /// </summary>
    /// <param name="sceneName">Nombre exacto de la escena en Build Settings</param>
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

    /// <summary>
    /// Cierra la aplicacion del juego. Solo tiene efecto en compilaciones (Builds).
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[GameSceneManager] Saliendo del juego...");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}