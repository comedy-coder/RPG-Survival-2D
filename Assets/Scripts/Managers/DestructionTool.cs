using UnityEngine;

public class DestructionTool : MonoBehaviour
{
    [Header("Tool Settings")]
    public string toolType = "Hammer";
    public int damagePerHit = 25;
    public float attackRange = 2f;
    public float attackCooldown = 0.5f;
    
    [Header("Tool Effectiveness")]
    public float woodEffectiveness = 1f;
    public float stoneEffectiveness = 0.7f;
    public float metalEffectiveness = 0.3f;
    
    [Header("Visual Effects")]
    public GameObject hitEffect;
    public AudioClip hitSound;
    public AudioClip breakSound;
    
    [Header("UI Feedback")]
    public bool showDamageNumbers = true;
    public Color damageNumberColor = Color.red;
    
    [Header("Debug")]
    public bool showDebugInfo = false; // ⭐ DEFAULT FALSE
    
    [Header("Build Mode Protection")]
    public bool respectBuildMode = true;
    public float buildModeDelay = 1f;
    
    // Private variables
    private float lastAttackTime = 0f;
    private Camera mainCamera;
    private BuildingManager buildingManager;
    private float lastBuildModeTime = 0f;
    private bool wasBuildModeActive = false;
    
    // ⭐ ANTI-SPAM SYSTEM
    private float lastDebugLogTime = 0f;
    private const float DEBUG_LOG_COOLDOWN = 1f; // Log mỗi 1 giây thay vì mỗi frame
    
    void Start()
    {
        mainCamera = Camera.main;
        buildingManager = GetComponent<BuildingManager>();
        
        if (showDebugInfo)
            Debug.Log($"🔨 DestructionTool ({toolType}): Ready");
    }
    
    void Update()
    {
        // ⭐ SMART BUILD MODE DETECTION
        bool isCurrentlyInBuildMode = IsInBuildMode();
        
        // Track build mode state changes
        if (wasBuildModeActive && !isCurrentlyInBuildMode)
        {
            // Just exited build mode
            lastBuildModeTime = Time.time;
            if (showDebugInfo)
                Debug.Log("🔨 Build mode ended - starting protection delay");
        }
        wasBuildModeActive = isCurrentlyInBuildMode;
        
        // ⭐ BLOCK DESTRUCTION IN MULTIPLE SCENARIOS
        if (ShouldBlockDestruction())
        {
            return; // Block all destruction input
        }
        
        // Handle attack input
        if (Input.GetMouseButtonDown(0))
        {
            TryAttack();
        }
    }
    
    // ⭐ COMPREHENSIVE BLOCKING LOGIC (NO SPAM)
    bool ShouldBlockDestruction()
    {
        if (!respectBuildMode) return false;
        
        bool shouldLog = showDebugInfo && (Time.time - lastDebugLogTime > DEBUG_LOG_COOLDOWN);
        
        // Block if currently in build mode
        if (IsInBuildMode())
        {
            if (shouldLog)
            {
                Debug.Log("🚫 Destruction blocked: Currently in build mode");
                lastDebugLogTime = Time.time;
            }
            return true;
        }
        
        // Block for delay period after exiting build mode
        if (Time.time - lastBuildModeTime < buildModeDelay)
        {
            if (shouldLog)
            {
                Debug.Log($"🚫 Destruction blocked: Build mode protection ({buildModeDelay - (Time.time - lastBuildModeTime):F1}s remaining)");
                lastDebugLogTime = Time.time;
            }
            return true;
        }
        
        // Block if B key is being held
        if (Input.GetKey(KeyCode.B))
        {
            if (shouldLog)
            {
                Debug.Log("🚫 Destruction blocked: B key held");
                lastDebugLogTime = Time.time;
            }
            return true;
        }
        
        return false;
    }
    
