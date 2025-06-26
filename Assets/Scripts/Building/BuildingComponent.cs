using UnityEngine;

public class BuildingComponent : MonoBehaviour
{
    [Header("Building Information")]
    public string buildingName = "Building";
    public string buildingDescription = "A building";
    
    [Header("Building Stats")]
    public int maxDurability = 100;
    public int currentDurability = 100; // ⭐ SET DEFAULT VALUE
    public bool canBeDestroyed = true;
    
    [Header("Building Category")]
    public string buildingCategory = "Structure";
    
    [Header("Construction")]
    public float constructionTime = 2f;
    public bool isConstructed = true;
    
    void Start()
    {
        InitializeBuilding();
    }
    
    void InitializeBuilding()
    {
        // ⭐ ENSURE PROPER INITIALIZATION
        if (currentDurability <= 0 || currentDurability > maxDurability)
        {
            currentDurability = maxDurability;
            Debug.Log($"🔧 Fixed durability for {buildingName}: {currentDurability}/{maxDurability}");
        }
        
        Debug.Log($"🏠 Building '{buildingName}' initialized - Durability: {currentDurability}/{maxDurability}");
    }
    
    public void TakeDamage(int damage)
    {
        if (!canBeDestroyed)
        {
            Debug.Log($"🛡️ {buildingName} cannot be destroyed!");
            return;
        }
        
        if (damage <= 0)
        {
            Debug.Log($"⚠️ Invalid damage amount: {damage}");
            return;
        }
        
        // ⭐ SHOW BEFORE STATE
        int beforeDurability = currentDurability;
        
        currentDurability -= damage;
        currentDurability = Mathf.Max(0, currentDurability);
        
        ShowDamageEffect();
        
        Debug.Log($"💥 {buildingName} took {damage} damage. {beforeDurability} → {currentDurability}/{maxDurability}");
        
        if (currentDurability <= 0)
        {
            DestroyBuilding();
        }
    }
    
    public void RepairBuilding(int repairAmount)
    {
        currentDurability += repairAmount;
        currentDurability = Mathf.Min(maxDurability, currentDurability);
        
        Debug.Log($"🔧 {buildingName} repaired. Durability: {currentDurability}/{maxDurability}");
    }
    
    void DestroyBuilding()
    {
        Debug.Log($"💥 {buildingName} has been destroyed!");
        
        // ⭐ ADD RESOURCE RECOVERY
        RecoverResources();
        
        Destroy(gameObject, 0.5f);
    }
    
    // ⭐ SIMPLE RESOURCE RECOVERY
    void RecoverResources()
    {
        SimpleInventory inventory = FindObjectOfType<SimpleInventory>();
        if (inventory != null)
        {
            // 50% recovery rate
            Debug.Log($"🔄 Recovering materials from {buildingName} (50% rate)");
            
            string lowerName = buildingName.ToLower();
            
            if (lowerName.Contains("foundation"))
            {
                inventory.AddItem("Wood", 1);
                inventory.AddItem("Stone", 1);
                Debug.Log("✅ Recovered: 1x Wood, 1x Stone");
            }
            else if (lowerName.Contains("wall"))
            {
                inventory.AddItem("Wood", 1);
                inventory.AddItem("Stone", 1);
                Debug.Log("✅ Recovered: 1x Wood, 1x Stone");
            }
            else if (lowerName.Contains("workbench"))
            {
                inventory.AddItem("Wood", 2);
                Debug.Log("✅ Recovered: 2x Wood");
            }
            else
            {
                // Generic recovery
                inventory.AddItem("Wood", 1);
                Debug.Log("✅ Recovered: 1x Wood");
            }
        }
    }
    
    void ShowDamageEffect()
    {
        StartCoroutine(DamageFlash());
    }
    
    System.Collections.IEnumerator DamageFlash()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color originalColor = sr.color;
            sr.color = Color.red;
            
            yield return new WaitForSeconds(0.1f);
            
            sr.color = originalColor;
        }
    }
    
    public bool IsDestroyed()
    {
        return currentDurability <= 0;
    }
    
    public bool IsDamaged()
    {
        return currentDurability < maxDurability;
    }
    
    public float GetDurabilityPercentage()
    {
        if (maxDurability <= 0)
            return 0f;
        
        return (float)currentDurability / maxDurability;
    }
    
    public string GetBuildingInfo()
    {
        string status = "";
        
        if (currentDurability <= 0)
            status = "Destroyed";
        else if (currentDurability < maxDurability)
            status = "Damaged";
        else
            status = "Healthy";
        
        return $"{buildingName}\nCategory: {buildingCategory}\nDurability: {currentDurability}/{maxDurability}\nStatus: {status}";
    }
    
    // ⭐ PUBLIC METHOD TO FORCE FULL HEALTH
    [ContextMenu("Force Full Health")]
    public void ForceFullHealth()
    {
        currentDurability = maxDurability;
        Debug.Log($"💚 {buildingName} health restored to full: {currentDurability}/{maxDurability}");
    }
    
    // ⭐ PUBLIC METHOD TO SET CUSTOM HEALTH
    public void SetHealth(int health)
    {
        currentDurability = Mathf.Clamp(health, 0, maxDurability);
        Debug.Log($"❤️ {buildingName} health set to: {currentDurability}/{maxDurability}");
    }
}