using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Health Indicator UI System
/// Menampilkan icon health (hati) sesuai jumlah health player
/// Script ini modular dan dapat dipanggil dari sistem health manapun
/// </summary>
public class HealthIndicator : MonoBehaviour
{
    [Header("Icon Settings")]
    [Tooltip("Prefab icon health (UI Image dengan sprite hati)")]
    public GameObject iconPrefab;
    
    [Header("Panel Settings")]
    [Tooltip("Parent panel tempat icon akan di-spawn")]
    public Transform parentPanel;
    
    [Header("Spacing Settings")]
    [Tooltip("Jarak antar icon (jika tidak menggunakan Layout Group)")]
    public float iconSpacing = 10f;
    
    [Header("Debug")]
    [Tooltip("Tampilkan log debug untuk troubleshooting")]
    public bool showDebugLog = false;
    
    // Private variables
    private List<GameObject> healthIcons = new List<GameObject>();
    private int currentDisplayedHealth = 0;
    
    #region Unity Lifecycle
    
    void Start()
    {
        // Validasi di Start
        ValidateReferences();
        
        if (showDebugLog)
        {
            Debug.Log("[HealthIndicator] Initialized successfully");
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Update health indicator dengan jumlah health terbaru
    /// Method utama yang dipanggil dari sistem health lain
    /// </summary>
    /// <param name="currentHealth">Jumlah health saat ini (akan generate sejumlah icon ini)</param>
    public void UpdateHealth(int currentHealth)
    {
        // Validasi input
        if (currentHealth < 0)
        {
            Debug.LogWarning("[HealthIndicator] Health tidak boleh negatif. Setting ke 0.");
            currentHealth = 0;
        }
        
        // Cek apakah ada perubahan
        if (currentHealth == currentDisplayedHealth)
        {
            if (showDebugLog)
            {
                Debug.Log($"[HealthIndicator] Health tidak berubah ({currentHealth}). Skip update.");
            }
            return;
        }
        
        // Validasi references
        if (!ValidateReferences())
        {
            Debug.LogError("[HealthIndicator] Cannot update health - missing references!");
            return;
        }
        
        // Clear icon lama
        ClearAllIcons();
        
        // Generate icon baru
        GenerateHealthIcons(currentHealth);
        
        // Update tracker
        currentDisplayedHealth = currentHealth;
        
        if (showDebugLog)
        {
            Debug.Log($"[HealthIndicator] Health updated to: {currentHealth}");
        }
    }
    
    /// <summary>
    /// Update health dengan nilai float (otomatis di-round)
    /// </summary>
    public void UpdateHealth(float currentHealth)
    {
        UpdateHealth(Mathf.RoundToInt(currentHealth));
    }
    
    /// <summary>
    /// Tambah health sebanyak amount
    /// </summary>
    public void AddHealth(int amount)
    {
        UpdateHealth(currentDisplayedHealth + amount);
    }
    
    /// <summary>
    /// Kurangi health sebanyak amount
    /// </summary>
    public void RemoveHealth(int amount)
    {
        UpdateHealth(currentDisplayedHealth - amount);
    }
    
    /// <summary>
    /// Reset semua icon health
    /// </summary>
    public void ResetHealth()
    {
        ClearAllIcons();
        currentDisplayedHealth = 0;
    }
    
    /// <summary>
    /// Get current displayed health
    /// </summary>
    public int GetCurrentHealth()
    {
        return currentDisplayedHealth;
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Validasi semua references yang diperlukan
    /// </summary>
    private bool ValidateReferences()
    {
        bool isValid = true;
        
        if (iconPrefab == null)
        {
            Debug.LogError("[HealthIndicator] Icon Prefab belum di-assign di Inspector!");
            isValid = false;
        }
        
        if (parentPanel == null)
        {
            Debug.LogError("[HealthIndicator] Parent Panel belum di-assign di Inspector!");
            isValid = false;
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Hapus semua icon yang sudah di-spawn
    /// </summary>
    private void ClearAllIcons()
    {
        // Destroy semua icon dari list
        foreach (GameObject icon in healthIcons)
        {
            if (icon != null)
            {
                Destroy(icon);
            }
        }
        
        // Clear list
        healthIcons.Clear();
        
        if (showDebugLog)
        {
            Debug.Log("[HealthIndicator] All icons cleared");
        }
    }
    
    /// <summary>
    /// Generate icon health sejumlah amount
    /// </summary>
    private void GenerateHealthIcons(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // Instantiate icon sebagai child dari parent panel
            GameObject newIcon = Instantiate(iconPrefab, parentPanel);
            
            // Pastikan scale benar (kadang prefab punya scale aneh)
            newIcon.transform.localScale = Vector3.one;
            
            // Optional: Set nama untuk debugging
            newIcon.name = $"HealthIcon_{i + 1}";
            
            // Tambahkan ke list
            healthIcons.Add(newIcon);
        }
        
        if (showDebugLog)
        {
            Debug.Log($"[HealthIndicator] Generated {amount} health icons");
        }
    }
    
    #endregion
    
    #region Editor Helper
    
    /// <summary>
    /// Helper untuk setup di Editor
    /// </summary>
    [ContextMenu("Auto Find Parent Panel")]
    private void AutoFindParentPanel()
    {
        if (parentPanel == null)
        {
            parentPanel = transform;
            Debug.Log("[HealthIndicator] Parent Panel set to this GameObject");
        }
    }
    
    /// <summary>
    /// Test generate 5 icons
    /// </summary>
    [ContextMenu("Test - Generate 5 Hearts")]
    private void TestGenerate5()
    {
        UpdateHealth(5);
    }
    
    /// <summary>
    /// Test generate 3 icons
    /// </summary>
    [ContextMenu("Test - Generate 3 Hearts")]
    private void TestGenerate3()
    {
        UpdateHealth(3);
    }
    
    /// <summary>
    /// Test clear semua
    /// </summary>
    [ContextMenu("Test - Clear All")]
    private void TestClear()
    {
        ResetHealth();
    }
    
    #endregion
}