    // ⭐ IMPROVED BUILD MODE DETECTION
    bool IsInBuildMode()
    {
        // Method 1: Check BuildingManager if available
        if (buildingManager != null)
        {
            // Try to access isBuildingMode field via reflection
            var field = buildingManager.GetType().GetField("isBuildingMode", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                return (bool)field.GetValue(buildingManager);
            }
            
            // Alternative: Check for preview objects
            Transform[] children = buildingManager.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.name.Contains("Preview") || child.name.Contains("preview"))
                {
                    return true;
                }
            }
        }
        
        // Method 2: Check for build preview objects in scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Building_") && obj.name.Contains("(Clone)"))
            {
                // Check if it's a preview (transparent sprite)
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null && sr.color.a < 1f)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    void TryAttack()
    {
        // Check cooldown
        if (Time.time - lastAttackTime < attackCooldown)
        {
            if (showDebugInfo)
                Debug.Log("🔨 Attack on cooldown");
            return;
        }
        
        // Get mouse world position
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        
        if (showDebugInfo)
            Debug.Log($"🔨 {toolType} attack at {mouseWorldPos}");
        
        // Find target
        BuildingComponent target = FindNearestBuilding(mouseWorldPos);
        
        if (target != null)
        {
            AttackBuilding(target);
            lastAttackTime = Time.time;
        }
        else
        {
            if (showDebugInfo)
                Debug.Log($"❌ No building found within {attackRange}m range");
        }
    }
    
    BuildingComponent FindNearestBuilding(Vector3 position)
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(position, attackRange);
        
        BuildingComponent closestBuilding = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider2D col in nearbyColliders)
        {
            if (col != null)
            {
                BuildingComponent building = col.GetComponent<BuildingComponent>();
                if (building != null)
                {
                    // ⭐ SKIP PREVIEW BUILDINGS
                    SpriteRenderer sr = col.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.color.a < 1f)
                    {
                        continue; // Skip transparent preview buildings
                    }
                    
                    float distance = Vector2.Distance(position, col.transform.position);
                    
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestBuilding = building;
                    }
                }
            }
        }
        
        return closestBuilding;
    }
    
    void AttackBuilding(BuildingComponent building)
    {
        // Calculate effectiveness
        float effectiveness = GetEffectiveness(building.buildingName);
        int finalDamage = Mathf.RoundToInt(damagePerHit * effectiveness);
        
        if (showDebugInfo)
        {
            Debug.Log($"🎯 Attacking {building.buildingName} with {toolType}");
            Debug.Log($"   Base Damage: {damagePerHit}, Effectiveness: {effectiveness}x, Final: {finalDamage}");
        }
        
        // Apply damage
        building.TakeDamage(finalDamage);
        
        // Visual/Audio feedback
        PlayHitEffects(building.transform.position);
        
        // Show damage number
        if (showDamageNumbers)
        {
            ShowDamageNumber(building.transform.position, finalDamage);
        }
    }
    
    float GetEffectiveness(string buildingName)
    {
        if (string.IsNullOrEmpty(buildingName))
            return 1f;
            
        string lowerName = buildingName.ToLower();
        
        if (lowerName.Contains("wood") || lowerName.Contains("foundation"))
            return woodEffectiveness;
        else if (lowerName.Contains("stone") || lowerName.Contains("wall"))
            return stoneEffectiveness;
        else if (lowerName.Contains("metal") || lowerName.Contains("workbench"))
            return metalEffectiveness;
        
        return 1f;
    }
    
    void PlayHitEffects(Vector3 position)
    {
        if (hitEffect != null)
        {
            Instantiate(hitEffect, position, Quaternion.identity);
        }
        
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, position);
        }
    }
    
    void ShowDamageNumber(Vector3 position, int damage)
    {
        if (showDebugInfo)
        {
            Debug.Log($"💥 -{damage} damage");
        }
    }
    
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
    
    // ⭐ PUBLIC CONTROL METHODS
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        
        if (showDebugInfo)
            Debug.Log($"🔨 DestructionTool: {(enabled ? "ENABLED" : "DISABLED")}");
    }
    
    public void SetBuildModeRespect(bool respect)
    {
        respectBuildMode = respect;
        Debug.Log($"🔨 Build mode respect: {respectBuildMode}");
    }
    
    public bool IsOnCooldown()
    {
        return Time.time - lastAttackTime < attackCooldown;
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (Application.isPlaying && mainCamera != null)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(mousePos, 0.2f);
        }
    }
}