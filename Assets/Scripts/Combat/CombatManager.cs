using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CombatManager : MonoBehaviour
{
    [Header("Combat Settings")]
    public float combatRange = 2f;
    public float combatCooldown = 1f;
    public LayerMask enemyLayers = -1;
    public LayerMask obstacleLayer = -1;
    
    [Header("Player Combat")]
    public int playerBaseDamage = 10;
    public float playerAttackSpeed = 1f;
    public float playerCriticalChance = 0.1f;
    public float playerCriticalMultiplier = 1.5f;
    
    [Header("Combat Effects")]
    public GameObject hitEffect;
    public GameObject criticalHitEffect;
    public GameObject blockEffect;
    public AudioClip[] attackSounds;
    public AudioClip[] hitSounds;
    public AudioClip criticalHitSound;
    
    [Header("UI Settings")]
    public bool showCombatUI = true;
    public bool showDamageNumbers = true;
    public bool showCombatRange = true;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool showCombatGizmos = true;
    
    // Combat state
    private bool isInCombat = false;
    private float lastAttackTime = 0f;
    private List<CombatEntity> combatants = new List<CombatEntity>();
    private List<CombatEntity> enemies = new List<CombatEntity>();
    
    // Components
    private Camera mainCamera;
    private CombatEntity playerCombat;
    private SimpleInventory inventory;
    
    // Events
    public static System.Action<CombatEntity, CombatEntity, int> OnDamageDealt;
    public static System.Action<CombatEntity> OnCombatantDied;
    public static System.Action<bool> OnCombatStateChanged;
    
    public bool IsInCombat => isInCombat;
    public float LastAttackTime => lastAttackTime;
    
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
            
        inventory = FindFirstObjectByType<SimpleInventory>();
        
        // Find player combat entity
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerCombat = player.GetComponent<CombatEntity>();
            if (playerCombat == null)
            {
                playerCombat = player.AddComponent<CombatEntity>();
                SetupPlayerCombat();
            }
        }
        
        // Find all combat entities
        RefreshCombatants();
        
        // Subscribe to events
        OnCombatantDied += HandleCombatantDeath;
        
        if (enableDebugLogs)
            Debug.Log($"🗡️ Combat System initialized - Range: {combatRange}m, Cooldown: {combatCooldown}s");
    }
    
    void SetupPlayerCombat()
    {
        if (playerCombat == null) return;
        
        playerCombat.entityType = CombatEntityType.Player;
        playerCombat.maxHealth = 100;
        playerCombat.currentHealth = 100;
        playerCombat.baseDamage = playerBaseDamage;
        playerCombat.attackSpeed = playerAttackSpeed;
        playerCombat.criticalChance = playerCriticalChance;
        playerCombat.criticalMultiplier = playerCriticalMultiplier;
        
        if (enableDebugLogs)
            Debug.Log($"🛡️ Player combat setup complete - HP: {playerCombat.maxHealth}, Damage: {playerBaseDamage}");
    }
    
    #endregion
    
    #region Update & Input
    
    void Update()
    {
        HandleCombatInput();
        UpdateCombatState();
        CheckCombatProximity();
    }
    
    void HandleCombatInput()
    {
        if (playerCombat == null || playerCombat.IsDead()) return;
        
        // Left click to attack
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = GetMouseWorldPosition();
            TryAttack(mousePos);
        }
        
        // Right click to block (if implemented)
        if (Input.GetMouseButtonDown(1))
        {
            StartBlocking();
        }
        
        if (Input.GetMouseButtonUp(1))
        {
            StopBlocking();
        }
        
        // Combat keys
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CycleNearestEnemy();
        }
    }
    
    void UpdateCombatState()
    {
        bool wasInCombat = isInCombat;
        
        // Check if player is near enemies
        isInCombat = false;
        if (playerCombat != null && !playerCombat.IsDead())
        {
            foreach (CombatEntity enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead())
                {
                    float distance = Vector3.Distance(playerCombat.transform.position, enemy.transform.position);
                    if (distance <= combatRange * 2f) // Combat zone is larger than attack range
                    {
                        isInCombat = true;
                        break;
                    }
                }
            }
        }
        
        // Fire event if combat state changed
        if (wasInCombat != isInCombat)
        {
            OnCombatStateChanged?.Invoke(isInCombat);
            
            if (enableDebugLogs)
                Debug.Log($"⚔️ Combat state changed: {(isInCombat ? "ENTERED COMBAT" : "LEFT COMBAT")}");
        }
    }
    
    void CheckCombatProximity()
    {
        if (playerCombat == null) return;
        
        // Update enemy list
        RefreshEnemies();
        
        // Check for nearby enemies
        foreach (CombatEntity enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead())
            {
                float distance = Vector3.Distance(playerCombat.transform.position, enemy.transform.position);
                
                // Mark enemy as aware if close enough
                if (distance <= combatRange * 1.5f)
                {
                    enemy.SetAwareOfPlayer(true);
                }
                else if (distance > combatRange * 3f)
                {
                    enemy.SetAwareOfPlayer(false);
                }
            }
        }
    }
    
    #endregion
    
    #region Combat Actions
    
    public void TryAttack(Vector3 targetPosition)
    {
        if (!CanAttack()) return;
        
        // Find target at position
        CombatEntity target = FindTargetAtPosition(targetPosition);
        
        if (target != null)
        {
            float distance = Vector3.Distance(playerCombat.transform.position, target.transform.position);
            
            if (distance <= combatRange)
            {
                PerformAttack(playerCombat, target);
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"❌ Target too far! Distance: {distance:F1}m, Max: {combatRange}m");
            }
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"❌ No target found at position {targetPosition}");
        }
    }
    
    public void PerformAttack(CombatEntity attacker, CombatEntity target)
    {
        if (attacker == null || target == null) return;
        if (attacker.IsDead() || target.IsDead()) return;
        
        // Calculate damage
        int baseDamage = attacker.GetTotalDamage();
        bool isCritical = Random.Range(0f, 1f) < attacker.criticalChance;
        
        int finalDamage = baseDamage;
        if (isCritical)
        {
            finalDamage = Mathf.RoundToInt(baseDamage * attacker.criticalMultiplier);
        }
        
        // Apply damage
        target.TakeDamage(finalDamage);
        
        // Effects
        PlayCombatEffects(target.transform.position, isCritical);
        
        // Show damage numbers
        if (showDamageNumbers)
        {
            ShowDamageNumber(target.transform.position, finalDamage, isCritical);
        }
        
        // Update attack time
        lastAttackTime = Time.time;
        
        // Fire event
        OnDamageDealt?.Invoke(attacker, target, finalDamage);
        
        if (enableDebugLogs)
        {
            Debug.Log($"⚔️ {attacker.name} attacked {target.name} for {finalDamage} damage" +
                     (isCritical ? " (CRITICAL!)" : "") +
                     $" | Target HP: {target.currentHealth}/{target.maxHealth}");
        }
    }
    
    bool CanAttack()
    {
        if (playerCombat == null || playerCombat.IsDead()) return false;
        if (Time.time - lastAttackTime < combatCooldown) return false;
        
        return true;
    }
    
    public void StartBlocking()
    {
        if (playerCombat != null)
        {
            playerCombat.SetBlocking(true);
            
            if (enableDebugLogs)
                Debug.Log($"🛡️ Player started blocking");
        }
    }
    
    public void StopBlocking()
    {
        if (playerCombat != null)
        {
            playerCombat.SetBlocking(false);
            
            if (enableDebugLogs)
                Debug.Log($"🛡️ Player stopped blocking");
        }
    }
    
    #endregion
    
    #region Target Finding
    
    CombatEntity FindTargetAtPosition(Vector3 position)
    {
        // Find closest combat entity to position
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(position, 0.5f, enemyLayers);
        
        CombatEntity closestTarget = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider2D col in nearbyColliders)
        {
            CombatEntity combatEntity = col.GetComponent<CombatEntity>();
            if (combatEntity != null && combatEntity != playerCombat && !combatEntity.IsDead())
            {
                float distance = Vector3.Distance(position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = combatEntity;
                }
            }
        }
        
        return closestTarget;
    }
    
    public CombatEntity FindNearestEnemy(Vector3 position)
    {
        CombatEntity nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        
        foreach (CombatEntity enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead())
            {
                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
        }
        
        return nearestEnemy;
    }
    
    void CycleNearestEnemy()
    {
        if (playerCombat == null) return;
        
        CombatEntity nearestEnemy = FindNearestEnemy(playerCombat.transform.position);
        if (nearestEnemy != null)
        {
            float distance = Vector3.Distance(playerCombat.transform.position, nearestEnemy.transform.position);
            
            if (enableDebugLogs)
                Debug.Log($"🎯 Nearest enemy: {nearestEnemy.name} at {distance:F1}m");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"🎯 No enemies found");
        }
    }
    
    #endregion
    
    #region Combat Entity Management
    
    void RefreshCombatants()
    {
        combatants.Clear();
        CombatEntity[] allCombatants = FindObjectsByType<CombatEntity>(FindObjectsSortMode.None);
        
        foreach (CombatEntity combatant in allCombatants)
        {
            if (combatant != null)
            {
                combatants.Add(combatant);
            }
        }
        
        RefreshEnemies();
        
        if (enableDebugLogs)
            Debug.Log($"🗡️ Refreshed combatants: {combatants.Count} total, {enemies.Count} enemies");
    }
    
    void RefreshEnemies()
    {
        enemies.Clear();
        
        foreach (CombatEntity combatant in combatants)
        {
            if (combatant != null && combatant.entityType == CombatEntityType.Enemy)
            {
                enemies.Add(combatant);
            }
        }
    }
    
    void HandleCombatantDeath(CombatEntity deadCombatant)
    {
        if (deadCombatant == null) return;
        
        // Remove from lists
        combatants.Remove(deadCombatant);
        enemies.Remove(deadCombatant);
        
        // Handle player death
        if (deadCombatant == playerCombat)
        {
            HandlePlayerDeath();
        }
        
        // Handle enemy death
        else if (deadCombatant.entityType == CombatEntityType.Enemy)
        {
            HandleEnemyDeath(deadCombatant);
        }
        
        if (enableDebugLogs)
            Debug.Log($"💀 {deadCombatant.name} died");
    }
    
    void HandlePlayerDeath()
    {
        isInCombat = false;
        OnCombatStateChanged?.Invoke(false);
        
        if (enableDebugLogs)
            Debug.Log($"💀 Player died - Game Over?");
        
        // TODO: Implement respawn or game over logic
    }
    
    void HandleEnemyDeath(CombatEntity enemy)
    {
        // TODO: Drop loot, give experience, etc.
        if (enableDebugLogs)
            Debug.Log($"💀 Enemy {enemy.name} defeated");
    }
    
    #endregion
    
    #region Effects & Audio
    
    void PlayCombatEffects(Vector3 position, bool isCritical = false)
    {
        // Visual effects
        GameObject effectPrefab = isCritical ? criticalHitEffect : hitEffect;
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Audio effects
        AudioClip soundToPlay = null;
        
        if (isCritical && criticalHitSound != null)
        {
            soundToPlay = criticalHitSound;
        }
        else if (hitSounds.Length > 0)
        {
            soundToPlay = hitSounds[Random.Range(0, hitSounds.Length)];
        }
        
        if (soundToPlay != null)
        {
            AudioSource.PlayClipAtPoint(soundToPlay, position);
        }
    }
    
    void ShowDamageNumber(Vector3 position, int damage, bool isCritical)
    {
        // Create floating damage number
        GameObject damageNumber = new GameObject($"DamageNumber_{damage}");
        damageNumber.transform.position = position + Vector3.up * 0.5f;
        
        // Add text component (simplified - you might want to use UI Text)
        TextMesh textMesh = damageNumber.AddComponent<TextMesh>();
        textMesh.text = damage.ToString();
        textMesh.fontSize = isCritical ? 20 : 16;
        textMesh.color = isCritical ? Color.red : Color.white;
        textMesh.anchor = TextAnchor.MiddleCenter;
        
        // Animate the number
        StartCoroutine(AnimateDamageNumber(damageNumber));
    }
    
    IEnumerator AnimateDamageNumber(GameObject damageNumber)
    {
        Vector3 startPos = damageNumber.transform.position;
        Vector3 endPos = startPos + Vector3.up * 1f;
        
        float timer = 0f;
        float duration = 1f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            
            damageNumber.transform.position = Vector3.Lerp(startPos, endPos, progress);
            
            // Fade out
            TextMesh textMesh = damageNumber.GetComponent<TextMesh>();
            if (textMesh != null)
            {
                Color color = textMesh.color;
                color.a = 1f - progress;
                textMesh.color = color;
            }
            
            yield return null;
        }
        
        Destroy(damageNumber);
    }
    
    #endregion
    
    #region Utility Methods
    
    Vector3 GetMouseWorldPosition()
    {
        if (mainCamera != null)
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            return mousePos;
        }
        return Vector3.zero;
    }
    
    public bool IsPositionInCombatRange(Vector3 position)
    {
        if (playerCombat == null) return false;
        
        float distance = Vector3.Distance(playerCombat.transform.position, position);
        return distance <= combatRange;
    }
    
    public float GetDistanceToPlayer(Vector3 position)
    {
        if (playerCombat == null) return float.MaxValue;
        
        return Vector3.Distance(playerCombat.transform.position, position);
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Debug Combat State")]
    public void DebugCombatState()
    {
        Debug.Log($"🗡️ === COMBAT STATE DEBUG ===");
        Debug.Log($"Is in combat: {isInCombat}");
        Debug.Log($"Combat range: {combatRange}m");
        Debug.Log($"Last attack: {Time.time - lastAttackTime:F1}s ago");
        Debug.Log($"Can attack: {CanAttack()}");
        
        if (playerCombat != null)
        {
            Debug.Log($"\n🛡️ Player Combat:");
            Debug.Log($"  HP: {playerCombat.currentHealth}/{playerCombat.maxHealth}");
            Debug.Log($"  Damage: {playerCombat.GetTotalDamage()}");
            Debug.Log($"  Is blocking: {playerCombat.IsBlocking()}");
            Debug.Log($"  Is dead: {playerCombat.IsDead()}");
        }
        
        Debug.Log($"\n👹 Enemies ({enemies.Count}):");
        foreach (CombatEntity enemy in enemies)
        {
            if (enemy != null)
            {
                float distance = Vector3.Distance(playerCombat.transform.position, enemy.transform.position);
                Debug.Log($"  {enemy.name}: HP {enemy.currentHealth}/{enemy.maxHealth}, Distance: {distance:F1}m");
            }
        }
    }
    
    [ContextMenu("Refresh Combatants")]
    public void RefreshCombatantsMenu()
    {
        RefreshCombatants();
    }
    
    [ContextMenu("Spawn Test Enemy")]
    public void SpawnTestEnemy()
    {
        if (playerCombat == null) return;
        
        // Create test enemy
        GameObject testEnemy = new GameObject("Test Enemy");
        testEnemy.transform.position = playerCombat.transform.position + Vector3.right * 2f;
        testEnemy.tag = "Enemy";
        
        // Add components
        CombatEntity enemyCombat = testEnemy.AddComponent<CombatEntity>();
        enemyCombat.entityType = CombatEntityType.Enemy;
        enemyCombat.maxHealth = 50;
        enemyCombat.currentHealth = 50;
        enemyCombat.baseDamage = 8;
        
        // Add visual
        SpriteRenderer sr = testEnemy.AddComponent<SpriteRenderer>();
        sr.color = Color.red;
        
        // Add collider
        CircleCollider2D col = testEnemy.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        
        RefreshCombatants();
        
        Debug.Log($"🧪 Test enemy spawned at {testEnemy.transform.position}");
    }
    
    #endregion
    
    #region Gizmos
    
    void OnDrawGizmos()
    {
        if (!showCombatGizmos || playerCombat == null) return;
        
        // Combat range
        Gizmos.color = isInCombat ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(playerCombat.transform.position, combatRange);
        
        // Combat zone (larger detection area)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(playerCombat.transform.position, combatRange * 2f);
        
        // Mouse position
        if (Application.isPlaying && mainCamera != null)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            bool inRange = IsPositionInCombatRange(mousePos);
            
            Gizmos.color = inRange ? Color.green : Color.red;
            Gizmos.DrawWireCube(mousePos, Vector3.one * 0.3f);
        }
        
        // Enemy connections
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            foreach (CombatEntity enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead())
                {
                    Gizmos.DrawLine(playerCombat.transform.position, enemy.transform.position);
                }
            }
        }
    }
    
    #endregion
    public void StartCombat()
{
    if (!isInCombat)
    {
        isInCombat = true;
        OnCombatStateChanged?.Invoke(true);
        
        if (enableDebugLogs)
            Debug.Log("⚔️ Combat STARTED manually");
    }
}

public void EndCombat()
{
    if (isInCombat)
    {
        isInCombat = false;
        OnCombatStateChanged?.Invoke(false);
        
        if (enableDebugLogs)
            Debug.Log("🕊️ Combat ENDED manually");
    }
}
    void OnDestroy()
    {
        // Unsubscribe from events
        OnCombatantDied -= HandleCombatantDeath;
    }
}