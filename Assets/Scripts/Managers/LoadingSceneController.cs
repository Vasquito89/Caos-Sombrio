using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI")]
    public Slider progressSlider;
    public TextMeshProUGUI loadingText;
    public GameObject pressAnyKeyPanel;
    public GameObject loadingGamePanel;

    [Header("Settings")]
    public float minLoadTime = 4f;   
    public string nextSceneName = "MenuPrincipal";

    private bool canContinue = false;

    private void Start()
    {
        pressAnyKeyPanel.SetActive(false);
        progressSlider.value = 0f;
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        float timer = 0f;

        // -------------------------------------------------------------
        // 1) Inicializar DB
        // -------------------------------------------------------------
        loadingText.text = "Cargando";
        progressSlider.value = 0.5f;


        // -------------------------------------------------------------
        // 2) Simular progreso hasta 60%
        // -------------------------------------------------------------
        while (progressSlider.value < 0.6f)
        {
            progressSlider.value += Time.deltaTime * 0.25f;
            yield return null;
        }

        // -------------------------------------------------------------
        // 3) Cargar escena siguiente en segundo plano
        // -------------------------------------------------------------
        loadingText.text = "Cargando";
        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);

            float target = Mathf.Lerp(0.6f, 1f, progress);
            progressSlider.value = target;

            timer += Time.deltaTime;

            // No dejamos continuar hasta tiempo m�nimo
            if (progress >= 1f && timer >= minLoadTime)
            {
                break;
            }

            yield return null;
        }

        // -------------------------------------------------------------
        // 4) Mostrar pulse cualquier tecla
        // -------------------------------------------------------------
        loadingGamePanel.SetActive(false);
        loadingText.text = "Carga completa";
        pressAnyKeyPanel.SetActive(true);
        canContinue = true;

        // Esperar que el usuario presione una tecla
        while (canContinue)
        {
            if (Input.anyKeyDown)
            {
                op.allowSceneActivation = true;
                yield break;
            }
            yield return null;
        }
    }
}
