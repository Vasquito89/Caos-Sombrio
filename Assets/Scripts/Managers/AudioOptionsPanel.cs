using UnityEngine;
using UnityEngine.UI;

public class AudioOptionsPanel : MonoBehaviour
{
    [Header("Sliders de Volumen")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider fxSlider;
    [SerializeField] private Slider menuSlider;
    [SerializeField] private Slider characterSlider;

    [Header("Mute Toggle")]
    [SerializeField] private Toggle muteToggle;

    [Header("Botón de Cierre (Opcional)")]
    [SerializeField] private Button backButton;

    private void OnEnable()
    {
        // Verificar si el AudioManager existe en la escena actual
        if (AudioManager.Instance != null)
        {
            // Vinculamos cada slider pasándole el nombre exacto de su parámetro expuesto en el Mixer
            if (masterSlider) AudioManager.Instance.RegisterSlider("VolMaster", masterSlider);
            if (fxSlider) AudioManager.Instance.RegisterSlider("VolFX", fxSlider);
            if (menuSlider) AudioManager.Instance.RegisterSlider("VolMenu", menuSlider);
            if (characterSlider) AudioManager.Instance.RegisterSlider("VolCharacter", characterSlider);

            // Vinculamos el Toggle de Mute
            if (muteToggle) AudioManager.Instance.RegisterMute(muteToggle);
        }
        else
        {
            Debug.LogWarning("No se encontró una instancia de AudioManager en la escena.");
        }

        // Si usas el botón para volver atrás, guarda los cambios al hacer clic
        if (backButton)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayClickAndSave();
                gameObject.SetActive(false); // Oculta este panel
            });
        }
    }
}