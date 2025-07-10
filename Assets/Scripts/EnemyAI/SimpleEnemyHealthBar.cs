using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// =============================================================================
// SIMPLE ENEMY HEALTH BAR - Final Clean Version
// =============================================================================

public class SimpleEnemyHealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public Vector3 offset = new Vector3(0, 1.2f, 0);
    public Vector2 size = new Vector2(1.0f, 0.12f);
    public bool alwaysVisible = false;
    public bool autoHide = true;
    public float autoHideDelay = 3f;
    
    [Header("Colors")]
    public Color healthyColor = Color.green;
    public Color damagedColor = Color.yellow;
    public Color criticalColor = Color.red;
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    [Header("Animation")]
    public bool smoothTransition = true;
    public float transitionSpeed = 5f;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    // Components
    private Canvas canvas;
    private Slider healthSlider;
    private Image fillImage;
    private Image backgroundImage;
    private RectTransform canvasRect;
    private EnemyHealth enemyHealth;
    private Coroutine hideCoroutine;
    
    // Animation
    private float targetFillAmount = 1f;
    private bool isVisible = false;
    
    void Start()
    {
        InitializeHealthBar();
    }
    
    void InitializeHealthBar()
    {
        // Get EnemyHealth component
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            Debug.LogError($"❌ {gameObject.name} - No EnemyHealth component found!");
            return;
        }
        
        // Subscribe to health events
        enemyHealth.OnHealthChanged += OnHealthChanged;
        enemyHealth.OnDeath += OnEnemyDeath;
        
        // Delete old health bar if exists
        Transform existingHealthBar = transform.Find("EnemyHealthBar");
        if (existingHealthBar != null)
        {
            DestroyImmediate(existingHealthBar.gameObject);
        }
        
        CreateHealthBar();
        
        // Set initial visibility
        if (!alwaysVisible)
        {
            SetVisibility(false);
        }
        
        // Set initial health
        UpdateHealthBar();
        
        if (showDebugLogs)
        {
            Debug.Log($"✅ {gameObject.name} health bar created");
        }
    }
    
    void CreateHealthBar()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("EnemyHealthBar");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = offset;
        canvasObj.transform.localRotation = Quaternion.identity;
        
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1000;
        
        // Set scale for world space
        canvasObj.transform.localScale = Vector3.one * 0.01f;
        
        // Add Canvas Scaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        
        canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = size * 100f; // Scale up for world space
        
        // Create Background
        CreateBackground();
        
        // Create Slider
        CreateSlider();
        
        if (showDebugLogs)
        {
            Debug.Log($"🎨 Health bar UI created with size: {size}");
        }
    }
    
    void CreateBackground()
    {
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvas.transform);
        
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        backgroundImage = bgObj.AddComponent<Image>();
        
        // Background setup
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        backgroundImage.color = backgroundColor;
    }
    
    void CreateSlider()
    {
        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(canvas.transform);
        
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        healthSlider = sliderObj.AddComponent<Slider>();
        
        // Slider setup
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;
        
        // Create Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform);
        
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;
        
        // Create Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform);
        
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillImage = fillObj.AddComponent<Image>();
        
        // Fill setup
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillImage.color = healthyColor;
        
        // Configure slider
        healthSlider.fillRect = fillRect;
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.value = 1f;
        healthSlider.interactable = false;
    }
    
    void Update()
    {
        if (canvas == null || enemyHealth == null) return;
        
        // Keep health bar horizontal (ignore parent rotation)
        canvas.transform.rotation = Quaternion.identity;
        
        // Update position
        canvas.transform.localPosition = offset;
        
        // Smooth transition
        if (smoothTransition && healthSlider != null)
        {
            float currentValue = healthSlider.value;
            float newValue = Mathf.Lerp(currentValue, targetFillAmount, transitionSpeed * Time.deltaTime);
            healthSlider.value = newValue;
        }
    }
    
    void OnHealthChanged(float currentHealth)
    {
        if (enemyHealth == null) return;
        
        // Show health bar when damaged
        if (!alwaysVisible && currentHealth < enemyHealth.GetMaxHealth())
        {
            ShowHealthBar();
        }
        
        UpdateHealthBar();
    }
    
    void UpdateHealthBar()
    {
        if (enemyHealth == null || healthSlider == null || fillImage == null) return;
        
        float healthPercent = enemyHealth.GetHealthPercent();
        targetFillAmount = healthPercent;
        
        // Update color based on health
        Color targetColor = GetHealthColor(healthPercent);
        fillImage.color = targetColor;
        
        // Instant update if not smooth
        if (!smoothTransition)
        {
            healthSlider.value = targetFillAmount;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"📊 {gameObject.name} health bar updated: {healthPercent:P1}");
        }
    }
    
    Color GetHealthColor(float healthPercent)
    {
        if (healthPercent > 0.6f)
        {
            return Color.Lerp(damagedColor, healthyColor, (healthPercent - 0.6f) / 0.4f);
        }
        else if (healthPercent > 0.3f)
        {
            return Color.Lerp(criticalColor, damagedColor, (healthPercent - 0.3f) / 0.3f);
        }
        else
        {
            return criticalColor;
        }
    }
    
    void OnEnemyDeath()
    {
        if (showDebugLogs)
        {
            Debug.Log($"💀 {gameObject.name} health bar hidden due to death");
        }
        
        SetVisibility(false);
    }
    
    public void ShowHealthBar()
    {
        SetVisibility(true);
        
        // Cancel hide coroutine
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        
        // Start auto hide
        if (autoHide && !alwaysVisible)
        {
            hideCoroutine = StartCoroutine(HideAfterDelay());
        }
    }
    
    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(autoHideDelay);
        
        if (!alwaysVisible)
        {
            SetVisibility(false);
        }
    }
    
    public void SetVisibility(bool visible)
    {
        isVisible = visible;
        
        if (canvas != null)
        {
            canvas.gameObject.SetActive(visible);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"👁️ {gameObject.name} health bar visibility: {visible}");
        }
    }
    
    // Public customization methods
    public void SetColors(Color healthy, Color damaged, Color critical, Color background)
    {
        healthyColor = healthy;
        damagedColor = damaged;
        criticalColor = critical;
        backgroundColor = background;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
        }
        
        UpdateHealthBar();
    }
    
    public void SetSize(Vector2 newSize)
    {
        size = newSize;
        
        if (canvasRect != null)
        {
            canvasRect.sizeDelta = size * 100f;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"📏 {gameObject.name} health bar size changed to: {size}");
        }
    }
    
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
        
        if (canvas != null)
        {
            canvas.transform.localPosition = offset;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"📍 {gameObject.name} health bar offset changed to: {offset}");
        }
    }
    
    // Context menu methods - Basic only
    [ContextMenu("Show Health Bar")]
    public void ForceShowHealthBar()
    {
        ShowHealthBar();
    }
    
    [ContextMenu("Hide Health Bar")]
    public void ForceHideHealthBar()
    {
        SetVisibility(false);
    }
    
    [ContextMenu("Set Small Size")]
    public void SetSmallSize()
    {
        SetSize(new Vector2(1.0f, 0.12f));
    }
    
    [ContextMenu("Set Medium Size")]
    public void SetMediumSize()
    {
        SetSize(new Vector2(1.2f, 0.15f));
    }
    
    [ContextMenu("Set Large Size")]
    public void SetLargeSize()
    {
        SetSize(new Vector2(1.5f, 0.18f));
    }
    
    [ContextMenu("Move Up")]
    public void MoveUp()
    {
        SetOffset(offset + Vector3.up * 0.2f);
    }
    
    [ContextMenu("Move Down")]
    public void MoveDown()
    {
        SetOffset(offset + Vector3.down * 0.2f);
    }
    
    [ContextMenu("Show Health Bar Info")]
    public void ShowHealthBarInfo()
    {
        Debug.Log($"📊 === {gameObject.name} HEALTH BAR INFO ===");
        Debug.Log($"   Size: {size}");
        Debug.Log($"   Offset: {offset}");
        Debug.Log($"   Always Visible: {alwaysVisible}");
        Debug.Log($"   Auto Hide: {autoHide}");
        Debug.Log($"   Is Visible: {isVisible}");
        
        if (enemyHealth != null)
        {
            Debug.Log($"   Enemy Health: {enemyHealth.GetCurrentHealth():F1}/{enemyHealth.GetMaxHealth():F1}");
        }
        
        if (canvas != null)
        {
            Debug.Log($"   Canvas: ✅ Active");
        }
    }
    
    // Cleanup
    void OnDestroy()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged -= OnHealthChanged;
            enemyHealth.OnDeath -= OnEnemyDeath;
        }
    }
    
    // Validation
    void OnValidate()
    {
        // Ensure positive values
        size.x = Mathf.Max(0.1f, size.x);
        size.y = Mathf.Max(0.1f, size.y);
        autoHideDelay = Mathf.Max(0.1f, autoHideDelay);
        transitionSpeed = Mathf.Max(0.1f, transitionSpeed);
        
        // Update in play mode
        if (Application.isPlaying && canvas != null)
        {
            canvas.transform.localPosition = offset;
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = size * 100f;
            }
        }
    }
}