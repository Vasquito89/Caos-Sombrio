using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance; // Instancia estática para acceso rápido

    [Header("Mixer Configuration")]
    [SerializeField] private AudioMixer mixer;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource fxSource;
    [SerializeField] private AudioClip clickSound;

    private float lastMasterVolume;

    private void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPreferences();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- Public Methods for Prefabs ---

    public void RegisterSlider(string volumeType, Slider slider)
    {
        // Sincronizar el slider con el valor actual del Mixer
        float currentVal;
        mixer.GetFloat(volumeType, out currentVal);
        slider.value = currentVal;

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener((val) => {
            mixer.SetFloat(volumeType, val);

            // Si es Master, movemos visualmente los demás
            if (volumeType == "VolMaster") SynchronizeAllSliders(val);

            PlayerPrefs.SetFloat(volumeType, val);
        });
    }

    public void RegisterMute(Toggle toggle)
    {
        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener((isOn) => {
            if (isOn)
            {
                mixer.GetFloat("VolMaster", out lastMasterVolume);
                mixer.SetFloat("VolMaster", -80);
            }
            else
            {
                mixer.SetFloat("VolMaster", lastMasterVolume);
            }
        });
    }

    public void PlayClickAndSave()
    {
        if (fxSource && clickSound) fxSource.PlayOneShot(clickSound);
        PlayerPrefs.Save();
    }

    // --- Internal Logic ---

    private void SynchronizeAllSliders(float value)
    {
        VolumeSliderController[] sliders = Object.FindObjectsByType<VolumeSliderController>(FindObjectsSortMode.None);
        foreach (var s in sliders)
        {
            s.GetComponent<Slider>().value = value;
        }
    }

    private void LoadPreferences()
    {
        mixer.SetFloat("VolMaster", PlayerPrefs.GetFloat("VolMaster", 0));
        mixer.SetFloat("VolFX", PlayerPrefs.GetFloat("VolFX", 0));
        mixer.SetFloat("VolMenu", PlayerPrefs.GetFloat("VolMenu", 0));
        mixer.SetFloat("VolCharacter", PlayerPrefs.GetFloat("VolCharacter", 0));
    }
}