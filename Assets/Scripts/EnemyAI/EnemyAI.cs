using UnityEngine;

// ✅ ENUM BÊN NGOÀI CLASS - FIX CS0050 ERROR
public enum EnemyState
{
    Idle,
    Chasing,
    Attacking
}

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float detectionRange = 8f;
    public float attackRange = 3f;
    public float moveSpeed = 2f;
    public float attackDamage = 20f;
    public float attackCooldown = 1.5f;
    
    [Header("Visual Settings")]
    public Color idleColor = Color.white;
    public Color chaseColor = Color.red;
    public Color attackColor = Color.yellow;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showGizmos = true;
    
    // Private variables
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private SpriteRenderer spriteRenderer;
    private float lastAttackTime = 0f;
    private EnemyState currentState = EnemyState.Idle;
    private bool isInitialized = false;
    
    void Start()
    {
        InitializeEnemy();
    }
    
    void InitializeEnemy()
    {
        // Find player by tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerHealth = player.GetComponent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                if (showDebugLogs)
                    Debug.Log($"✅ {gameObject.name} found player: {player.name}");
            }
            else
            {
                Debug.LogError($"❌ {gameObject.name} - Player found but no PlayerHealth component!");
            }
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name} - No player found with tag 'Player'!");
        }
        
        // Get sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name} - No SpriteRenderer found!");
        }
        
        // Set initial state
        SetState(EnemyState.Idle);
        isInitialized = true;
        
        if (showDebugLogs)
            Debug.Log($"🤖 {gameObject.name} AI initialized - Detection: {detectionRange}m, Attack: {attackRange}m, Damage: {attackDamage}");
    }
    
    void Update()
    {
        if (!isInitialized || playerTransform == null) return;
        
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Update AI state based on distance
        UpdateAIState(distanceToPlayer);
        
        // Execute current state behavior
        ExecuteCurrentState(distanceToPlayer);
    }
    
    void UpdateAIState(float distanceToPlayer)
    {
        EnemyState newState = currentState;
        
        // Determine new state based on distance
        if (distanceToPlayer <= attackRange)
        {
            newState = EnemyState.Attacking;
        }
        else if (distanceToPlayer <= detectionRange)
        {
            newState = EnemyState.Chasing;
        }
        else
        {
            newState = EnemyState.Idle;
        }
        
        // Update state if changed
        if (newState != currentState)
        {
            SetState(newState);
        }
    }
    
    void ExecuteCurrentState(float distanceToPlayer)
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                ExecuteIdleState();
                break;
                
            case EnemyState.Chasing:
                ExecuteChasingState();
                break;
                
            case EnemyState.Attacking:
                ExecuteAttackingState();
                break;
        }
    }
    
    void ExecuteIdleState()
    {
        // Just wait in idle state
        // Future: Add patrol behavior here
    }
    
    void ExecuteChasingState()
    {
        ChasePlayer();
    }
    
    void ExecuteAttackingState()
    {
        AttackPlayer();
    }
    
    void SetState(EnemyState newState)
    {
        if (currentState == newState) return;
        
        EnemyState oldState = currentState;
        currentState = newState;
        
        // Update visual
        UpdateVisualState();
        
        if (showDebugLogs)
            Debug.Log($"🤖 {gameObject.name} state: {oldState} → {newState}");
    }
    
    void UpdateVisualState()
    {
        if (spriteRenderer == null) return;
        
        switch (currentState)
        {
            case EnemyState.Idle:
                spriteRenderer.color = idleColor;
                break;
            case EnemyState.Chasing:
                spriteRenderer.color = chaseColor;
                break;
            case EnemyState.Attacking:
                spriteRenderer.color = attackColor;
                break;
        }
    }
    
    void ChasePlayer()
    {
        if (playerTransform == null) return;
        
        // Calculate direction to player
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        
        // Move towards player
        Vector3 newPosition = transform.position + direction * moveSpeed * Time.deltaTime;
        transform.position = newPosition;
        
        // Face player direction
        FaceDirection(direction);
    }
    
    void AttackPlayer()
    {
        // Check attack cooldown
        if (Time.time - lastAttackTime < attackCooldown)
            return;
        
        // Check if player health exists
        if (playerHealth == null)
        {
            Debug.LogError($"❌ {gameObject.name} - Cannot attack: PlayerHealth is null!");
            return;
        }
        
        // Perform attack
        PerformAttack();
    }
    
    void PerformAttack()
    {
        if (showDebugLogs)
            Debug.Log($"🔥 {gameObject.name} attacking player for {attackDamage} damage!");
        
        // Deal damage to player
        playerHealth.TakeDamage(attackDamage, gameObject);
        
        // Update last attack time
        lastAttackTime = Time.time;
        
        if (showDebugLogs)
        {
            Debug.Log($"⚔️ {gameObject.name} attack completed!");
            Debug.Log($"💔 Player health: {playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}");
        }
    }
    
    void FaceDirection(Vector3 direction)
    {
        if (direction.magnitude < 0.1f) return;
        
        // Flip sprite based on direction
        if (direction.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else
            transform.localScale = new Vector3(1, 1, 1);
    }
    
    // ✅ FIXED: Remove GetCurrentState() method that causes CS0050
    // Public methods for external access
    public float GetDistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue;
        return Vector3.Distance(transform.position, playerTransform.position);
    }
    
    public bool CanAttack()
    {
        return Time.time - lastAttackTime >= attackCooldown;
    }
    
    public bool IsPlayerInRange()
    {
        return GetDistanceToPlayer() <= detectionRange;
    }
    
    public bool IsPlayerInAttackRange()
    {
        return GetDistanceToPlayer() <= attackRange;
    }
    
    // Debug and testing methods
    [ContextMenu("Force Attack Player")]
    public void ForceAttackPlayer()
    {
        if (playerHealth != null)
        {
            Debug.Log($"🧪 {gameObject.name} - Manual attack triggered!");
            PerformAttack();
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name} - Cannot force attack: PlayerHealth not found!");
        }
    }
    
    [ContextMenu("Show Enemy Status")]
    public void ShowEnemyStatus()
    {
        Debug.Log($"🤖 === {gameObject.name} STATUS ===");
        Debug.Log($"Current State: {currentState}");
        Debug.Log($"Detection Range: {detectionRange}m");
        Debug.Log($"Attack Range: {attackRange}m");
        Debug.Log($"Attack Damage: {attackDamage}");
        Debug.Log($"Attack Cooldown: {attackCooldown}s");
        Debug.Log($"Move Speed: {moveSpeed}");
        Debug.Log($"Last Attack: {Time.time - lastAttackTime:F1}s ago");
        Debug.Log($"Can Attack: {CanAttack()}");
        Debug.Log($"Is Initialized: {isInitialized}");
        
        if (playerTransform != null)
        {
            float distance = GetDistanceToPlayer();
            Debug.Log($"Distance to Player: {distance:F1}m");
            Debug.Log($"Player in Detection Range: {IsPlayerInRange()}");
            Debug.Log($"Player in Attack Range: {IsPlayerInAttackRange()}");
        }
        else
        {
            Debug.Log($"❌ Player Transform: NULL");
        }
        
        if (playerHealth != null)
        {
            Debug.Log($"Player Health: {playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}");
        }
        else
        {
            Debug.Log($"❌ Player Health: NULL");
        }
    }
    
    [ContextMenu("Reset Enemy")]
    public void ResetEnemy()
    {
        lastAttackTime = 0f;
        SetState(EnemyState.Idle);
        transform.localScale = Vector3.one;
        
        if (spriteRenderer != null)
            spriteRenderer.color = idleColor;
            
        Debug.Log($"🔄 {gameObject.name} has been reset");
    }
    
    // Gizmos for visual debugging
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Detection range (yellow circle)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack range (red circle)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Line to player
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            // Color based on range
            if (distance <= attackRange)
                Gizmos.color = Color.red;
            else if (distance <= detectionRange)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.gray;
                
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Show current state as colored cube above enemy
        Vector3 stateIndicatorPos = transform.position + Vector3.up * 2f;
        
        Color stateColor = Color.white;
        switch (currentState)
        {
            case EnemyState.Idle:
                stateColor = idleColor;
                break;
            case EnemyState.Chasing:
                stateColor = chaseColor;
                break;
            case EnemyState.Attacking:
                stateColor = attackColor;
                break;
        }
        
        Gizmos.color = stateColor;
        Gizmos.DrawCube(stateIndicatorPos, Vector3.one * 0.3f);
    }
    
    // Validation method
    void OnValidate()
    {
        // Ensure positive values
        detectionRange = Mathf.Max(0.1f, detectionRange);
        attackRange = Mathf.Max(0.1f, attackRange);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        attackDamage = Mathf.Max(1f, attackDamage);
        attackCooldown = Mathf.Max(0.1f, attackCooldown);
        
        // Ensure attack range is not larger than detection range
        if (attackRange > detectionRange)
            attackRange = detectionRange;
    }
}