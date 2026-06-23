using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button instructiveButton;
    [SerializeField] private Button audioButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button creditsButton;

    [Header("Paneles")]
    [SerializeField] private GameObject creditsPanel;

    private void Start()
    {
        creditsPanel.SetActive(false); // Asegura que el panel de créditos esté oculto al inicio

        playButton.onClick.AddListener(StartMatchScene);
        instructiveButton.onClick.AddListener(StartInstructiveScene);
        audioButton.onClick.AddListener(StartAudioScene);
        closeButton.onClick.AddListener(OnExit);
        creditsButton.onClick.AddListener(StarCredits);
    }



    private void OnExit() => Application.Quit();

    // --- Transiciones de escena ---
    private void StartMatchScene()
    {
        SceneManager.LoadScene("Nivel1");
    }

    private void StartInstructiveScene()
    {
        SceneManager.LoadScene("Instructive");
    }

    private void StartAudioScene()
    {
        SceneManager.LoadScene("Audio");
    }

    private void StarCredits()
    {
        creditsPanel.SetActive(true);
    }
}
