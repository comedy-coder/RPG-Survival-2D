using UnityEngine;

// =============================================================================
// ENEMY STATE ENUM - Đặt trước class EnemyAI
// =============================================================================

public enum EnemyState
{
    Idle,
    Chasing,
    Attacking
}

// =============================================================================
// ENEMY AI - Complete with enum
// =============================================================================

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
    
    [Header("Flipping Settings")]
    public bool enableFlipping = true;
    public bool preserveScale = true;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showGizmos = true;
    
    // Private variables
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;
    private SpriteRenderer spriteRenderer;
    private float lastAttackTime = 0f;
    private EnemyState currentState = EnemyState.Idle;
    private bool isInitialized = false;
    
    // Scale preservation
    private Vector3 baseScale;
    private bool facingRight = true;
    
    void Start()
    {
        InitializeEnemy();
    }
    
    void InitializeEnemy()
    {
        // Store base scale
        baseScale = transform.localScale;
        
        // Get own health component
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            Debug.LogWarning($"⚠️ {gameObject.name} - No EnemyHealth component found!");
        }
        
        // Find player
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
        {
            Debug.Log($"🤖 {gameObject.name} AI initialized");
            Debug.Log($"   Base Scale: {baseScale}");
        }
    }
    
    void Update()
    {
        if (!isInitialized || playerTransform == null) return;
        
        // Check if enemy is dead
        if (enemyHealth != null && enemyHealth.IsDead())
        {
            return;
        }
        
        // Preserve scale
        if (preserveScale)
        {
            Vector3 currentScale = transform.localScale;
            if (Mathf.Abs(currentScale.x) != Mathf.Abs(baseScale.x) || 
                Mathf.Abs(currentScale.y) != Mathf.Abs(baseScale.y))
            {
                transform.localScale = new Vector3(
                    facingRight ? Mathf.Abs(baseScale.x) : -Mathf.Abs(baseScale.x),
                    baseScale.y,
                    baseScale.z
                );
            }
        }
        
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Update AI state
        UpdateAIState(distanceToPlayer);
        
        // Execute current state
        ExecuteCurrentState(distanceToPlayer);
    }
    
    void UpdateAIState(float distanceToPlayer)
    {
        EnemyState newState = currentState;
        
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
                // Just wait
                break;
                
            case EnemyState.Chasing:
                ChasePlayer();
                break;
                
            case EnemyState.Attacking:
                AttackPlayer();
                break;
        }
    }
    
    void SetState(EnemyState newState)
    {
        if (currentState == newState) return;
        
        EnemyState oldState = currentState;
        currentState = newState;
        
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
        
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        Vector3 newPosition = transform.position + direction * moveSpeed * Time.deltaTime;
        transform.position = newPosition;
        
        FaceDirection(direction);
    }
    
    void AttackPlayer()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        if (playerHealth == null) return;
        
        PerformAttack();
    }
    
    void PerformAttack()
    {
        if (showDebugLogs)
            Debug.Log($"🔥 {gameObject.name} attacking player for {attackDamage} damage!");
        
        playerHealth.TakeDamage(attackDamage, gameObject);
        lastAttackTime = Time.time;
        
        if (showDebugLogs)
        {
            Debug.Log($"⚔️ {gameObject.name} attack completed!");
        }
    }
    
    void FaceDirection(Vector3 direction)
    {
        if (!enableFlipping || direction.magnitude < 0.1f) return;
        
        bool shouldFaceRight = direction.x > 0;
        
        if (shouldFaceRight != facingRight)
        {
            facingRight = shouldFaceRight;
            
            if (preserveScale)
            {
                transform.localScale = new Vector3(
                    facingRight ? Mathf.Abs(baseScale.x) : -Mathf.Abs(baseScale.x),
                    baseScale.y,
                    baseScale.z
                );
            }
        }
    }
    
    public void UpdateBaseScale(Vector3 newBaseScale)
    {
        baseScale = newBaseScale;
        
        transform.localScale = new Vector3(
            facingRight ? Mathf.Abs(baseScale.x) : -Mathf.Abs(baseScale.x),
            baseScale.y,
            baseScale.z
        );
        
        if (showDebugLogs)
        {
            Debug.Log($"🔄 {gameObject.name} base scale updated to: {baseScale}");
        }
    }
    
    // Public methods
    public float GetDistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue;
        return Vector3.Distance(transform.position, playerTransform.position);
    }
    
    public bool IsAlive()
    {
        return enemyHealth != null ? enemyHealth.IsAlive() : true;
    }
    
    public Vector3 GetBaseScale()
    {
        return baseScale;
    }
    
    public bool IsFacingRight()
    {
        return facingRight;
    }
    
    [ContextMenu("Show Enemy Status")]
    public void ShowEnemyStatus()
    {
        Debug.Log($"🤖 === {gameObject.name} STATUS ===");
        Debug.Log($"Current State: {currentState}");
        Debug.Log($"Is Alive: {IsAlive()}");
        Debug.Log($"Base Scale: {baseScale}");
        Debug.Log($"Current Scale: {transform.localScale}");
        Debug.Log($"Facing Right: {facingRight}");
        
        if (enemyHealth != null)
        {
            Debug.Log($"Enemy Health: {enemyHealth.GetCurrentHealth():F1}/{enemyHealth.GetMaxHealth():F1}");
        }
        
        if (playerTransform != null)
        {
            float distance = GetDistanceToPlayer();
            Debug.Log($"Distance to Player: {distance:F1}m");
        }
    }
    
    // Gizmos
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distance <= attackRange)
                Gizmos.color = Color.red;
            else if (distance <= detectionRange)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.gray;
                
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
    
    void OnValidate()
    {
        detectionRange = Mathf.Max(0.1f, detectionRange);
        attackRange = Mathf.Max(0.1f, attackRange);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        attackDamage = Mathf.Max(1f, attackDamage);
        attackCooldown = Mathf.Max(0.1f, attackCooldown);
        
        if (attackRange > detectionRange)
            attackRange = detectionRange;
    }
}