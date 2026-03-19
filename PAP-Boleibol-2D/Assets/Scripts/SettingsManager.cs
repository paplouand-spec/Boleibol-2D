using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic; // Para usar List<T>
using System.Linq; // Para usar .Distinct() e .ToList()
using TMPro; // Para usar TMP_Dropdown
public class SettingsMenu : MonoBehaviour
{
// --- Definições de Áudio ---
public AudioMixer mainMixer;
public Slider masterVolumeSlider;
public Slider musicVolumeSlider;
public Slider sfxVolumeSlider;
// --- Definições de Ecrã ---
public TMP_Dropdown displayModeDropdown;
public TMP_Dropdown resolutionDropdown;
private Resolution[] resolutions;
// --- Painel de Controlos ---
public GameObject controlsPanel;
void Start()
{
// --- Inicialização do Ecrã ---
SetupResolutionDropdown();
SetupDisplayModeDropdown();
// --- Inicialização do Áudio ---
SetupVolumeSliders();
// --- Inicialização dos Painéis ---
if (controlsPanel != null) controlsPanel.SetActive(false);
}
// --- MÉTODOS DE CONFIGURAÇÃO DE ECRÃ ---
void SetupResolutionDropdown()
{
resolutions = Screen.resolutions.Select(res => new Resolution { width = res.width,
height = res.height }).Distinct().ToArray();
resolutionDropdown.ClearOptions();
List<string> options = new List<string>();
int currentResolutionIndex = 0;
for (int i = 0; i < resolutions.Length; i++)
{
string option = resolutions[i].width + " x " + resolutions[i].height;
options.Add(option);
if (resolutions[i].width == Screen.currentResolution.width &&
resolutions[i].height == Screen.currentResolution.height)
{
currentResolutionIndex = i;
}
}
resolutionDropdown.AddOptions(options);
resolutionDropdown.value = currentResolutionIndex;
resolutionDropdown.RefreshShownValue();
}
void SetupDisplayModeDropdown()
{
// O valor do enum FullScreenMode corresponde ao índice do nosso dropdown
// 0 = ExclusiveFullScreen, 1 = FullScreenWindow (Borderless), 3 = Windowed
// Como o nosso dropdown é 0, 1, 2, precisamos de uma pequena conversão.
int currentModeIndex = 0;
if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
currentModeIndex = 1;
if (Screen.fullScreenMode == FullScreenMode.Windowed) currentModeIndex = 2;
displayModeDropdown.value = currentModeIndex;
displayModeDropdown.RefreshShownValue();
}
public void SetResolution(int resolutionIndex)
{
Resolution resolution = resolutions[resolutionIndex];
Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
}
public void SetDisplayMode(int modeIndex)
{
FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
if (modeIndex == 1) mode = FullScreenMode.FullScreenWindow; // Janela sem Bordas
if (modeIndex == 2) mode = FullScreenMode.Windowed; // Janela
Screen.fullScreenMode = mode;
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