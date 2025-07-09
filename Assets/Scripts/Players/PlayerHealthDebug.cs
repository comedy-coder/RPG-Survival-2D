using UnityEngine;

public class PlayerDamageDebug : MonoBehaviour
{
    [Header("Debug Settings")]
    public KeyCode testDamageKey = KeyCode.X;
    public float testDamageAmount = 25f;
    
    void Update()
    {
        if (Input.GetKeyDown(testDamageKey))
        {
            TestPlayerDamage();
        }
        
        if (Input.GetKeyDown(KeyCode.V))
        {
            CheckPlayerHealth();
        }
    }
    
    void TestPlayerDamage()
    {
        Debug.Log("🧪 === MANUAL DAMAGE TEST ===");
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("❌ Player not found with tag 'Player'");
            return;
        }
        
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth component not found on player!");
            return;
        }
        
        Debug.Log($"Before damage: {playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}");
        
        playerHealth.TakeDamage(testDamageAmount);
        
        Debug.Log($"After damage: {playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}");
        
        if (playerHealth.GetCurrentHealth() < 100f)
        {
            Debug.Log("✅ PlayerHealth.TakeDamage() is working!");
        }
        else
        {
            Debug.LogError("❌ PlayerHealth.TakeDamage() is NOT working!");
        }
    }
    
    void CheckPlayerHealth()
    {
        Debug.Log("🔍 === PLAYER HEALTH CHECK ===");
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("❌ Player GameObject not found!");
            return;
        }
        
        Debug.Log($"✅ Player found: {player.name}");
        Debug.Log($"Player tag: {player.tag}");
        Debug.Log($"Player position: {player.transform.position}");
        
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth component missing!");
            return;
        }
        
        Debug.Log($"✅ PlayerHealth component found");
        Debug.Log($"Current Health: {playerHealth.GetCurrentHealth()}");
        Debug.Log($"Max Health: {playerHealth.GetMaxHealth()}");
        Debug.Log($"Is Dead: {playerHealth.IsDead()}");
        Debug.Log($"Can Take Damage: {playerHealth.CanTakeDamage()}");
        
        // Check for other health components
        Component[] healthComponents = player.GetComponents<Component>();
        Debug.Log($"All components on player:");
        foreach (Component comp in healthComponents)
        {
            Debug.Log($"  - {comp.GetType().Name}");
        }
    }
    
    [ContextMenu("Force Player Damage")]
    public void ForcePlayerDamage()
    {
        TestPlayerDamage();
    }
    
    [ContextMenu("Check Player Setup")]
    public void CheckPlayerSetup()
    {
        CheckPlayerHealth();
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 20), $"Press {testDamageKey} to test damage");
        GUI.Label(new Rect(10, 30, 200, 20), "Press V to check player health");
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                GUI.Label(new Rect(10, 50, 200, 20), $"Player HP: {health.GetCurrentHealth():F0}/{health.GetMaxHealth():F0}");
            }
        }
    }
}