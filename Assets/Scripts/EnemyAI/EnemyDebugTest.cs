using UnityEngine;

public class EnemyDebugFix : MonoBehaviour
{
    [Header("Enemy Debug")]
    public bool enableDebug = true;
    public float debugAttackRange = 1.5f;
    public float debugAttackDamage = 20f;
    public float debugAttackCooldown = 1.5f;
    
    private Transform playerTransform;
    private float lastAttackTime = 0f;
    
    void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            Debug.Log($"🤖 Enemy Debug: Player found at {playerTransform.position}");
        }
        else
        {
            Debug.LogError("❌ Enemy Debug: Player not found!");
        }
    }
    
    void Update()
    {
        if (!enableDebug || playerTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Auto attack if player is close
        if (distanceToPlayer <= debugAttackRange)
        {
            if (Time.time - lastAttackTime >= debugAttackCooldown)
            {
                AttackPlayer();
                lastAttackTime = Time.time;
            }
        }
        
        // Manual attack test with F key
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("🔫 Manual enemy attack triggered!");
            AttackPlayer();
        }
    }
    
    void AttackPlayer()
    {
        if (playerTransform == null) return;
        
        Debug.Log($"🔥 === ENEMY ATTACK ATTEMPT ===");
        Debug.Log($"Enemy: {gameObject.name}");
        Debug.Log($"Player: {playerTransform.name}");
        
        // Method 1: Try PlayerHealth component
        PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            Debug.Log("✅ Found PlayerHealth component!");
            Debug.Log($"Player health before attack: {playerHealth.GetCurrentHealth()}");
            
            // Attack with attacker reference
            playerHealth.TakeDamage(debugAttackDamage, gameObject);
            
            Debug.Log($"Player health after attack: {playerHealth.GetCurrentHealth()}");
            Debug.Log("✅ Enemy attack via PlayerHealth completed!");
        }
        else
        {
            Debug.LogError("❌ PlayerHealth component not found on player!");
        }
        
        // Method 2: Try CombatEntity component
        CombatEntity playerCombat = playerTransform.GetComponent<CombatEntity>();
        if (playerCombat != null)
        {
            Debug.Log("✅ Found CombatEntity component!");
            Debug.Log($"Player combat health before: {playerCombat.currentHealth}");
            
            playerCombat.TakeDamage((int)debugAttackDamage);
            
            Debug.Log($"Player combat health after: {playerCombat.currentHealth}");
            Debug.Log("✅ Enemy attack via CombatEntity completed!");
        }
        else
        {
            Debug.LogWarning("⚠️ CombatEntity component not found on player!");
        }
        
        // Method 3: Try direct method call
        try
        {
            // Try to call TakeDamage directly
            playerTransform.SendMessage("TakeDamage", debugAttackDamage, SendMessageOptions.DontRequireReceiver);
            Debug.Log("✅ Enemy attack via SendMessage completed!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ SendMessage failed: {e.Message}");
        }
    }
    
    [ContextMenu("Force Attack Player")]
    public void ForceAttackPlayer()
    {
        AttackPlayer();
    }
    
    [ContextMenu("Check Player Components")]
    public void CheckPlayerComponents()
    {
        if (playerTransform == null)
        {
            Debug.LogError("❌ Player not found!");
            return;
        }
        
        Debug.Log($"🔍 === PLAYER COMPONENT CHECK ===");
        Debug.Log($"Player GameObject: {playerTransform.name}");
        Debug.Log($"Player Tag: {playerTransform.tag}");
        Debug.Log($"Player Position: {playerTransform.position}");
        
        // Check all components
        Component[] components = playerTransform.GetComponents<Component>();
        Debug.Log($"📦 Player has {components.Length} components:");
        
        foreach (Component comp in components)
        {
            Debug.Log($"   - {comp.GetType().Name}");
        }
        
        // Specific health component checks
        PlayerHealth health = playerTransform.GetComponent<PlayerHealth>();
        if (health != null)
        {
            Debug.Log($"💚 PlayerHealth: {health.GetCurrentHealth()}/{health.GetMaxHealth()}");
            Debug.Log($"💚 Can Take Damage: {health.CanTakeDamage()}");
            Debug.Log($"💚 Is Dead: {health.IsDead()}");
        }
        
        CombatEntity combat = playerTransform.GetComponent<CombatEntity>();
        if (combat != null)
        {
            Debug.Log($"⚔️ CombatEntity: {combat.currentHealth}/{combat.maxHealth}");
            Debug.Log($"⚔️ Is Dead: {combat.IsDead()}");
        }
    }
    
    [ContextMenu("Test Distance")]
    public void TestDistance()
    {
        if (playerTransform == null)
        {
            Debug.LogError("❌ Player not found for distance test!");
            return;
        }
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        Debug.Log($"📏 Distance to player: {distance:F2}m");
        Debug.Log($"🎯 In attack range ({debugAttackRange}m): {distance <= debugAttackRange}");
        Debug.Log($"🕐 Attack cooldown ready: {Time.time - lastAttackTime >= debugAttackCooldown}");
    }
    
    void OnDrawGizmos()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, debugAttackRange);
        
        // Draw line to player
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            Gizmos.color = distance <= debugAttackRange ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}