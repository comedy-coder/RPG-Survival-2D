using UnityEngine;
using UnityEngine.UI;

public class SimpleHealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public bool alwaysShow = true;
    
    // Health Bar Components
    private Canvas healthBarCanvas;
    private Slider healthBar;
    private Image healthBarFill;
    
    void Start()
    {
        CreateHealthBar();
        UpdateHealthBar();
    }
    
    void CreateHealthBar()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * 1.5f;
        
        healthBarCanvas = canvasObj.AddComponent<Canvas>();
        healthBarCanvas.renderMode = RenderMode.WorldSpace;
        healthBarCanvas.sortingOrder = 10;
        
        // Scale for visibility
        canvasObj.transform.localScale = Vector3.one * 0.01f;
        
        // Create background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.8f);
        bgRect.sizeDelta = new Vector2(100f, 20f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.anchorMin = Vector2.one * 0.5f;
        bgRect.anchorMax = Vector2.one * 0.5f;
        
        // Create health fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        healthBarFill = fillObj.AddComponent<Image>();
        healthBarFill.color = Color.green;
        fillRect.sizeDelta = new Vector2(100f, 20f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.anchorMin = Vector2.one * 0.5f;
        fillRect.anchorMax = Vector2.one * 0.5f;
        
        // Create slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(canvasObj.transform);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        healthBar = sliderObj.AddComponent<Slider>();
        sliderRect.sizeDelta = new Vector2(100f, 20f);
        sliderRect.anchoredPosition = Vector2.zero;
        sliderRect.anchorMin = Vector2.one * 0.5f;
        sliderRect.anchorMax = Vector2.one * 0.5f;
        
        healthBar.fillRect = fillRect;
        healthBar.minValue = 0f;
        healthBar.maxValue = 1f;
        healthBar.value = 1f;
        healthBar.interactable = false;
        
        healthBarCanvas.gameObject.SetActive(alwaysShow);
        
        Debug.Log($"Health bar created for {gameObject.name}");
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        UpdateHealthBar();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void UpdateHealthBar()
    {
        if (healthBar != null && healthBarFill != null)
        {
            float healthPercent = currentHealth / maxHealth;
            healthBar.value = healthPercent;
            
            // Change color based on health
            if (healthPercent > 0.6f)
                healthBarFill.color = Color.green;
            else if (healthPercent > 0.3f)
                healthBarFill.color = Color.yellow;
            else
                healthBarFill.color = Color.red;
            
            // Always show when damaged
            if (healthBarCanvas != null)
            {
                healthBarCanvas.gameObject.SetActive(alwaysShow || healthPercent < 1f);
            }
        }
    }
    
    void Die()
    {
        Debug.Log($"{gameObject.name} died!");
        
        // Hide health bar
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(false);
        }
        
        // Destroy enemy
        Destroy(gameObject, 1f);
    }
    
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
    
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
}