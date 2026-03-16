using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public Toggle fullscreenToggle;

    void Start()
    {
        // Carrega a preferência de fullscreen guardada ou define como true por padrão
        if (PlayerPrefs.HasKey("fullscreen"))
        {
            fullscreenToggle.isOn = PlayerPrefs.GetInt("fullscreen") == 1 ? true : false;
        }
        else
        {
            fullscreenToggle.isOn = Screen.fullScreen;
        }
        SetFullscreen(fullscreenToggle.isOn);

        // Adiciona um listener para quando o valor do Toggle muda
        fullscreenToggle.onValueChanged.AddListener(delegate { SetFullscreen(fullscreenToggle.isOn); });
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}