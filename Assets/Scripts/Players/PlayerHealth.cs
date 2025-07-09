using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float healthRegenRate = 1f;
    public float regenDelay = 5f;
    
    [Header("Damage Settings")]
    public float invulnerabilityTime = 0.5f;
    public bool enableHealthRegen = true;
    
    private bool isInvulnerable = false;
    private bool isDead = false;
    private float lastDamageTime = 0f;
    
    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log($"🩺 PlayerHealth initialized: {currentHealth}/{maxHealth} HP");
    }
    
    void Update()
    {
        if (enableHealthRegen && !isDead && currentHealth < maxHealth)
        {
            if (Time.time - lastDamageTime >= regenDelay)
            {
                RegenerateHealth();
            }
        }
    }
    
    public void TakeDamage(float damage, GameObject attacker = null)
    {
        Debug.Log($"🩸 Player taking {damage} damage");
        
        if (isDead || isInvulnerable) return;
        if (damage <= 0) return;
        
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        lastDamageTime = Time.time;
        
        if (invulnerabilityTime > 0)
            StartCoroutine(InvulnerabilityPeriod());
        
        if (currentHealth <= 0)
            Die();
        
        Debug.Log($"💔 Player health: {currentHealth}/{maxHealth}");
    }
    
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Debug.Log($"💚 Player healed: {currentHealth}/{maxHealth}");
    }
    
    public float GetCurrentHealth() { return currentHealth; }
    public float GetMaxHealth() { return maxHealth; }
    public float GetHealthPercentage() { return currentHealth / maxHealth; }
    public float GetHealthPercent() { return GetHealthPercentage(); }
    public bool IsDead() { return isDead; }
    public bool CanTakeDamage() { return !isDead && !isInvulnerable; }
    
    void RegenerateHealth()
    {
        Heal(healthRegenRate * Time.deltaTime);
    }
    
    void Die()
    {
        isDead = true;
        Debug.Log("💀 Player died!");
        
        // Respawn after 3 seconds
        StartCoroutine(RespawnAfterDelay());
    }
    
    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        
        isDead = false;
        currentHealth = maxHealth;
        Debug.Log("✨ Player respawned!");
    }
    
    IEnumerator InvulnerabilityPeriod()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityTime);
        isInvulnerable = false;
    }
    
    [ContextMenu("Test Damage")]
    public void TestDamage() { TakeDamage(25f); }
    
    [ContextMenu("Full Heal")]
    public void FullHeal() { currentHealth = maxHealth; isDead = false; }
}