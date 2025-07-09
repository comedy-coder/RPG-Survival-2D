using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SimpleCombatUI_TMP : MonoBehaviour
{
    [Header("Combat Status")]
    public TextMeshProUGUI combatStatusText; // Changed to TextMeshPro
    public Image combatIndicator;
    public Color inCombatColor = Color.red;
    public Color outOfCombatColor = Color.green;
    
    [Header("Weapon UI")]
    public TextMeshProUGUI weaponNameText; // Changed to TextMeshPro
    public TextMeshProUGUI weaponDamageText; // Changed to TextMeshPro
    public TextMeshProUGUI weaponSpeedText; // Changed to TextMeshPro
    public Slider weaponDurabilitySlider;
    public TextMeshProUGUI weaponDurabilityText; // Changed to TextMeshPro
    public Image weaponIcon;
    
    [Header("Target Info")]
    public GameObject targetInfoPanel;
    public TextMeshProUGUI targetNameText; // Changed to TextMeshPro
    public Slider targetHealthSlider;
    public TextMeshProUGUI targetHealthText; // Changed to TextMeshPro
    public Image targetIcon;
    
    [Header("Combat Log")]
    public GameObject combatLogPanel;
    public TextMeshProUGUI combatLogText; // Changed to TextMeshPro
    public ScrollRect combatLogScroll;
    public bool showCombatLog = true;
    public int maxLogEntries = 50;
    
    [Header("Crosshair")]
    public GameObject crosshair;
    public Image crosshairImage;
    public Color normalCrosshairColor = Color.white;
    public Color enemyCrosshairColor = Color.red;
    
    // Simple state tracking
    private bool isInCombat = false;
    private bool isUIVisible = true;
    private List<string> combatLogEntries = new List<string>();
    private Camera mainCamera;
    
    // Test values
    private float testHealth = 100f;
    private float testMaxHealth = 100f;
    private string currentWeapon = "Fists";
    private int weaponDamage = 5;
    private float weaponSpeed = 1.0f;
    private float weaponDurability = 100f;
    
    #region Initialization
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<Camera>();
        
        SetupUI();
        
        Debug.Log("🎯 Simple Combat UI (TextMeshPro) initialized");
    }
    
    void SetupUI()
    {
        // Hide target info initially
        if (targetInfoPanel != null)
        {
            targetInfoPanel.SetActive(false);
        }
        
        // Setup combat log
        if (combatLogPanel != null)
        {
            combatLogPanel.SetActive(showCombatLog);
        }
        
        // Enable crosshair
        if (crosshair != null)
        {
            crosshair.SetActive(true);
        }
        
        // Initial states
        UpdateCombatStatusDisplay("READY", false);
        UpdateWeaponDisplay();
        
        AddToCombatLog("Combat system ready");
    }
    
    #endregion
    
    #region Update
    
    void Update()
    {
        if (!isUIVisible) return;
        
        UpdateCrosshair();
        HandleInput();
    }
    
    void HandleInput()
    {
        // Test controls
        if (Input.GetKeyDown(KeyCode.F))
        {
            TestStartCombat();
        }
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            TestEndCombat();
        }
        
        if (Input.GetKeyDown(KeyCode.X))
        {
            TestTakeDamage();
        }
        
        if (Input.GetKeyDown(KeyCode.H))
        {
            TestHeal();
        }
        
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TestSwitchWeapon();
        }
        
        if (Input.GetKeyDown(KeyCode.L))
        {
            ToggleCombatLog();
        }
        
        if (Input.GetKeyDown(KeyCode.U))
        {
            ToggleUI();
        }
    }
    
    #endregion
    
    #region Combat Status
    
    void UpdateCombatStatusDisplay(string status, bool inCombat)
    {
        if (combatStatusText != null)
            combatStatusText.text = status;
            
        if (combatIndicator != null)
            combatIndicator.color = inCombat ? inCombatColor : outOfCombatColor;
        
        isInCombat = inCombat;
    }
    
    #endregion
    
    #region Weapon UI
    
    void UpdateWeaponDisplay()
    {
        if (weaponNameText != null)
            weaponNameText.text = currentWeapon;
            
        if (weaponDamageText != null)
            weaponDamageText.text = $"DMG: {weaponDamage}";
            
        if (weaponSpeedText != null)
            weaponSpeedText.text = $"SPD: {weaponSpeed:F1}";
            
        if (weaponDurabilitySlider != null)
            weaponDurabilitySlider.value = weaponDurability / 100f;
            
        if (weaponDurabilityText != null)
            weaponDurabilityText.text = $"{weaponDurability:F0}/100";
    }
    
    #endregion
    
    #region Crosshair
    
    void UpdateCrosshair()
    {
        if (crosshair == null || crosshairImage == null || mainCamera == null) return;
        
        // Position crosshair at mouse
        Vector3 mousePos = Input.mousePosition;
        crosshair.transform.position = mousePos;
        
        // Simple color change based on combat state
        crosshairImage.color = isInCombat ? enemyCrosshairColor : normalCrosshairColor;
    }
    
    #endregion
    
    #region Combat Log
    
    void AddToCombatLog(string entry)
    {
        if (!showCombatLog) return;
        
        // Add timestamp
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string logEntry = $"[{timestamp}] {entry}";
        
        combatLogEntries.Add(logEntry);
        
        // Limit log size
        if (combatLogEntries.Count > maxLogEntries)
        {
            combatLogEntries.RemoveAt(0);
        }
        
        // Update log text
        UpdateCombatLogDisplay();
        
        Debug.Log($"🔥 Combat: {entry}");
    }
    
    void UpdateCombatLogDisplay()
    {
        if (combatLogText == null) return;
        
        string logText = "";
        foreach (string entry in combatLogEntries)
        {
            logText += entry + "\n";
        }
        
        combatLogText.text = logText;
        
        // Scroll to bottom
        if (combatLogScroll != null)
        {
            combatLogScroll.verticalNormalizedPosition = 0f;
        }
    }
    
    void ToggleCombatLog()
    {
        showCombatLog = !showCombatLog;
        
        if (combatLogPanel != null)
        {
            combatLogPanel.SetActive(showCombatLog);
        }
        
        Debug.Log($"Combat log: {(showCombatLog ? "ON" : "OFF")}");
    }
    
    #endregion
    
    #region UI Management
    
    void ToggleUI()
    {
        isUIVisible = !isUIVisible;
        
        // Toggle UI elements
        if (combatStatusText != null) combatStatusText.gameObject.SetActive(isUIVisible);
        if (weaponNameText != null) weaponNameText.gameObject.SetActive(isUIVisible);
        if (targetInfoPanel != null && !isUIVisible) targetInfoPanel.SetActive(false);
        if (combatLogPanel != null && !isUIVisible) combatLogPanel.SetActive(false);
        if (crosshair != null) crosshair.SetActive(isUIVisible);
        
        Debug.Log($"Combat UI: {(isUIVisible ? "ON" : "OFF")}");
    }
    
    #endregion
    
    #region Test Functions
    
    void TestStartCombat()
    {
        UpdateCombatStatusDisplay("IN COMBAT", true);
        AddToCombatLog("Combat started!");
    }
    
    void TestEndCombat()
    {
        UpdateCombatStatusDisplay("READY", false);
        AddToCombatLog("Combat ended");
    }
    
    void TestTakeDamage()
    {
        float damage = Random.Range(10f, 25f);
        testHealth -= damage;
        if (testHealth < 0) testHealth = 0;
        
        AddToCombatLog($"Player took {damage:F0} damage! Health: {testHealth:F0}/{testMaxHealth:F0}");
        
        if (!isInCombat)
        {
            TestStartCombat();
        }
    }
    
    void TestHeal()
    {
        float heal = Random.Range(15f, 30f);
        testHealth += heal;
        if (testHealth > testMaxHealth) testHealth = testMaxHealth;
        
        AddToCombatLog($"Player healed for {heal:F0}! Health: {testHealth:F0}/{testMaxHealth:F0}");
    }
    
    void TestSwitchWeapon()
    {
        string[] weapons = { "Fists", "Sword", "Axe", "Bow", "Hammer" };
        int[] damages = { 5, 25, 35, 20, 30 };
        float[] speeds = { 1.2f, 0.8f, 0.6f, 1.0f, 0.7f };
        
        int index = Random.Range(0, weapons.Length);
        currentWeapon = weapons[index];
        weaponDamage = damages[index];
        weaponSpeed = speeds[index];
        weaponDurability = Random.Range(50f, 100f);
        
        UpdateWeaponDisplay();
        AddToCombatLog($"Equipped {currentWeapon}");
    }
    
    #endregion
    
    #region Integration Methods (for future use)
    
    public void SetCombatState(bool inCombat)
    {
        if (inCombat)
            TestStartCombat();
        else
            TestEndCombat();
    }
    
    public void SetPlayerHealth(float current, float max)
    {
        testHealth = current;
        testMaxHealth = max;
    }
    
    public void SetWeaponInfo(string name, int damage, float speed, float durability)
    {
        currentWeapon = name;
        weaponDamage = damage;
        weaponSpeed = speed;
        weaponDurability = durability;
        UpdateWeaponDisplay();
    }
    
    public void ShowTargetInfo(string name, float health, float maxHealth)
    {
        if (targetInfoPanel != null)
        {
            targetInfoPanel.SetActive(true);
            
            if (targetNameText != null)
                targetNameText.text = name;
                
            if (targetHealthSlider != null)
                targetHealthSlider.value = health / maxHealth;
                
            if (targetHealthText != null)
                targetHealthText.text = $"{health:F0}/{maxHealth:F0}";
        }
    }
    
    public void HideTargetInfo()
    {
        if (targetInfoPanel != null)
        {
            targetInfoPanel.SetActive(false);
        }
    }
    
    #endregion
    
    #region Debug
    
   // REPLACE THIS METHOD IN SimpleCombatUI_TMP.cs

