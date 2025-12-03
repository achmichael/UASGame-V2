using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Background Music")]
    public AudioClip musicClip;
    [Tooltip("Auto play music on Start")]
    public bool playOnStart = true;
    [Range(0f,1f)] public float musicVolume = 0.6f;
    public bool musicMuted = false;

    [Header("SFX Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    public bool sfxMuted = false;

    AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        
        // Load saved settings immediately
        LoadSettings();
        
        // Apply initial volume
        UpdateMusicVolume();
    }

    void LoadSettings()
    {
        // Load Music Settings
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.6f);
        musicMuted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
        
        // Load SFX Settings
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        sfxMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
    }

    void Start()
    {
        if (musicClip != null && playOnStart)
        {
            PlayMusic();
        }
    }

    public void PlayMusic()
    {
        if (audioSource == null) return;
        if (musicClip != null && audioSource.clip != musicClip)
            audioSource.clip = musicClip;

        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    public void PauseMusic()
    {
        if (audioSource == null) return;
        if (audioSource.isPlaying)
            audioSource.Pause();
    }

    public void ResumeMusic()
    {
        if (audioSource == null) return;
        if (!audioSource.isPlaying && audioSource.clip != null)
            audioSource.UnPause();
    }

    public void StopMusic()
    {
        if (audioSource == null) return;
        audioSource.Stop();
    }

    public void SetVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        UpdateMusicVolume();
    }

    public void SetMusicMuted(bool muted)
    {
        musicMuted = muted;
        UpdateMusicVolume();
    }

    private void UpdateMusicVolume()
    {
        if (audioSource != null)
        {
            audioSource.volume = musicMuted ? 0f : musicVolume;
        }
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
    }

    public void SetSFXMuted(bool muted)
    {
        sfxMuted = muted;
    }

    /// <summary>
    /// Play SFX at position with current global SFX volume
    /// </summary>
    public void PlaySFX(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        float finalVolume = sfxMuted ? 0f : sfxVolume;
        
        // Don't spawn AudioSource if volume is 0
        if (finalVolume > 0.01f)
        {
            AudioSource.PlayClipAtPoint(clip, position, finalVolume);
        }
    }
}
