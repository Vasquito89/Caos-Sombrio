using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CloseButtonController : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "MenuPrincipal";

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClickAndSave();
        SceneManager.LoadScene(sceneToLoad);
    }
}
