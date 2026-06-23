using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject audioOptionsPanel;

    [Header("Botones del menú de pausa")]
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button mainMenuButton;

    private bool isPaused = false;

    void Start()
    {
        pauseMenu.SetActive(false); // Asegura que el menú de pausa esté oculto al inicio
        if (audioOptionsPanel != null) audioOptionsPanel.SetActive(false); // Asegurar que inicie oculto
        isPaused = false;

        quitButton.onClick.AddListener(OnQuit);
        mainMenuButton.onClick.AddListener(OnMainMenu);

        if (optionsButton != null && audioOptionsPanel != null)
        {
            optionsButton.onClick.AddListener(() => {
                audioOptionsPanel.SetActive(true);
            });
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isPaused)
            { 
                ResumeGame(); 
            }
            else
            { 
                PauseGame(); 
            }
        }
    }

    private void PauseGame()
    {
        isPaused = true;
        pauseMenu.SetActive(true);
        Time.timeScale = 0f; // Pausa el juego

        // IMPORTANTE: Liberar el cursor para poder hacer clic
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ResumeGame()
    {
        pauseMenu.SetActive(false);
        if (audioOptionsPanel != null) audioOptionsPanel.SetActive(false);
        Time.timeScale = 1f; // Reanuda el juego
        isPaused = false;

        // Volver a bloquear el cursor para el juego
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnQuit()
    {
        Application.Quit();
    }

    private void OnMainMenu()
    {
        Time.timeScale = 1f; // Asegura que el tiempo esté normal al cargar el menú principal
        SceneManager.LoadScene("MenuPrincipal"); 
    }
}