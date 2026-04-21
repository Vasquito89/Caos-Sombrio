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

    private void Start()
    {
        playButton.onClick.AddListener(StartMatchScene);
        instructiveButton.onClick.AddListener(StartInstructiveScene);
        audioButton.onClick.AddListener(StartAudioScene);
        closeButton.onClick.AddListener(OnExit);
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
}
