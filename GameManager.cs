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
    private HealthIndicator healthIndicator; // Auto-assigned health indicator

    // State Management
    public bool IsPaused { get; private set; } = false;
    private const string PauseSceneName = "Pause";
    private const string MainMenuSceneName = "Main-menu";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe ke event scene loaded untuk refresh UI references
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Unsubscribe untuk mencegah memory leak
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Dipanggil setiap kali scene baru di-load
    /// Refresh semua UI references yang mungkin sudah null
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene loaded: {scene.name}, Mode: {mode}");

        // Skip refresh jika ini adalah scene additive (seperti Pause)
        if (mode == LoadSceneMode.Additive)
        {
            return;
        }

        // Delay sedikit untuk memastikan semua object sudah terinisialisasi
        StartCoroutine(RefreshUIReferencesDelayed());
    }

    /// <summary>
    /// Refresh UI references dengan sedikit delay
    /// </summary>
    private System.Collections.IEnumerator RefreshUIReferencesDelayed()
    {
        // Tunggu 1 frame agar semua Awake() dan Start() object lain selesai
        yield return null;

        RefreshUIReferences();
        UpdateHUD();
        UpdateHealthIndicator();
    }

    /// <summary>
    /// Refresh semua references UI (HUDController, HealthIndicator)
    /// Dipanggil saat scene berganti atau manual jika diperlukan
    /// </summary>
    public void RefreshUIReferences()
    {
        // Re-find HUDController di scene baru
        hudController = FindObjectOfType<HUDController>();
        if (hudController != null)
        {
            Debug.Log("[GameManager] HUDController found and assigned!");
        }
        else
        {
            Debug.LogWarning("[GameManager] HUDController tidak ditemukan di scene ini.");
        }

        // Re-find HealthIndicator di scene baru
        healthIndicator = FindObjectOfType<HealthIndicator>();
        if (healthIndicator != null)
        {
            Debug.Log("[GameManager] HealthIndicator found and assigned!");
        }
        else
        {
            Debug.LogWarning("[GameManager] HealthIndicator tidak ditemukan di scene ini.");
        }
    }

    void Start()
    {
        int difficulty = PlayerPrefs.GetInt("Difficulty", 0); // default Easy
        Debug.Log("Game Difficulty Level: " + difficulty);
        switch (difficulty)
        {
            case 0: // Easy
                playerLives = 5;
                break;
            case 1: // Normal
                playerLives = 4;
                break;
            case 2: // Hard
                playerLives = 3;
                break;
        }

        // Try find HUD in scene if not manually assigned
        if (hudController == null)
            hudController = FindObjectOfType<HUDController>();

        // Auto-assign HealthIndicator
        if (healthIndicator == null)
        {
            healthIndicator = FindObjectOfType<HealthIndicator>();
            if (healthIndicator != null)
            {
                Debug.Log("[GameManager] HealthIndicator auto-assigned successfully!");
            }
        }

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

        // Update health indicator dengan playerLives awal
        UpdateHealthIndicator();
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

        // Check if player actually died from health depletion
        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        bool shouldLoseLife = true;

        if (ph != null)
        {
            // Only lose life if health is depleted (<= 0)
            // This prevents losing lives on simple respawns/teleports
            if (ph.GetCurrentHealth() > 0)
            {
                shouldLoseLife = false;
                Debug.Log("Respawn requested but health > 0. Not deducting life.");
            }
        }

        if (shouldLoseLife)
        {
            // Kurangi nyawa HANYA SEKALI di sini
            playerLives--;
            Debug.Log($"Player Respawning... Lives remaining: {playerLives}");

            // Update health indicator setelah kehilangan life
            UpdateHealthIndicator();
        }

        if (playerLives <= 0 && shouldLoseLife)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("Gameover");
            return;
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
            // FIX: Handle Rigidbody teleportation explicitly
            // Jika player menggunakan Rigidbody (seperti di MovementLogic), transform.position saja kadang gagal
            // terutama jika Rigidbody Interpolation aktif.
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = lastCheckpointPos;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            player.transform.position = lastCheckpointPos;
        }

        UpdateHUD();

        // Reset status player (Health, Invulnerability, IsDead flag)
        if (ph != null)
        {
            ph.OnRespawn();
        }
    }

    void TriggerNormalEnding()
    {
        SceneManager.LoadScene("TrueEnding");
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
            hudController.UpdateHUD(collectedCount, playerLives, totalCollectibles);
    }

    /// <summary>
    /// Update health indicator dengan playerLives
    /// </summary>
    void UpdateHealthIndicator()
    {
        if (healthIndicator != null)
        {
            // Gunakan ForceUpdateHealth untuk memastikan icon selalu ditampilkan
            healthIndicator.ForceUpdateHealth(playerLives);
            Debug.Log($"[GameManager] Health indicator updated: {playerLives} lives");
        }
        else
        {
            Debug.LogWarning($"[GameManager] healthIndicator is NULL, cannot update health UI");
        }
    }

    void Update()
    {
        // Handle Pause Input Global di sini
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (IsPaused) return;

        // Load scene additive
        SceneManager.LoadScene(PauseSceneName, LoadSceneMode.Additive);

        Time.timeScale = 0f;
        IsPaused = true;

        // Unlock cursor agar bisa klik tombol
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Pause background music while paused
        if (AudioManager.Instance != null)
            AudioManager.Instance.PauseMusic();
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;

        // Unload scene pause jika ada
        // Cek apakah scene pause benar-benar loaded sebelum unload untuk menghindari error
        SceneManager.UnloadSceneAsync(PauseSceneName);

        Time.timeScale = 1f;
        IsPaused = false;

        // Kembalikan cursor ke state semula (misal terkunci untuk FPS/TPS)
        // Sesuaikan dengan kebutuhan game Anda, biasanya locked saat gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Resume background music when game resumes
        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeMusic();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene(MainMenuSceneName);
    }
}