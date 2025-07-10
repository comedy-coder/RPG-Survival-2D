using UnityEngine;
using System.Collections;

// =============================================================================
// ENEMY HEALTH - HOÀN TOÀN ĐỘC LẬP, KHÔNG CẦN ADVANCEDHEALTHBAR
// =============================================================================

public class EnemyHealth : MonoBehaviour
{
    [Header("Enemy Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    
    [Header("Death Settings")]
    public bool destroyOnDeath = true;
    public float deathDelay = 2f;
    
    [Header("Visual Settings")]
    public bool flashOnDamage = true;
    public Color damageFlashColor = Color.red;
    public float flashDuration = 0.2f;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    // Components
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDead = false;
    
    // Events
    public System.Action<float> OnHealthChanged;
    public System.Action OnDeath;
    
    void Start()
    {
        InitializeHealth();
    }
    
    void InitializeHealth()
    {
        // Get sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Set initial health
        currentHealth = maxHealth;
        
        if (showDebugLogs)
        {
            Debug.Log($"🏥 {gameObject.name} EnemyHealth initialized: {currentHealth}/{maxHealth}");
        }
    }
    
    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (isDead || damage <= 0) return;
        
        // Apply damage
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Trigger events
        OnHealthChanged?.Invoke(currentHealth);
        
        // Visual feedback
        if (flashOnDamage)
        {
            StartCoroutine(FlashDamage());
        }
        
        if (showDebugLogs)
        {
            string attackerName = attacker != null ? attacker.name : "Unknown";
            Debug.Log($"💥 {gameObject.name} took {damage} damage from {attackerName}. Health: {currentHealth:F1}/{maxHealth}");
        }
        
        // Check death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead || amount <= 0) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        
        // Trigger events
        OnHealthChanged?.Invoke(currentHealth);
        
        if (showDebugLogs)
        {
            Debug.Log($"💚 {gameObject.name} healed {amount}. Health: {currentHealth:F1}/{maxHealth}");
        }
    }
    
    public void SetHealth(float health)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        
        // Trigger events
        OnHealthChanged?.Invoke(currentHealth);
        
        if (showDebugLogs)
        {
            Debug.Log($"🏥 {gameObject.name} health set to: {currentHealth:F1}/{maxHealth}");
        }
        
        // Check death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"💀 {gameObject.name} has died!");
        }
        
        // Trigger death event
        OnDeath?.Invoke();
        