void OnGUI()
{
    // Position at bottom-right corner
    float panelWidth = 300f;
    float panelHeight = 300f;
    float margin = 10f;
    
    float posX = Screen.width - panelWidth - margin;
    float posY = Screen.height - panelHeight - margin;
    
    GUILayout.BeginArea(new Rect(posX, posY, panelWidth, panelHeight));
    GUILayout.Box("Simple Combat UI Test (TMP)");
    
    GUILayout.Label($"Health: {testHealth:F0}/{testMaxHealth:F0}");
    GUILayout.Label($"Weapon: {currentWeapon}");
    GUILayout.Label($"Damage: {weaponDamage}");
    GUILayout.Label($"In Combat: {isInCombat}");
    
    if (GUILayout.Button("Start Combat (F)"))
    {
        TestStartCombat();
    }
    
    if (GUILayout.Button("End Combat (G)"))
    {
        TestEndCombat();
    }
    
    if (GUILayout.Button("Take Damage (X)"))
    {
        TestTakeDamage();
    }
    
    if (GUILayout.Button("Heal (H)"))
    {
        TestHeal();
    }
    
    if (GUILayout.Button("Switch Weapon (Q)"))
    {
        TestSwitchWeapon();
    }
    
    if (GUILayout.Button("Toggle Log (L)"))
    {
        ToggleCombatLog();
    }
    
    if (GUILayout.Button("Toggle UI (U)"))
    {
        ToggleUI();
    }
    
    GUILayout.EndArea();
}
    
    #endregion
}