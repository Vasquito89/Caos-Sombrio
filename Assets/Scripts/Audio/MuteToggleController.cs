using UnityEngine;
using UnityEngine.UI;

public class MuteToggleController : MonoBehaviour
{
    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RegisterMute(GetComponent<Toggle>());
        }
    }
}