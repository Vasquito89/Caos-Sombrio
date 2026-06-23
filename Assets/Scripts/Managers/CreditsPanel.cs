using UnityEngine;
using UnityEngine.UI;

public class CreditsPanel : MonoBehaviour
{
    [SerializeField] private Button creditsButton;
    [SerializeField] private GameObject creditsPanel;


    void Start()
    {
        creditsButton.onClick.AddListener(ShowCreditsPanel);
    }

    private void ShowCreditsPanel()
    {
        creditsPanel.SetActive(false);
    }
}