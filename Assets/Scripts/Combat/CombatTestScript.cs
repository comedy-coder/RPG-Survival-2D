using UnityEngine;

public class CombatTest : MonoBehaviour
{
    [Header("Test Settings")]
    public KeyCode testDamageKey = KeyCode.X;
    public KeyCode testHealKey = KeyCode.C;
    public KeyCode spawnEnemyKey = KeyCode.E;
    public float testDamageAmount = 25f;
    public float testHealAmount = 50f;
    
    [Header("Enemy Spawn")]
    public GameObject enemyPrefab;
    public float spawnDistance = 5f;
    
    void Update()
    {
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
        
        if (Input.GetKeyDown(KeyCode.V))
        {
            ShowPlayerStatus();
        }
    }
    
    void TestPlayerDamage()
    {
        Debug.Log("🧪 === MANUAL DAMAGE TEST ===");
        
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth not found!");
            return;
        }
        
        Debug.Log($"Before damage: {playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}");
        
        playerHealth.TakeDamage(testDamageAmount);
        
        Debug.Log($"After damage: {playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}");
        Debug.Log("✅ Manual damage test completed!");
    }
    
    void TestPlayerHeal()
    {
        Debug.Log("💚 === MANUAL HEAL TEST ===");
        
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth not found!");
            return;
        }
        
        Debug.Log($"Before heal: {playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}");
        
        playerHealth.Heal(testHealAmount);
        
        Debug.Log($"After heal: {playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}");
        Debug.Log("✅ Manual heal test completed!");
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
        
        // Create enemy manually if no prefab
        if (enemyPrefab == null)
        {
            CreateTestEnemy(player.transform.position);
        }
        else
        {
            Vector3 spawnPos = player.transform.position + Vector3.right * spawnDistance;
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }
        
        Debug.Log("✅ Test enemy spawned!");
    }
    
    void CreateTestEnemy(Vector3 playerPosition)
    {
        GameObject enemy = new GameObject("TestEnemy");
        enemy.transform.position = playerPosition + Vector3.right * spawnDistance;
        enemy.tag = "Enemy";
        
        // Add visual
        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.color = Color.red;
        
        // Add collider
        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        
        // Add AI
        EnemyAI ai = enemy.AddComponent<EnemyAI>();
        
        Debug.Log($"✅ Created test enemy at {enemy.transform.position}");
    }
    
    void ShowPlayerStatus()
    {
        Debug.Log("📊 === PLAYER STATUS ===");
        
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth not found!");
            return;
        }
        
        Debug.Log($"Health: {playerHealth.GetCurrentHealth():F1}/{playerHealth.GetMaxHealth():F1}");
        Debug.Log($"Health %: {playerHealth.GetHealthPercentage() * 100:F1}%");
        Debug.Log($"Is Dead: {playerHealth.IsDead()}");
        Debug.Log($"Can Take Damage: {playerHealth.CanTakeDamage()}");
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), $"Press {testDamageKey} - Test Damage");
        GUI.Label(new Rect(10, 30, 300, 20), $"Press {testHealKey} - Test Heal");
        GUI.Label(new Rect(10, 50, 300, 20), $"Press {spawnEnemyKey} - Spawn Enemy");
        GUI.Label(new Rect(10, 70, 300, 20), "Press V - Show Player Status");
        
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            GUI.Label(new Rect(10, 100, 300, 20), $"Player HP: {playerHealth.GetCurrentHealth():F0}/{playerHealth.GetMaxHealth():F0}");
        }
    }
}