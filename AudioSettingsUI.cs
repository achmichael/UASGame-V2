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

    [Header("Optional: Audio Mixer")]
    [Tooltip("Jika menggunakan AudioMixer, assign di sini. Jika kosong, akan menggunakan AudioManager.")]
    public AudioMixer audioMixer;
    public string sfxParameterName = "SFXVolume";

    private const string PREF_SFX_VOLUME = "SFXVolume";
    private const string PREF_SFX_MUTED = "SFXMuted";

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

    void LoadSettings()
    {
        // Default values
        float savedVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f);
        bool savedMuted = PlayerPrefs.GetInt(PREF_SFX_MUTED, 0) == 1;

        // Apply to UI
        if (sfxSlider != null)
        {
            sfxSlider.value = savedVolume;
        }

        if (sfxToggle != null)
        {
            // Toggle ON = Suara Ada (Not Muted)
            sfxToggle.isOn = !savedMuted;
        }

        // Apply to System
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(savedVolume);
            AudioManager.Instance.SetSFXMuted(savedMuted);
        }
        
        // Trigger update logic once to sync everything
        OnSFXVolumeChanged(savedVolume);
        OnSFXToggleChanged(!savedMuted);
    }

    void SaveSettings()
    {
        if (sfxSlider != null)
        {
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, sfxSlider.value);
        }

        if (sfxToggle != null)
        {
            // Simpan state Muted (Toggle OFF = Muted = 1)
            PlayerPrefs.SetInt(PREF_SFX_MUTED, sfxToggle.isOn ? 0 : 1);
        }

        PlayerPrefs.Save();
    }
}
