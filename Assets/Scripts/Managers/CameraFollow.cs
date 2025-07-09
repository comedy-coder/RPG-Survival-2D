using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public bool autoFindPlayer = true;
    
    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0, 0, -10);
    public float smoothSpeed = 0.125f;
    public bool useFixedUpdate = false;
    
    [Header("Look Ahead")]
    public bool enableLookAhead = true;
    public float lookAheadDistance = 2f;
    public float lookAheadSpeed = 2f;
    
    [Header("Camera Bounds")]
    public bool useBounds = false;
    public Vector2 minBounds = new Vector2(-10, -10);
    public Vector2 maxBounds = new Vector2(10, 10);
    
    [Header("Combat Camera")]
    public bool combatCameraMode = true;
    public float combatZoomOut = 2f;
    public float combatTransitionSpeed = 1f;
    
    [Header("Shake Effect")]
    public bool enableShake = true;
    public float shakeMagnitude = 0.1f;
    public float shakeDuration = 0.1f;
    
    // Private variables
    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private Vector3 currentLookAhead = Vector3.zero;
    private Vector3 lastTargetPosition;
    private bool isInCombat = false;
    private float defaultCameraSize;
    private float shakeTimer = 0f;
    private Vector3 originalPosition;
    
    // Component references
    private CombatManager combatManager;
    private Rigidbody2D targetRigidbody;
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        cam = GetComponent<Camera>();
        
        // Store default camera size
        if (cam != null)
        {
            defaultCameraSize = cam.orthographicSize;
        }
        
        // Auto find player if not assigned
        if (autoFindPlayer && target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("🎥 Camera auto-found Player target");
            }
            else
            {
                Debug.LogWarning("⚠️ Camera: Player not found! Make sure Player has 'Player' tag");
            }
        }
        
        // Get component references
        combatManager = FindFirstObjectByType<CombatManager>();
        if (target != null)
        {
            targetRigidbody = target.GetComponent<Rigidbody2D>();
            lastTargetPosition = target.position;
        }
        
        // Subscribe to combat events
        if (combatManager != null && combatCameraMode)
        {
            CombatManager.OnCombatStateChanged += HandleCombatStateChanged;
            CombatManager.OnDamageDealt += HandleDamageDealt;
        }
        
        Debug.Log($"🎥 Camera Follow initialized - Target: {(target != null ? target.name : "None")}");
    }
    
    void Update()
    {
        if (!useFixedUpdate)
        {
            UpdateCamera();
        }
        
        // Handle camera shake
        if (shakeTimer > 0)
        {
            UpdateShake();
        }
    }
    
    void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            UpdateCamera();
        }
    }
    
    void LateUpdate()
    {
        if (!useFixedUpdate)
        {
            UpdateCamera();
        }
    }
    
    void UpdateCamera()
    {
        if (target == null) return;
        
        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;
        
        // Add look ahead
        if (enableLookAhead)
        {
            Vector3 lookAheadTarget = CalculateLookAhead();
            currentLookAhead = Vector3.Lerp(currentLookAhead, lookAheadTarget, lookAheadSpeed * Time.deltaTime);
            desiredPosition += currentLookAhead;
        }
        
        // Apply bounds
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        }
        
        // Smooth movement
        Vector3 smoothedPosition;
        if (smoothSpeed > 0)
        {
            smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
        }
        else
        {
            smoothedPosition = desiredPosition;
        }
        
        // Store original position for shake
        originalPosition = smoothedPosition;
        
        // Apply shake if active
        if (shakeTimer > 0)
        {
            smoothedPosition += GetShakeOffset();
        }
        
        transform.position = smoothedPosition;
        
        // Update combat camera zoom
        if (combatCameraMode && cam != null)
        {
            UpdateCombatZoom();
        }
    }
    
    Vector3 CalculateLookAhead()
    {
        Vector3 lookAhead = Vector3.zero;
        
        // Method 1: Use Rigidbody2D velocity if available
        if (targetRigidbody != null)
        {
            Vector3 playerVelocity = targetRigidbody.linearVelocity;
            if (playerVelocity.magnitude > 0.1f)
            {
                lookAhead = playerVelocity.normalized * lookAheadDistance;
            }
        }
        // Method 2: Calculate velocity from position change
        else if (target != null)
        {
            Vector3 calculatedVelocity = (target.position - lastTargetPosition) / Time.deltaTime;
            if (calculatedVelocity.magnitude > 0.1f)
            {
                lookAhead = calculatedVelocity.normalized * lookAheadDistance;
            }
            lastTargetPosition = target.position;
        }
        
        // Method 3: Fallback to input direction
        if (lookAhead.magnitude < 0.1f)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            Vector3 inputDirection = new Vector3(horizontal, vertical, 0).normalized;
            if (inputDirection.magnitude > 0.1f)
            {
                lookAhead = inputDirection * lookAheadDistance;
            }
        }
        
        return lookAhead;
    }
    
    void UpdateCombatZoom()
    {
        if (cam == null) return;
        
        float targetSize = isInCombat ? defaultCameraSize + combatZoomOut : defaultCameraSize;
        float currentSize = cam.orthographicSize;
        
        if (Mathf.Abs(currentSize - targetSize) > 0.1f)
        {
            cam.orthographicSize = Mathf.Lerp(currentSize, targetSize, combatTransitionSpeed * Time.deltaTime);
        }
    }
    
    #region Shake System
    
    void UpdateShake()
    {
        shakeTimer -= Time.deltaTime;
        
        if (shakeTimer <= 0)
        {
            shakeTimer = 0;
        }
    }
    
    Vector3 GetShakeOffset()
    {
        if (shakeTimer <= 0) return Vector3.zero;
        
        float shakeAmount = shakeMagnitude * (shakeTimer / shakeDuration);
        
        return new Vector3(
            Random.Range(-shakeAmount, shakeAmount),
            Random.Range(-shakeAmount, shakeAmount),
            0
        );
    }
    
    public void ShakeCamera(float magnitude, float duration)
    {
        if (!enableShake) return;
        
        shakeMagnitude = magnitude;
        shakeDuration = duration;
        shakeTimer = duration;
    }
    
    public void ShakeCamera()
    {
        ShakeCamera(shakeMagnitude, shakeDuration);
    }
    
    #endregion
    
    #region Combat Events
    
    void HandleCombatStateChanged(bool inCombat)
    {
        isInCombat = inCombat;
        
        Debug.Log($"🎥 Camera combat mode: {(inCombat ? "ON" : "OFF")}");
    }
    
    void HandleDamageDealt(CombatEntity attacker, CombatEntity target, int damage)
    {
        // Shake camera when player takes damage
        if (target != null && target.entityType == CombatEntityType.Player)
        {
            float shakeIntensity = Mathf.Min(damage / 20f, 1f);
            ShakeCamera(shakeMagnitude * shakeIntensity, shakeDuration);
        }
    }
    
    #endregion
    
    #region Manual Controls
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        
        // Get new rigidbody reference
        if (target != null)
        {
            targetRigidbody = target.GetComponent<Rigidbody2D>();
            lastTargetPosition = target.position;
        }
        
        Debug.Log($"🎥 Camera target changed to: {(target != null ? target.name : "None")}");
    }
    
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
        
        Debug.Log($"🎥 Camera bounds set: {min} to {max}");
    }
    
    public void DisableBounds()
    {
        useBounds = false;
        Debug.Log("🎥 Camera bounds disabled");
    }
    
    public void ResetCamera()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            velocity = Vector3.zero;
            currentLookAhead = Vector3.zero;
            shakeTimer = 0;
            lastTargetPosition = target.position;
            
            if (cam != null)
            {
                cam.orthographicSize = defaultCameraSize;
            }
        }
        
        Debug.Log("🎥 Camera reset to target position");
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Find Player Target")]
    public void FindPlayerTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SetTarget(player.transform);
            Debug.Log("✅ Player target found and assigned");
        }
        else
        {
            Debug.LogWarning("❌ Player not found! Check Player tag");
        }
    }
    
    [ContextMenu("Test Camera Shake")]
    public void TestCameraShake()
    {
        ShakeCamera(0.3f, 0.5f);
    }
    
    [ContextMenu("Reset Camera Position")]
    public void ResetCameraPosition()
    {
        ResetCamera();
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw camera bounds
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, (minBounds.y + maxBounds.y) / 2f, 0);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0);
            Gizmos.DrawWireCube(center, size);
        }
        
        // Draw look ahead direction
        if (enableLookAhead && target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(target.position, target.position + currentLookAhead);
            Gizmos.DrawWireSphere(target.position + currentLookAhead, 0.5f);
        }
        
        // Draw target connection
        if (target != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (combatManager != null)
        {
            CombatManager.OnCombatStateChanged -= HandleCombatStateChanged;
            CombatManager.OnDamageDealt -= HandleDamageDealt;
        }
    }
}