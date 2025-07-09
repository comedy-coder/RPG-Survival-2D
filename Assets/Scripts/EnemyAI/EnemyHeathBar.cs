using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AdvancedHealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    
    [Header("Visual Settings")]
    public Vector3 offset = new Vector3(0, 0.2f, 0); // ✅ Closer to enemy
    public Vector2 size = new Vector2(1.0f, 0.12f); // ✅ Longer width
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
    public bool pulseWhenLow = true;
    
    // Components
    private Canvas canvas;
    private Slider healthSlider;
    private Image fillImage;
    private Image backgroundImage;
    private RectTransform canvasRect;
    private Camera mainCamera;
    
    // Animation
    private float targetHealth;
    private bool isVisible = true;
    private Coroutine hideCoroutine;
    private Coroutine pulseCoroutine;
    
    void Start()
    {
        mainCamera = Camera.main;
        targetHealth = currentHealth;
        
        // ✅ FORCE SETTINGS: Longer health bar, closer to enemy
        offset = new Vector3(0, 0.2f, 0); 
        size = new Vector2(1.0f, 0.12f);
        
        // ✅ DELETE OLD HEALTH BAR if exists
        Transform existingHealthBar = transform.Find("AdvancedHealthBar");
        if (existingHealthBar != null)
        {
            DestroyImmediate(existingHealthBar.gameObject);
            Debug.Log("🗑️ Deleted old health bar");
        }
        
        CreateHealthBar();
        
        if (!alwaysVisible)
        {
            SetVisibility(false);
        }
        
        Debug.Log($"✅ Fixed Health Bar created: Size={size}, Position={offset}");
    }
    
    void CreateHealthBar()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("AdvancedHealthBar");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = offset;
        
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1000;
        
        // Perfect scale for world space
        canvasObj.transform.localScale = Vector3.one * 0.005f;
        
        // Add Canvas Scaler for better resolution
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        
        canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = size;
        
        // Create Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform);
        
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        backgroundImage = bgObj.AddComponent<Image>();
        
        // Background setup
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        backgroundImage.color = backgroundColor;
        
        // Create Slider
        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(canvasObj.transform);
        
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
        if (smoothTransition && Mathf.Abs(currentHealth - targetHealth) > 0.1f)
        {
            currentHealth = Mathf.Lerp(currentHealth, targetHealth, transitionSpeed * Time.deltaTime);
            UpdateHealthBar();
        }
        
        // ✅ FIXED: Keep health bar horizontal (no rotation from enemy)
        if (canvas != null)
        {
            // Force position and scale
            canvas.transform.localPosition = offset;
            canvas.transform.localScale = Vector3.one * 0.005f;
            
            // ✅ MOST IMPORTANT: Always keep horizontal, ignore parent rotation
            canvas.transform.rotation = Quaternion.identity;
            
            // Update size
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = size;
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        float newHealth = currentHealth - damage;
        SetHealth(newHealth);
        
        Debug.Log($"💥 {gameObject.name} took {damage} damage. Health: {currentHealth:F1}/{maxHealth}");
        
        // Show health bar when damaged
        if (!alwaysVisible)
        {
            ShowHealthBar();
        }
        
        // Flash effect
        StartCoroutine(DamageFlash());
    }
    
    public void Heal(float amount)
    {
        float newHealth = currentHealth + amount;
        SetHealth(newHealth);
        
        Debug.Log($"💚 {gameObject.name} healed {amount}. Health: {currentHealth:F1}/{maxHealth}");
        
        // Show health bar when healed
        if (!alwaysVisible)
        {
            ShowHealthBar();
        }
    }
    
    public void SetHealth(float health)
    {
        if (smoothTransition)
        {
            targetHealth = Mathf.Clamp(health, 0, maxHealth);
        }
        else
        {
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
            targetHealth = currentHealth;
            UpdateHealthBar();
        }
    }
    
    void UpdateHealthBar()
    {
        if (healthSlider == null || fillImage == null) return;
        
        float healthPercent = currentHealth / maxHealth;
        healthSlider.value = healthPercent;
        
        // Update color based on health
        Color targetColor = GetHealthColor(healthPercent);
        fillImage.color = targetColor;
        
        // Start pulsing if health is low
        if (pulseWhenLow && healthPercent < 0.3f)
        {
            if (pulseCoroutine == null)
            {
                pulseCoroutine = StartCoroutine(PulseEffect());
            }
        }
        else
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
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
    
    IEnumerator DamageFlash()
    {
        if (backgroundImage != null)
        {
            Color originalColor = backgroundImage.color;
            backgroundImage.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            backgroundImage.color = originalColor;
        }
    }
    
    IEnumerator PulseEffect()
    {
        float originalAlpha = fillImage.color.a;
        
        while (true)
        {
            // Fade out
            for (float t = 0; t < 1; t += Time.deltaTime * 2f)
            {
                Color color = fillImage.color;
                color.a = Mathf.Lerp(originalAlpha, 0.3f, t);
                fillImage.color = color;
                yield return null;
            }
            
            // Fade in
            for (float t = 0; t < 1; t += Time.deltaTime * 2f)
            {
                Color color = fillImage.color;
                color.a = Mathf.Lerp(0.3f, originalAlpha, t);
                fillImage.color = color;
                yield return null;
            }
        }
    }
    
    public void ShowHealthBar()
    {
        SetVisibility(true);
        
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        
        if (autoHide && !alwaysVisible)
        {
            hideCoroutine = StartCoroutine(HideAfterDelay());
        }
    }
    
    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(autoHideDelay);
        SetVisibility(false);
    }
    
    public void SetVisibility(bool visible)
    {
        isVisible = visible;
        if (canvas != null)
        {
            canvas.gameObject.SetActive(visible);
        }
    }
    
    // Public getters
    public float GetHealthPercent() => currentHealth / maxHealth;
    public bool IsAlive() => currentHealth > 0;
    public bool IsVisible() => isVisible;
    
    // Health bar customization
    public void SetColors(Color healthy, Color damaged, Color critical)
    {
        healthyColor = healthy;
        damagedColor = damaged;
        criticalColor = critical;
        UpdateHealthBar();
    }
    
    public void SetSize(Vector2 newSize)
    {
        size = newSize;
        if (canvasRect != null)
        {
            canvasRect.sizeDelta = size;
        }
    }
    
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
        if (canvas != null)
        {
            canvas.transform.localPosition = offset;
        }
    }
    
    // ✅ CONTEXT MENU METHODS for easy testing
    [ContextMenu("Make Health Bar Longer")]
    public void MakeLonger()
    {
        size.x = 1.5f;
        SetSize(size);
        Debug.Log($"📏 Health bar made longer: {size.x}");
    }
    
    [ContextMenu("Make Health Bar Shorter")]
    public void MakeShorter()
    {
        size.x = 0.8f;
        SetSize(size);
        Debug.Log($"📏 Health bar made shorter: {size.x}");
    }
    
    [ContextMenu("Move Health Bar Up")]
    public void MoveUp()
    {
        offset.y += 0.1f;
        SetOffset(offset);
        Debug.Log($"⬆️ Health bar moved up: Y = {offset.y}");
    }
    
    [ContextMenu("Move Health Bar Down")]
    public void MoveDown()
    {
        offset.y -= 0.1f;
        SetOffset(offset);
        Debug.Log($"⬇️ Health bar moved down: Y = {offset.y}");
    }
    
    [ContextMenu("Test Damage")]
    public void TestDamage()
    {
        TakeDamage(25f);
    }
    
    [ContextMenu("Force Recreate Health Bar")]
    public void ForceRecreateHealthBar()
    {
        // Delete old health bar
        if (canvas != null)
        {
            DestroyImmediate(canvas.gameObject);
        }
        
        // Create new health bar
        CreateHealthBar();
        
        if (!alwaysVisible)
        {
            SetVisibility(false);
        }
        
        Debug.Log("🔄 Health bar recreated!");
    }
    
    void OnDestroy()
    {
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
    }
}