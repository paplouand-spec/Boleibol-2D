using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private Toggle fullscreenToggle;
    
    private void Start()
    {
        // Carrega a preferência guardada (ou usa o padrão do sistema)
        fullscreenToggle.isOn = Screen.fullScreen;
        
        // Subscreve ao evento de mudança
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
    }
    
    private void OnFullscreenToggled(bool isFullscreen)
    {
        // Muda o modo de fullscreen
        Screen.fullScreen = isFullscreen;
        
        // Guarda a preferência do jogador
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
        
        Debug.Log($"Fullscreen: {isFullscreen}");
    }
    
    private void OnDestroy()
    {
        // Remove o listener quando o objeto é destruído
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggled);
    }
}