using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class InteractionUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private TextMeshProUGUI interactionText;

    [Header("Animación")]
    [SerializeField] private float fadeSpeed = 8f;

    private CanvasGroup panelCanvasGroup;  // Para animar la opacidad del panel
    private bool shouldBeVisible = false;  // ¿Debería estar visible el prompt?

    private void Awake()
    {
        // Obtener o añadir el CanvasGroup para controlar la opacidad
        if (interactionPanel != null)
        {
            panelCanvasGroup = interactionPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = interactionPanel.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        // Asegurarse de que el panel esté oculto al iniciar el juego
        HidePrompt();

        if (interactionPanel == null)
            Debug.LogError("[InteractionUI] No se asignó el 'interactionPanel' en el Inspector.");
        if (interactionText == null)
            Debug.LogError("[InteractionUI] No se asignó el 'interactionText' en el Inspector.");
    }

    private void Update()
    {
        if (panelCanvasGroup == null) return;

        // Animar suavemente la opacidad del panel (fade in/out)
        float targetAlpha = shouldBeVisible ? 1f : 0f;
        panelCanvasGroup.alpha = Mathf.Lerp(
            panelCanvasGroup.alpha,
            targetAlpha,
            fadeSpeed * Time.deltaTime
        );
    }

    public void ShowPrompt(string prompt)
    {
        if (interactionText != null)
            interactionText.text = $"[E]  {prompt}";

        shouldBeVisible = true;
    }

    public void HidePrompt()
    {
        shouldBeVisible = false;
    }
}
