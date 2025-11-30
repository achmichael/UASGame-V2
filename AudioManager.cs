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
        audioSource.volume = musicVolume;
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
        if (audioSource != null) audioSource.volume = musicVolume;
    }
}
