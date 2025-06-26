using UnityEngine;
using UnityEngine.UI;

public class SurvivalManager : MonoBehaviour
{
    [Header("Survival Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float maxThirst = 100f;
    
    [Header("Current Values")]
    public float currentHealth;
    public float currentHunger;
    public float currentThirst;
    
    [Header("Degradation Rates (per second)")]
    [SerializeField] private float hungerDecayRate = 1f;
    [SerializeField] private float thirstDecayRate = 1.2f;
    [SerializeField] private float healthDecayRate = 0.5f; // when hungry/thirsty
    
    [Header("Critical Thresholds")]
    [SerializeField] private float criticalThreshold = 20f;
    [SerializeField] private float dangerThreshold = 40f;
    
    [Header("UI References")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider hungerBar;
    [SerializeField] private Slider thirstBar;
    
    [Header("UI Colors")]
    [SerializeField] private Color healthColor = Color.red;
    [SerializeField] private Color hungerColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color thirstColor = Color.blue;
    [SerializeField] private Color criticalColor = Color.red;
    
    // Events for other systems
    public System.Action OnPlayerDied;
    public System.Action<float> OnHealthChanged;
    public System.Action<float> OnHungerChanged;
    public System.Action<float> OnThirstChanged;
    
    private bool isDead = false;
    
    void Start()
    {
        // Initialize survival stats
        InitializeSurvivalStats();
        
        // Setup UI
        SetupUI();
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Decay survival stats over time
        DecaySurvivalStats();
        
        // Update UI
        UpdateUI();
        
        // Check for critical conditions
        CheckCriticalConditions();
    }
    
    void InitializeSurvivalStats()
    {
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        currentThirst = maxThirst;
        
        Debug.Log("Survival Manager: Stats initialized");
    }
    
    void SetupUI()
    {
        // Setup health bar
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
            SetSliderColor(healthBar, healthColor);
        }
        
        // Setup hunger bar
        if (hungerBar != null)
        {
            hungerBar.maxValue = maxHunger;
            hungerBar.value = currentHunger;
            SetSliderColor(hungerBar, hungerColor);
        }
        
        // Setup thirst bar
        if (thirstBar != null)
        {
            thirstBar.maxValue = maxThirst;
            thirstBar.value = currentThirst;
            SetSliderColor(thirstBar, thirstColor);
        }
    }
    
    void DecaySurvivalStats()
    {
        // Hunger decreases over time
        currentHunger -= hungerDecayRate * Time.deltaTime;
        currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
        
        // Thirst decreases over time
        currentThirst -= thirstDecayRate * Time.deltaTime;
        currentThirst = Mathf.Clamp(currentThirst, 0f, maxThirst);
        
        // Health decreases if hungry or thirsty
        if (currentHunger <= 0f || currentThirst <= 0f)
        {
            currentHealth -= healthDecayRate * Time.deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }
        
        // Trigger events
        OnHealthChanged?.Invoke(currentHealth);
        OnHungerChanged?.Invoke(currentHunger);
        OnThirstChanged?.Invoke(currentThirst);
    }
    
    void UpdateUI()
    {
        // Update slider values
        if (healthBar != null) healthBar.value = currentHealth;
        if (hungerBar != null) hungerBar.value = currentHunger;
        if (thirstBar != null) thirstBar.value = currentThirst;
        
        // Update colors based on critical levels
        UpdateBarColors();
    }
    
    void UpdateBarColors()
    {
        // Health bar color
        if (healthBar != null)
        {
            Color color = currentHealth <= criticalThreshold ? criticalColor : healthColor;
            SetSliderColor(healthBar, color);
        }
        
        // Hunger bar color
        if (hungerBar != null)
        {
            Color color = currentHunger <= criticalThreshold ? criticalColor : hungerColor;
            SetSliderColor(hungerBar, color);
        }
        
        // Thirst bar color
        if (thirstBar != null)
        {
            Color color = currentThirst <= criticalThreshold ? criticalColor : thirstColor;
            SetSliderColor(thirstBar, color);
        }
    }
    
    void SetSliderColor(Slider slider, Color color)
    {
        if (slider != null && slider.fillRect != null)
        {
            Image fillImage = slider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = color;
            }
        }
    }
    
    void CheckCriticalConditions()
    {
        // Check for death
        if (currentHealth <= 0f && !isDead)
        {
            PlayerDied();
        }
        
        // Debug warnings for critical stats
        if (currentHunger <= criticalThreshold)
        {
            Debug.Log("WARNING: Player is starving!");
        }
        
        if (currentThirst <= criticalThreshold)
        {
            Debug.Log("WARNING: Player is dehydrated!");
        }
    }
    
    void PlayerDied()
    {
        isDead = true;
        Debug.Log("GAME OVER: Player died!");
        OnPlayerDied?.Invoke();
        
        // TODO: Implement game over screen
        Time.timeScale = 0f; // Pause game temporarily
    }
    
    // Public methods for other systems
    public void RestoreHealth(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        Debug.Log($"Health restored: +{amount}");
    }
    
    public void RestoreHunger(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger + amount, 0f, maxHunger);
        Debug.Log($"Hunger restored: +{amount}");
    }
    
    public void RestoreThirst(float amount)
    {
        currentThirst = Mathf.Clamp(currentThirst + amount, 0f, maxThirst);
        Debug.Log($"Thirst restored: +{amount}");
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        Debug.Log($"Player took damage: -{damage}");
    }
    
    // Getters for other systems
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetHungerPercentage() => currentHunger / maxHunger;
    public float GetThirstPercentage() => currentThirst / maxThirst;
    
    public bool IsHealthCritical() => currentHealth <= criticalThreshold;
    public bool IsHungerCritical() => currentHunger <= criticalThreshold;
    public bool IsThirstCritical() => currentThirst <= criticalThreshold;
}