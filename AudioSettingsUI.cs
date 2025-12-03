using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Slider untuk mengatur volume SFX")]
    public Slider sfxSlider;
    
    [Tooltip("Toggle untuk Mute/Unmute SFX")]
    public Toggle sfxToggle;

    [Tooltip("Slider untuk mengatur volume Music")]
    public Slider musicSlider;

    [Tooltip("Toggle untuk Mute/Unmute Music")]
    public Toggle musicToggle;

    [Header("Optional: Audio Mixer")]
    [Tooltip("Jika menggunakan AudioMixer, assign di sini. Jika kosong, akan menggunakan AudioManager.")]
    public AudioMixer audioMixer;
    public string sfxParameterName = "SFXVolume";
    public string musicParameterName = "MusicVolume";

    private const string PREF_SFX_VOLUME = "SFXVolume";
    private const string PREF_SFX_MUTED = "SFXMuted";
    private const string PREF_MUSIC_VOLUME = "MusicVolume";
    private const string PREF_MUSIC_MUTED = "MusicMuted";

    void Start()
    {
        LoadSettings();

        // Setup Listeners
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (musicToggle != null)
        {
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        }
    }

    public void OnSFXVolumeChanged(float value)
    {
        // Update AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }

        // Update AudioMixer (Logarithmic conversion for Mixer)
        if (audioMixer != null)
        {
            float dbVolume = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20;
            // Jika toggle OFF, paksa mute di mixer juga (meskipun toggle handler sudah handle)
            if (sfxToggle != null && !sfxToggle.isOn) dbVolume = -80f;
            
            audioMixer.SetFloat(sfxParameterName, dbVolume);
        }

        SaveSettings();
    }

    public void OnSFXToggleChanged(bool isOn)
    {
        // Logic: Toggle ON = Suara Ada, Toggle OFF = Mute
        // Note: User request "Toggle SFX dicentang (ON) -> Suara aktif"
        
        bool isMuted = !isOn; // Jika ON, tidak muted. Jika OFF, muted.

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXMuted(isMuted);
        }

        // Jika pakai Mixer, kita bisa set volume ke -80db saat mute
        if (audioMixer != null)
        {
            if (isMuted)
            {
                audioMixer.SetFloat(sfxParameterName, -80f);
            }
            else
            {
                // Restore volume dari slider
                if (sfxSlider != null)
                {
                    float value = sfxSlider.value;
                    float dbVolume = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20;
                    audioMixer.SetFloat(sfxParameterName, dbVolume);
                }
            }
        }

        SaveSettings();
    }

    public void OnMusicVolumeChanged(float value)
    {
        // Update AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(value);
        }

        // Update AudioMixer
        if (audioMixer != null)
        {
            float dbVolume = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20;
            if (musicToggle != null && !musicToggle.isOn) dbVolume = -80f;
            
            audioMixer.SetFloat(musicParameterName, dbVolume);
        }

        SaveSettings();
    }

    public void OnMusicToggleChanged(bool isOn)
    {
        bool isMuted = !isOn;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicMuted(isMuted);
        }

        if (audioMixer != null)
        {
            if (isMuted)
            {
                audioMixer.SetFloat(musicParameterName, -80f);
            }
            else
            {
                if (musicSlider != null)
                {
                    float value = musicSlider.value;
                    float dbVolume = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20;
                    audioMixer.SetFloat(musicParameterName, dbVolume);
                }
            }
        }

        SaveSettings();
    }

    void LoadSettings()
    {
        // Default values
        float savedSFXVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f);
        bool savedSFXMuted = PlayerPrefs.GetInt(PREF_SFX_MUTED, 0) == 1;
        
        float savedMusicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, 0.6f);
        bool savedMusicMuted = PlayerPrefs.GetInt(PREF_MUSIC_MUTED, 0) == 1;

        // Apply to UI
        if (sfxSlider != null) sfxSlider.value = savedSFXVolume;
        if (sfxToggle != null) sfxToggle.isOn = !savedSFXMuted;

        if (musicSlider != null) musicSlider.value = savedMusicVolume;
        if (musicToggle != null) musicToggle.isOn = !savedMusicMuted;

        // Apply to System
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(savedSFXVolume);
            AudioManager.Instance.SetSFXMuted(savedSFXMuted);
            
            AudioManager.Instance.SetVolume(savedMusicVolume);
            AudioManager.Instance.SetMusicMuted(savedMusicMuted);
        }
        
        // Trigger update logic once to sync everything
        OnSFXVolumeChanged(savedSFXVolume);
        OnSFXToggleChanged(!savedSFXMuted);
        
        OnMusicVolumeChanged(savedMusicVolume);
        OnMusicToggleChanged(!savedMusicMuted);
    }

    void SaveSettings()
    {
        if (sfxSlider != null) PlayerPrefs.SetFloat(PREF_SFX_VOLUME, sfxSlider.value);
        if (sfxToggle != null) PlayerPrefs.SetInt(PREF_SFX_MUTED, sfxToggle.isOn ? 0 : 1);

        if (musicSlider != null) PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, musicSlider.value);
        if (musicToggle != null) PlayerPrefs.SetInt(PREF_MUSIC_MUTED, musicToggle.isOn ? 0 : 1);

        PlayerPrefs.Save();
    }
}
