using UnityEngine;
using System.Collections;

public enum CombatEntityType
{
    Player,
    Enemy,
    NPC,
    Neutral
}

public class CombatEntity : MonoBehaviour
{
    [Header("Entity Info")]
    public CombatEntityType entityType = CombatEntityType.Enemy;
    public string entityName = "Combat Entity";
    
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    public bool canRegenerate = false;
    public float regenerationRate = 1f;
    public float regenerationDelay = 5f;
    
    [Header("Combat Stats")]
    public int baseDamage = 10;
    public float attackSpeed = 1f;
    public float criticalChance = 0.05f;
    public float criticalMultiplier = 1.5f;
    public float blockChance = 0.1f;
    public float blockDamageReduction = 0.5f;
    
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float combatMoveSpeed = 2f;
    public bool canMove = true;
    
    [Header("AI Settings")]
    public float detectionRange = 5f;
    public float aggroRange = 3f;
    public float attackRange = 1.5f;
    public bool isAggressive = true;
    
    [Header("Visual")]
    public GameObject healthBarPrefab;
    public Transform healthBarParent;
    public bool showHealthBar = true;
    public Color normalColor = Color.white;
    public Color combatColor = Color.red;
    public Color damagedColor = Color.yellow;
    
    [Header("Audio")]
    public AudioClip[] takeDamageSounds;
    public AudioClip[] attackSounds;
    public AudioClip deathSound;
    
    // Private variables
    private bool isDead = false;
    private bool isBlocking = false;
    private bool isAwareOfPlayer = false;
    private float lastDamageTime = 0f;
    private float lastAttackTime = 0f;
    private CombatEntity currentTarget = null;
    private Vector3 spawnPosition;
    
    // Components
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D col;
    private GameObject healthBarInstance;
    private Transform healthBarFill;
    
    // AI State
    private CombatAIState aiState = CombatAIState.Idle;
    private Vector3 lastKnownPlayerPosition;
    private float stateTimer = 0f;
    
    // Events
    public System.Action<CombatEntity, int> OnHealthChanged;
    public System.Action<CombatEntity> OnDeath;
    public System.Action<CombatEntity, CombatEntity> OnTargetChanged;
    
    public bool IsDead() => isDead;
    public bool IsBlocking() => isBlocking;
    public bool IsAwareOfPlayer() => isAwareOfPlayer;
    public CombatEntity GetCurrentTarget() => currentTarget;
    public CombatAIState GetAIState() => aiState;
    
    #region Initialization
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        spawnPosition = transform.position;
        
