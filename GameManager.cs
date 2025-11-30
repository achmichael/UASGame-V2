// GameManager.cs
// Mengatur logika global game: checkpoint, collectible, respawn, dan kondisi kemenangan
// - Singleton pattern
// - Simpan checkpoint di PlayerPrefs untuk autosave
// - Integrasi HUDController untuk update UI

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Collectibles")]
    public int collectedCount = 0;
    public int totalCollectibles = 5;

    [Header("Player")]
    public int playerLives = 3;
    private Vector3 lastCheckpointPos;

    [Header("References")]
    public HUDController hudController;
    public GameObject ghostPrefab; // Assign Ghost prefab di Inspector

    private bool isPaused = false;
    private bool isPauseSceneLoaded = false;
    private const string PauseScene = "Pause";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {

        int difficulty = PlayerPrefs.GetInt("Difficulty", 0); // default Easy
        Debug.Log("Game Difficulty Level: " + difficulty);
        switch (difficulty)
        {
            case 0: // Easy
                playerLives = 5;
                // totalCollectibles = 30;
                // Activate 1 ghosts only
                ActivateGhosts(1);
                break;
            case 1: // Normal
                playerLives = 4;
                // Activate 2 ghosts
                ActivateGhosts(2);
                break;
            case 2: // Hard
                playerLives = 3;
                // Activate 3 ghosts
                ActivateGhosts(3);
                break;
        }

        // Try find HUD in scene if not manually assigned
        if (hudController == null)
            hudController = FindObjectOfType<HUDController>();

        // load last checkpoint if present
        if (PlayerPrefs.HasKey("CheckpointX"))
        {
            lastCheckpointPos = new Vector3(
                PlayerPrefs.GetFloat("CheckpointX"),
                PlayerPrefs.GetFloat("CheckpointY"),
                PlayerPrefs.GetFloat("CheckpointZ")
            );
        }
        else
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                lastCheckpointPos = playerObj.transform.position;
        }

        UpdateHUD();

        // Start background music if AudioManager exists
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic();
        }
    }

    void ActivateGhosts(int count)
    {
        GameObject[] ghosts = GameObject.FindGameObjectsWithTag("Ghost");
        Debug.Log("Found " + ghosts.Length + " ghosts in scene, need " + count);
        
        // Jika ghost di scene kurang dari yang dibutuhkan, spawn ghost baru
        if (ghostPrefab != null && ghosts.Length < count)
        {
            int needed = count - ghosts.Length;
            Debug.Log("Spawning " + needed + " additional ghosts...");
            
            // Spawn posisi default - sesuaikan dengan map Anda
            Vector3[] spawnPositions = new Vector3[]
            {
                new Vector3(10, 1, 10),
                new Vector3(-10, 1, 10),
                new Vector3(10, 1, -10),
                new Vector3(-10, 1, -10)
            };
            
            for (int i = 0; i < needed && i < spawnPositions.Length; i++)
            {
                GameObject newGhost = Instantiate(ghostPrefab, spawnPositions[i], Quaternion.identity);
                newGhost.tag = "Ghost";
                newGhost.name = "Ghost_" + (ghosts.Length + i + 1);
            }
            
            // Refresh ghost list setelah spawn
            ghosts = GameObject.FindGameObjectsWithTag("Ghost");
        }
        
        // Activate/deactivate ghosts sesuai difficulty
        Debug.Log("Activating " + count + " ghosts out of " + ghosts.Length);
        for (int i = 0; i < ghosts.Length; i++)
        {
            ghosts[i].SetActive(i < count);
        }
    }

    public void AddCollectedItem()
    {
        collectedCount++;
        UpdateHUD();

        if (collectedCount >= totalCollectibles)
        {
            // Semua lembaran terkumpul → trigger ending
            Invoke(nameof(TriggerNormalEnding), 1.5f);
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        lastCheckpointPos = position;
        PlayerPrefs.SetFloat("CheckpointX", position.x);
        PlayerPrefs.SetFloat("CheckpointY", position.y);
        PlayerPrefs.SetFloat("CheckpointZ", position.z);
        PlayerPrefs.Save();

        Debug.Log("Checkpoint tersimpan di: " + position);
    }

    public void RespawnPlayer(GameObject player)
    {
        // Jika player null, cari berdasarkan tag
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("Tidak menemukan objek player untuk respawn.");
                return;
            }
        }

        // Teleport player ke last checkpoint
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            // disable controller temporarily to avoid collision/overlap issues
            cc.enabled = false;
            player.transform.position = lastCheckpointPos;
            cc.enabled = true;
        }
        else
        {
            player.transform.position = lastCheckpointPos;
        }

        playerLives--;

        if (playerLives <= 0)
        {
            SceneManager.LoadScene("GameOverScene");
        }
        else
        {
            UpdateHUD();
        }
    }

    void TriggerNormalEnding()
    {
        // Find CutsceneController and play normal ending if present
        // CutsceneController cs = FindObjectOfType<CutsceneController>();
        // if (cs != null)
        //     cs.PlayNormalEnding();
        // else
        //     SceneManager.LoadScene("NormalEndingScene");
    }

    public void TriggerSecretEnding()
    {
        // CutsceneController cs = FindObjectOfType<CutsceneController>();
        // if (cs != null)
        //     cs.PlaySecretEnding();
        // else
        //     SceneManager.LoadScene("SecretEndingScene");
    }

    void UpdateHUD()
    {
        if (hudController != null)
            hudController.UpdateHUD(collectedCount, playerLives);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void ResumeGame()
    {
        if (!isPauseSceneLoaded) return;
        SceneManager.UnloadSceneAsync(PauseScene);
        Time.timeScale = 1f;
        isPaused = false;
        isPauseSceneLoaded = false;

        // Resume background music when game resumes
        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeMusic();
    }

    public void PauseGame()
    {
        if (isPauseSceneLoaded) return;
        SceneManager.LoadScene(PauseScene, LoadSceneMode.Additive);
        Time.timeScale = 0f;
        isPaused = true;
        isPauseSceneLoaded = true;

        // Pause background music while paused
        if (AudioManager.Instance != null)
            AudioManager.Instance.PauseMusic();
    }
}