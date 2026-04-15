using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    private const string MainVolumeKey = "MainVolume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SFXVolume";

    public Slider mainvolume;
    public Slider musicvolume;
    public Slider sfxvolume;
    public AudioMixer AudioMixer;

    private void Awake()
    {
        LoadVolume(mainvolume, MainVolumeKey);
        LoadVolume(musicvolume, MusicVolumeKey);
        LoadVolume(sfxvolume, SfxVolumeKey);
    }

    private void OnDisable()
    {
        PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    public void ChangeMainVolume()
    {
        UpdateVolume(mainvolume, MainVolumeKey);
    }

    public void ChangeMusicVolume()
    {
        UpdateVolume(musicvolume, MusicVolumeKey);
    }

    public void ChangeSFXVolume()
    {
        UpdateVolume(sfxvolume, SfxVolumeKey);
    }

    private void LoadVolume(Slider slider, string volumeKey)
    {
        float defaultValue = slider != null ? slider.value : 0f;
        float savedValue = PlayerPrefs.GetFloat(volumeKey, defaultValue);

        if (slider != null)
        {
            savedValue = Mathf.Clamp(savedValue, slider.minValue, slider.maxValue);
            slider.SetValueWithoutNotify(savedValue);
        }

        if (AudioMixer != null)
        {
            AudioMixer.SetFloat(volumeKey, savedValue);
        }
    }

    private void UpdateVolume(Slider slider, string volumeKey)
    {
        if (slider == null)
        {
            return;
        }

        float sliderValue = slider.value;

        if (AudioMixer != null)
        {
            AudioMixer.SetFloat(volumeKey, sliderValue);
        }

        PlayerPrefs.SetFloat(volumeKey, sliderValue);
    }
}
