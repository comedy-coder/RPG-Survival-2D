using UnityEngine;
using System.Collections;

public class SimpleCombatIntegration : MonoBehaviour
{
    [Header("Test Settings")]
    public KeyCode testDamageKey = KeyCode.X;
    public KeyCode testHealKey = KeyCode.C;
    public KeyCode spawnEnemyKey = KeyCode.E;
    public KeyCode showStatusKey = KeyCode.V;
    
    [Header("Settings")]
    public float testDamageAmount = 25f;
    public float testHealAmount = 50f;
    public float enemySpawnDistance = 5f;
    
    private PlayerHealth playerHealth;
    private CombatManager combatManager;
    private bool isInitialized = false;
    
    void Start()
    {
        StartCoroutine(InitializeDelayed());
    }
    
    IEnumerator InitializeDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        
        InitializeComponents();
    }
    
    void InitializeComponents()
    {
        Debug.Log("🔗 === SIMPLE COMBAT INTEGRATION ===");
        
        // Find PlayerHealth
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            Debug.Log("✅ PlayerHealth found and connected");
        }
        else
        {
            Debug.LogError("❌ PlayerHealth not found!");
        }
        
        // Find CombatManager
        combatManager = FindObjectOfType<CombatManager>();
        if (combatManager != null)
        {
            Debug.Log("✅ CombatManager found and connected");
        }
        else
        {
            Debug.LogWarning("⚠️ CombatManager not found - will work without it");
        }
        
        // Setup player for combat
        SetupPlayerForCombat();
        
        isInitialized = true;
        Debug.Log("✅ Simple Combat Integration completed!");
    }
    
    void SetupPlayerForCombat()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("❌ Player GameObject not found!");
            return;
        }
        
        // Ensure player has PlayerHealth
        if (playerHealth == null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = player.AddComponent<PlayerHealth>();
                Debug.Log("✅ PlayerHealth added to player");
            }
        }
        
        // Ensure player has collider
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            Debug.Log("✅ Collider added to player");
        }
        
        Debug.Log("✅ Player setup completed for combat");
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        // Handle input
        if (Input.GetKeyDown(testDamageKey))
        {
            TestPlayerDamage();
        }
        
        if (Input.GetKeyDown(testHealKey))
        {
            TestPlayerHeal();
        }
        
        if (Input.GetKeyDown(spawnEnemyKey))
        {
            SpawnTestEnemy();
        }
        
        if (Input.GetKeyDown(showStatusKey))
        {
            ShowPlayerStatus();
        }
    }
    
    void TestPlayerDamage()
    {
        Debug.Log("🧪 === MANUAL DAMAGE TEST ===");
        
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth not found!");
            return;
        }
        
        float healthBefore = playerHealth.GetCurrentHealth();
        Debug.Log($"Health before damage: {healthBefore}");
        
        playerHealth.TakeDamage(testDamageAmount);
        
        float healthAfter = playerHealth.GetCurrentHealth();
        Debug.Log($"Health after damage: {healthAfter}");
        
        if (healthBefore != healthAfter)
        {
            Debug.Log("✅ Player damage test successful!");
        }
        else
        {
            Debug.LogError("❌ Player damage test failed!");
        }
    }
    
    void TestPlayerHeal()
    {
        Debug.Log("💚 === MANUAL HEAL TEST ===");
        
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth not found!");
            return;
        }
        
        float healthBefore = playerHealth.GetCurrentHealth();
        Debug.Log($"Health before heal: {healthBefore}");
        
        playerHealth.Heal(testHealAmount);
        
        float healthAfter = playerHealth.GetCurrentHealth();
        Debug.Log($"Health after heal: {healthAfter}");
        
        Debug.Log("✅ Player heal test completed!");
    }
    
    void SpawnTestEnemy()
    {
        Debug.Log("👹 === SPAWN TEST ENEMY ===");
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("❌ Player not found!");
            return;
        }
        
        // Create enemy
        GameObject enemy = new GameObject("TestEnemy");
        enemy.transform.position = player.transform.position + Vector3.right * enemySpawnDistance;
        enemy.tag = "Enemy";
        
        // Add visual
        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.color = Color.red;
        sr.sprite = CreateSquareSprite();
        
        // Add collider
        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        
        // Add EnemyAI
        EnemyAI enemyAI = enemy.AddComponent<EnemyAI>();
        enemyAI.attackDamage = 20f;
        enemyAI.attackRange = 2f;
        enemyAI.detectionRange = 6f;
        enemyAI.moveSpeed = 2f;
        enemyAI.attackCooldown = 1.5f;
        
        Debug.Log($"✅ Test enemy spawned at {enemy.transform.position}");
    }
    
    void ShowPlayerStatus()
    {
        Debug.Log("📊 === PLAYER STATUS ===");
        
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth not found!");
            return;
        }
        
        Debug.Log($"Health: {playerHealth.GetCurrentHealth():F1}/{playerHealth.GetMaxHealth():F1}");
        Debug.Log($"Health %: {playerHealth.GetHealthPercentage() * 100:F1}%");
        Debug.Log($"Is Dead: {playerHealth.IsDead()}");
        Debug.Log($"Can Take Damage: {playerHealth.CanTakeDamage()}");
        
        if (combatManager != null)
        {
            Debug.Log($"In Combat: {combatManager.IsInCombat}");
        }
    }
    
    Sprite CreateSquareSprite()
    {
        // Create a simple square sprite
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
    
    // Manual test methods
    [ContextMenu("Test Player Damage")]
    public void ManualTestDamage()
    {
        TestPlayerDamage();
    }
    
    [ContextMenu("Test Player Heal")]
    public void ManualTestHeal()
    {
        TestPlayerHeal();
    }
    
    [ContextMenu("Spawn Enemy")]
    public void ManualSpawnEnemy()
    {
        SpawnTestEnemy();
    }
    
    [ContextMenu("Show Player Status")]
    public void ManualShowStatus()
    {
        ShowPlayerStatus();
    }
    
    [ContextMenu("Reset Player Health")]
    public void ResetPlayerHealth()
    {
        if (playerHealth != null)
        {
            playerHealth.FullHeal();
            Debug.Log("✅ Player health reset to full");
        }
    }
    
    void OnGUI()
    {
        // Show controls on screen
        GUI.Label(new Rect(10, 10, 300, 20), $"Press {testDamageKey} - Test Damage");
        GUI.Label(new Rect(10, 30, 300, 20), $"Press {testHealKey} - Test Heal");
        GUI.Label(new Rect(10, 50, 300, 20), $"Press {spawnEnemyKey} - Spawn Enemy");
        GUI.Label(new Rect(10, 70, 300, 20), $"Press {showStatusKey} - Show Status");
        
        // Show player health
        if (playerHealth != null)
        {
            GUI.Label(new Rect(10, 100, 300, 20), $"Player HP: {playerHealth.GetCurrentHealth():F0}/{playerHealth.GetMaxHealth():F0}");
        }
        
        // Show integration status
        GUI.Label(new Rect(10, 120, 300, 20), $"Integration: {(isInitialized ? "✅ Ready" : "⏳ Loading...")}");
    }
}