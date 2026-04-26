using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class NoteInteractable : MonoBehaviour, IInteractable
{
    // ─────────────────────────────────────────────────────────
    //  PARÁMETROS (editables desde el Inspector)
    // ─────────────────────────────────────────────────────────

    [Header("Contenido de la Nota")]
    [SerializeField] private string noteTitle = "Nota sin título";

    [TextArea(4, 12)]
    [SerializeField] private string noteContent = "Escribe el contenido de la nota aquí...";

    [Header("Referencias UI")]
    [SerializeField] private GameObject notePanel;
    [SerializeField] private TextMeshProUGUI noteTitleText;
    [SerializeField] private TextMeshProUGUI noteBodyText;

    [Header("Control de Jugador")]
    private PlayerMovement playerMovement;

    private bool isReading = false;  

    private void Start()
    {
        if (notePanel != null)
            notePanel.SetActive(false);

        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();
    }

    private void Update()
    {
        if (isReading && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)))
        {
            CloseNote();
        }
    }

    public string GetInteractionPrompt()
    {
        return $"Leer \"{noteTitle}\"";
    }

    public void Interact(GameObject interactor)
    {
        OpenNote();
    }

    private void OpenNote()
    {
        isReading = true;

        if (notePanel != null) notePanel.SetActive(true);
        if (noteTitleText != null) noteTitleText.text = noteTitle;
        if (noteBodyText != null)  noteBodyText.text  = noteContent;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (playerMovement != null)
            playerMovement.enabled = false;

        Debug.Log($"[NoteInteractable] Nota abierta: '{noteTitle}'");
    }

    private void CloseNote()
    {
        isReading = false;

        if (notePanel != null) notePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (playerMovement != null)
            playerMovement.enabled = true;

        Debug.Log("[NoteInteractable] Nota cerrada.");
    }
}
