using UnityEngine;
using UnityEngine.UI; // Necessário para Toggle e Slider
using UnityEngine.Audio; // Necessário para AudioMixer
public class SettingsMenu : MonoBehaviour
{
// --- Definições de Áudio ---
public AudioMixer mainMixer;
public Slider masterVolumeSlider;
public Slider musicVolumeSlider;
public Slider sfxVolumeSlider;
// --- Definições de Ecrã ---
public Toggle fullscreenToggle;
// --- Painel de Controlos ---
public GameObject controlsPanel;
void Start()
{
// --- Inicialização do Ecrã ---
// Verifica o modo de ecrã atual e define o estado do toggle.
// Screen.fullScreenMode pode ser FullScreenWindow, que também é ecrã completo.
fullscreenToggle.isOn = Screen.fullScreenMode ==
FullScreenMode.ExclusiveFullScreen || Screen.fullScreenMode ==
FullScreenMode.FullScreenWindow;
// --- Inicialização do Áudio ---
SetupVolumeSliders();
// --- Inicialização dos Painéis ---
if (controlsPanel != null) controlsPanel.SetActive(false);
}
// --- MÉTODO DE CONFIGURAÇÃO DE ECRÃ (CORRIGIDO) ---
public void SetFullscreen(bool isFullscreen)
{
// Altera entre Windowed e ExclusiveFullScreen, que é o modo de ecrã completo mais
Screen.fullScreenMode = isFullscreen ? FullScreenMode.ExclusiveFullScreen :
FullScreenMode.Windowed;
}
// --- MÉTODOS DE CONFIGURAÇÃO DE ÁUDIO ---
void SetupVolumeSliders()
{
mainMixer.GetFloat("MasterVolume", out float masterDb);
masterVolumeSlider.value = DbToLinear(masterDb);
mainMixer.GetFloat("MusicVolume", out float musicDb);
musicVolumeSlider.value = DbToLinear(musicDb);
mainMixer.GetFloat("SFXVolume", out float sfxDb);
sfxVolumeSlider.value = DbToLinear(sfxDb);
}
public void SetMasterVolume(float volume) => mainMixer.SetFloat("MasterVolume",
LinearToDb(volume));
public void SetMusicVolume(float volume) => mainMixer.SetFloat("MusicVolume",
LinearToDb(volume));
public void SetSFXVolume(float volume) => mainMixer.SetFloat("SFXVolume",
LinearToDb(volume));
private float LinearToDb(float linear) => Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20;
private float DbToLinear(float db) => Mathf.Pow(10, db / 20);
// --- MÉTODO DO PAINEL DE CONTROLOS ---
public void ToggleControlsPanel()
{
if (controlsPanel != null) controlsPanel.SetActive(!controlsPanel.activeSelf);
}
}