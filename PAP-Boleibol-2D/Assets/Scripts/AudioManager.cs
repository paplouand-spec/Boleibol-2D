using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public Slider mainvolume;
    public Slider musicvolume;
    public Slider sfxvolume;
    public AudioMixer AudioMixer;

    public void ChangeMainVolume()
    {
        AudioMixer.SetFloat("MainVolume", mainvolume.value);
    }

    public void ChangeMusicVolume()
    {
        AudioMixer.SetFloat("MusicVolume", musicvolume.value);
    }

    public void ChangeSFXVolume()
    {
        AudioMixer.SetFloat("SFXVolume", sfxvolume.value);
    }
}