        // Disable AI
        var enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }
        
        // Start death sequence
        StartCoroutine(DeathSequence());
    }
    
    IEnumerator DeathSequence()
    {
        // Death animation - fade out
        if (spriteRenderer != null)
        {
            Color currentColor = spriteRenderer.color;
            float fadeSpeed = 1f / deathDelay;
            
            while (currentColor.a > 0)
            {
                currentColor.a -= fadeSpeed * Time.deltaTime;
                spriteRenderer.color = currentColor;
                yield return null;
            }
        }
        else
        {
            // Just wait if no sprite renderer
            yield return new WaitForSeconds(deathDelay);
        }
        
        if (destroyOnDeath)
        {
            if (showDebugLogs)
            {
                Debug.Log($"🗑️ {gameObject.name} destroyed after death delay");
            }
            
            Destroy(gameObject);
        }
    }
    
    IEnumerator FlashDamage()
    {
        if (spriteRenderer == null) yield break;
        
        // Flash to damage color
        Color originalSpriteColor = spriteRenderer.color;
        spriteRenderer.color = damageFlashColor;
        
        yield return new WaitForSeconds(flashDuration);
        
        // Return to original color
        spriteRenderer.color = originalSpriteColor;
    }
    
    // Public getters
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercent() => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsAlive() => !isDead && currentHealth > 0;
    public bool IsDead() => isDead;
    
    // Public setters
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }
    
    public void RestoreFullHealth()
    {
        currentHealth = maxHealth;
        
        // Trigger events
        OnHealthChanged?.Invoke(currentHealth);
        
        if (showDebugLogs)
        {
            Debug.Log($"🔄 {gameObject.name} health restored to full");
        }
    }
    
    public void Revive()
    {
        if (!isDead) return;
        
        isDead = false;
        currentHealth = maxHealth;
        
        // Restore sprite alpha
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
        
        // Re-enable AI
        var enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.enabled = true;
        }
        
        // Trigger events
        OnHealthChanged?.Invoke(currentHealth);
        
        if (showDebugLogs)
        {
            Debug.Log($"🔄 {gameObject.name} has been revived!");
        }
    }
    
    // Context menu methods for testing
    [ContextMenu("Take 25 Damage")]
    public void TestDamage25()
    {
        TakeDamage(25f);
    }
    
    [ContextMenu("Take 50 Damage")]
    public void TestDamage50()
    {
        TakeDamage(50f);
    }
    
    [ContextMenu("Heal 25 HP")]
    public void TestHeal25()
    {
        Heal(25f);
    }
    
    [ContextMenu("Set Health to 1")]
    public void TestCriticalHealth()
    {
        SetHealth(1f);
    }
    
    [ContextMenu("Restore Full Health")]
    public void TestRestoreFullHealth()
    {
        RestoreFullHealth();
    }
    
    [ContextMenu("Kill Enemy")]
    public void TestKillEnemy()
    {
        SetHealth(0f);
    }
    
    [ContextMenu("Revive Enemy")]
    public void TestReviveEnemy()
    {
        Revive();
    }
    
    [ContextMenu("Show Health Status")]
    public void ShowHealthStatus()
    {
        Debug.Log($"🏥 === {gameObject.name} HEALTH STATUS ===");
        Debug.Log($"   Current Health: {currentHealth:F1}/{maxHealth:F1} ({GetHealthPercent():P1})");
        Debug.Log($"   Is Alive: {IsAlive()}");
        Debug.Log($"   Is Dead: {IsDead()}");
        Debug.Log($"   Destroy On Death: {destroyOnDeath}");
        Debug.Log($"   Death Delay: {deathDelay}s");
        Debug.Log($"   Flash On Damage: {flashOnDamage}");
        
        if (spriteRenderer != null)
        {
            Debug.Log($"   Sprite Renderer: ✅ Found");
            Debug.Log($"   Current Color: {spriteRenderer.color}");
        }
        else
        {
            Debug.Log($"   Sprite Renderer: ❌ Not Found");
        }
    }
    
    [ContextMenu("Test Flash Effect")]
    public void TestFlashEffect()
    {
        StartCoroutine(FlashDamage());
    }
    
    // Health bar visualization in Scene view
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Draw health bar above enemy
        Vector3 healthBarPos = transform.position + Vector3.up * 1.5f;
        float healthBarWidth = 2f;
        float healthBarHeight = 0.2f;
        
        // Background
        Gizmos.color = Color.black;
        Gizmos.DrawCube(healthBarPos, new Vector3(healthBarWidth, healthBarHeight, 0));
        
        // Health fill
        float healthPercent = GetHealthPercent();
        Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
        Gizmos.color = healthColor;
        
        Vector3 fillSize = new Vector3(healthBarWidth * healthPercent, healthBarHeight * 0.8f, 0);
        Vector3 fillPos = healthBarPos + Vector3.left * (healthBarWidth * (1f - healthPercent) * 0.5f);
        Gizmos.DrawCube(fillPos, fillSize);
        
        // Health text
        Vector3 textPos = healthBarPos + Vector3.up * 0.3f;
        // Note: Can't draw text in OnDrawGizmosSelected, but position is ready for custom editor
    }
    
    // Validation
    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        deathDelay = Mathf.Max(0f, deathDelay);
        flashDuration = Mathf.Max(0.1f, flashDuration);
    }
}