        // Set default name
        if (string.IsNullOrEmpty(entityName))
            entityName = gameObject.name;
    }
    
    void Start()
    {
        // Ensure health is valid
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Create health bar
        if (showHealthBar && healthBarPrefab != null)
        {
            CreateHealthBar();
        }
        
        // Setup AI if enemy
        if (entityType == CombatEntityType.Enemy)
        {
            SetAIState(CombatAIState.Idle);
        }
        
        // Setup physics
        if (rb != null)
        {
            rb.gravityScale = 0f; // 2D top-down
        }
        
        UpdateVisuals();
    }
    
    void CreateHealthBar()
    {
        if (healthBarPrefab == null) return;
        
        Vector3 healthBarPosition = transform.position + Vector3.up * 1f;
        healthBarInstance = Instantiate(healthBarPrefab, healthBarPosition, Quaternion.identity);
        
        if (healthBarParent != null)
        {
            healthBarInstance.transform.SetParent(healthBarParent);
        }
        else
        {
            healthBarInstance.transform.SetParent(transform);
        }
        
        // Find health bar fill
        healthBarFill = healthBarInstance.transform.Find("Fill");
        if (healthBarFill == null && healthBarInstance.transform.childCount > 0)
        {
            healthBarFill = healthBarInstance.transform.GetChild(0);
        }
        
        UpdateHealthBar();
    }
    
    #endregion
    
    #region Health System
    
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        int actualDamage = damage;
        
        // Apply blocking
        if (isBlocking)
        {
            actualDamage = Mathf.RoundToInt(damage * (1f - blockDamageReduction));
            ShowBlockEffect();
        }
        
        // Apply damage
        int oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - actualDamage);
        
        lastDamageTime = Time.time;
        
        // Visual effects
        ShowDamageEffect();
        PlayTakeDamageSound();
        
        // Update UI
        UpdateHealthBar();
        UpdateVisuals();
        
        // Fire event
        OnHealthChanged?.Invoke(this, currentHealth - oldHealth);
        
        // Check for death
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
        
        // AI reaction
        if (entityType == CombatEntityType.Enemy)
        {
            ReactToDamage();
        }
        
        Debug.Log($"💥 {entityName} took {actualDamage} damage | HP: {currentHealth}/{maxHealth}");
    }
    
    public void Heal(int amount)
    {
        if (isDead) return;
        
        int oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        int actualHealing = currentHealth - oldHealth;
        
        if (actualHealing > 0)
        {
            // Visual effects
            ShowHealEffect();
            
            // Update UI
            UpdateHealthBar();
            UpdateVisuals();
            
            // Fire event
            OnHealthChanged?.Invoke(this, actualHealing);
            
            Debug.Log($"💚 {entityName} healed for {actualHealing} | HP: {currentHealth}/{maxHealth}");
        }
    }
    
    public void SetHealth(int newHealth)
    {
        int oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        
        UpdateHealthBar();
        UpdateVisuals();
        
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
        
        OnHealthChanged?.Invoke(this, currentHealth - oldHealth);
    }
    
    public void RestoreToFullHealth()
    {
        SetHealth(maxHealth);
    }
    
    public float GetHealthPercentage()
    {
        return maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }
    
    #endregion
    
    #region Combat Actions
    
    public int GetTotalDamage()
    {
        return baseDamage; // Could be modified by weapons, buffs, etc.
    }
    
    public bool CanAttack()
    {
        if (isDead) return false;
        if (Time.time - lastAttackTime < (1f / attackSpeed)) return false;
        
        return true;
    }
    
    public void AttackTarget(CombatEntity target)
    {
        if (target == null || !CanAttack()) return;
        
        // Check range
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > attackRange) return;
        
        // Face target
        FaceTarget(target.transform.position);
        
        // Perform attack
        CombatManager combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager != null)
        {
            combatManager.PerformAttack(this, target);
        }
        
        lastAttackTime = Time.time;
        PlayAttackSound();
        
        // AI reaction
        if (entityType == CombatEntityType.Enemy)
        {
            SetAIState(CombatAIState.Attacking);
        }
    }
    
    public void SetBlocking(bool blocking)
    {
        isBlocking = blocking;
        
        if (blocking)
        {
            Debug.Log($"🛡️ {entityName} is blocking");
        }
    }
    
    public void SetCurrentTarget(CombatEntity target)
    {
        CombatEntity oldTarget = currentTarget;
        currentTarget = target;
        
        if (oldTarget != target)
        {
            OnTargetChanged?.Invoke(this, target);
            
            if (target != null)
            {
                Debug.Log($"🎯 {entityName} is now targeting {target.entityName}");
            }
        }
    }
    
    #endregion
    
    #region Movement
    
    public void MoveTowards(Vector3 targetPosition)
    {
        if (!canMove || isDead) return;
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        float speed = isAwareOfPlayer ? combatMoveSpeed : moveSpeed;
        
        if (rb != null)
        {
            rb.MovePosition(transform.position + direction * speed * Time.fixedDeltaTime);
        }
        else
        {
            transform.position += direction * speed * Time.deltaTime;
        }
        
        // Face movement direction
        FaceDirection(direction);
    }
    
    public void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        FaceDirection(direction);
    }
    
    public void FaceDirection(Vector3 direction)
    {
        if (direction.magnitude > 0.1f)
        {
            // Simple sprite flipping for 2D
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }
    }
    
    #endregion
    
    #region AI System
    
    void Update()
    {
        if (isDead) return;
        
        // Handle regeneration
        if (canRegenerate && Time.time - lastDamageTime > regenerationDelay)
        {
            RegenerateHealth();
        }
        
        // Handle AI
        if (entityType == CombatEntityType.Enemy)
        {
            UpdateAI();
        }
        
        // Update state timer
        stateTimer += Time.deltaTime;
    }
    
    void UpdateAI()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        
        // Update awareness
        if (distanceToPlayer <= detectionRange)
        {
            SetAwareOfPlayer(true);
            lastKnownPlayerPosition = player.transform.position;
        }
        else if (distanceToPlayer > detectionRange * 1.5f)
        {
            SetAwareOfPlayer(false);
        }
        
        // AI State Machine
        switch (aiState)
        {
            case CombatAIState.Idle:
                UpdateIdleState(player.transform.position, distanceToPlayer);
                break;
                
            case CombatAIState.Patrolling:
                UpdatePatrollingState(player.transform.position, distanceToPlayer);
                break;
                
            case CombatAIState.Chasing:
                UpdateChasingState(player.transform.position, distanceToPlayer);
                break;
                
            case CombatAIState.Attacking:
                UpdateAttackingState(player.transform.position, distanceToPlayer);
                break;
                
            case CombatAIState.Returning:
                UpdateReturningState(player.transform.position, distanceToPlayer);
                break;
        }
    }
    
    void UpdateIdleState(Vector3 playerPos, float playerDistance)
    {
        if (isAwareOfPlayer && playerDistance <= aggroRange && isAggressive)
        {
            SetCurrentTarget(GameObject.FindGameObjectWithTag("Player").GetComponent<CombatEntity>());
            SetAIState(CombatAIState.Chasing);
        }
        else if (stateTimer > 3f)
        {
            SetAIState(CombatAIState.Patrolling);
        }
    }
    
    void UpdatePatrollingState(Vector3 playerPos, float playerDistance)
    {
        if (isAwareOfPlayer && playerDistance <= aggroRange && isAggressive)
        {
            SetCurrentTarget(GameObject.FindGameObjectWithTag("Player").GetComponent<CombatEntity>());
            SetAIState(CombatAIState.Chasing);
        }
        else
        {
            // Simple patrol: move towards spawn position
            MoveTowards(spawnPosition);
            
            if (Vector3.Distance(transform.position, spawnPosition) < 0.5f)
            {
                SetAIState(CombatAIState.Idle);
            }
        }
    }
    
    void UpdateChasingState(Vector3 playerPos, float playerDistance)
    {
        if (currentTarget == null || currentTarget.IsDead())
        {
            SetAIState(CombatAIState.Returning);
            return;
        }
        
        if (playerDistance <= attackRange)
        {
            SetAIState(CombatAIState.Attacking);
        }
        else if (playerDistance > aggroRange * 2f)
        {
            SetAIState(CombatAIState.Returning);
        }
        else
        {
            MoveTowards(playerPos);
        }
    }
    
    void UpdateAttackingState(Vector3 playerPos, float playerDistance)
    {
        if (currentTarget == null || currentTarget.IsDead())
        {
            SetAIState(CombatAIState.Returning);
            return;
        }
        
        if (playerDistance <= attackRange)
        {
            AttackTarget(currentTarget);
        }
        else if (playerDistance > attackRange * 1.5f)
        {
            SetAIState(CombatAIState.Chasing);
        }
    }
    
    void UpdateReturningState(Vector3 playerPos, float playerDistance)
    {
        MoveTowards(spawnPosition);
        
        if (Vector3.Distance(transform.position, spawnPosition) < 0.5f)
        {
            SetCurrentTarget(null);
            SetAIState(CombatAIState.Idle);
        }
        else if (isAwareOfPlayer && playerDistance <= aggroRange && isAggressive)
        {
            SetCurrentTarget(GameObject.FindGameObjectWithTag("Player").GetComponent<CombatEntity>());
            SetAIState(CombatAIState.Chasing);
        }
    }
    
    void SetAIState(CombatAIState newState)
    {
        if (aiState != newState)
        {
            aiState = newState;
            stateTimer = 0f;
            
            Debug.Log($"🤖 {entityName} AI State: {newState}");
        }
    }
    
    public void SetAwareOfPlayer(bool aware)
    {
        isAwareOfPlayer = aware;
        UpdateVisuals();
    }
    
    void ReactToDamage()
    {
        if (aiState == CombatAIState.Idle || aiState == CombatAIState.Patrolling)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                SetCurrentTarget(player.GetComponent<CombatEntity>());
                SetAIState(CombatAIState.Chasing);
            }
        }
    }
    
    #endregion
    
    #region Visual & Audio
    
    void UpdateVisuals()
    {
        if (spriteRenderer == null) return;
        
        if (isDead)
        {
            spriteRenderer.color = Color.gray;
        }
        else if (GetHealthPercentage() < 0.3f)
        {
            spriteRenderer.color = damagedColor;
        }
        else if (isAwareOfPlayer)
        {
            spriteRenderer.color = combatColor;
        }
        else
        {
            spriteRenderer.color = normalColor;
        }
    }
    
    void UpdateHealthBar()
    {
        if (healthBarFill == null) return;
        
        float healthPercent = GetHealthPercentage();
        healthBarFill.localScale = new Vector3(healthPercent, 1f, 1f);
        
        // Color based on health
        SpriteRenderer fillRenderer = healthBarFill.GetComponent<SpriteRenderer>();
        if (fillRenderer != null)
        {
            if (healthPercent > 0.6f)
                fillRenderer.color = Color.green;
            else if (healthPercent > 0.3f)
                fillRenderer.color = Color.yellow;
            else
                fillRenderer.color = Color.red;
        }
        
        // Hide health bar if at full health and not player
        if (healthBarInstance != null && entityType != CombatEntityType.Player)
        {
            healthBarInstance.SetActive(healthPercent < 1f);
        }
    }
    
    void ShowDamageEffect()
    {
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashEffect(Color.red, 0.1f));
        }
    }
    
    void ShowHealEffect()
    {
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashEffect(Color.green, 0.2f));
        }
    }
    
    void ShowBlockEffect()
    {
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashEffect(Color.blue, 0.1f));
        }
    }
    
    IEnumerator FlashEffect(Color flashColor, float duration)
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = flashColor;
        
        yield return new WaitForSeconds(duration);
        
        spriteRenderer.color = originalColor;
    }
    
    void PlayTakeDamageSound()
    {
        if (takeDamageSounds.Length > 0)
        {
            AudioClip sound = takeDamageSounds[Random.Range(0, takeDamageSounds.Length)];
            AudioSource.PlayClipAtPoint(sound, transform.position);
        }
    }
    
    void PlayAttackSound()
    {
        if (attackSounds.Length > 0)
        {
            AudioClip sound = attackSounds[Random.Range(0, attackSounds.Length)];
            AudioSource.PlayClipAtPoint(sound, transform.position);
        }
    }
    
    #endregion
    
    #region Death System
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // Visual changes
        UpdateVisuals();
        
        // Audio
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }
        
        // Disable components
        if (col != null)
            col.enabled = false;
            
        if (rb != null)
            rb.simulated = false;
        
        // Hide health bar
        if (healthBarInstance != null)
        {
            healthBarInstance.SetActive(false);
        }
        
        // Fire events
        OnDeath?.Invoke(this);
        CombatManager.OnCombatantDied?.Invoke(this);
        
        // Remove after delay
        StartCoroutine(RemoveAfterDelay(3f));
        
        Debug.Log($"💀 {entityName} died");
    }
    
    IEnumerator RemoveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
        }
        
        Destroy(gameObject);
    }
    
    #endregion
    
    #region Regeneration
    
    void RegenerateHealth()
    {
        if (currentHealth < maxHealth)
        {
            int regenAmount = Mathf.RoundToInt(regenerationRate * Time.deltaTime);
            if (regenAmount > 0)
            {
                Heal(regenAmount);
            }
        }
    }
    
    #endregion
    
    #region Gizmos
    
    void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Aggro range - SỬA: dùng new Color thay vì Color.orange
        Gizmos.color = new Color(1f, 0.5f, 0f); // Orange color
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Spawn position
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnPosition, Vector3.one * 0.5f);
        
        // Current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
    }
    
    #endregion
}

public enum CombatAIState
{
    Idle,
    Patrolling,
    Chasing,
    Attacking,
    Returning,
    Stunned
}