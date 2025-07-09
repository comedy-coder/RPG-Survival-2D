using UnityEngine;
using System.Collections;

public class BuildingComponent : MonoBehaviour
{
    [Header("Building Data")]
    public string buildingName = "";
    public BuildingType buildingType = BuildingType.Foundation;
    public BuildingMaterial primaryMaterial = BuildingMaterial.Wood;
    
    [Header("Durability System")]
    public int maxDurability = 100;
    public int currentDurability = 100;
    public bool canBeDestroyed = true;
    public bool canBeRepaired = true;
    
    [Header("Building Rules")]
    public bool blocksMovement = true;
    public bool blocksBuilding = true;
    public bool requiresFoundation = false;
    public bool isFoundation = false;
    
    [Header("Visual Feedback")]
    public GameObject healthBarPrefab;
    public Transform healthBarParent;
    public bool showHealthBar = true;
    public bool showDamageEffects = true;
    
    [Header("Resource Recovery")]
    [Range(0f, 1f)]
    public float resourceRecoveryRate = 0.5f; // 50% recovery
    public bool enableResourceRecovery = true;
    
    [Header("Audio")]
    public AudioClip buildSound;
    public AudioClip damageSound;
    public AudioClip repairSound;
    public AudioClip destroySound;
    
    // Private variables
    private SpriteRenderer spriteRenderer;
    private GameObject healthBarInstance;
    private Transform healthBarFill;
    private Color originalColor;
    private bool isDestroyed = false;
    
    // Events
    public System.Action<BuildingComponent, int> OnDamageTaken;
    public System.Action<BuildingComponent, int> OnRepaired;
    public System.Action<BuildingComponent> OnDestroyed;
    
    #region Building Types and Materials
    
    public enum BuildingType
    {
        Foundation,     // Base structure
        Wall,          // Defensive barrier
        Workbench,     // Crafting station
        Door,          // Passable barrier
        Window,        // Light/view barrier
        Storage,       // Item container
        Defense,       // Defensive structure
        Production,    // Resource generator
        Decoration,    // Aesthetic item
        Utility        // Special function
    }
    
    public enum BuildingMaterial
    {
        Wood,          // Basic material
        Stone,         // Medium durability
        Metal,         // High durability
        Composite,     // Advanced material
        Crystal,       // Special material
        Organic        // Living material
    }
    
    #endregion
    
    #region Initialization
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }
    
    void Start()
    {
        InitializeBuilding();
    }
    
    void InitializeBuilding()
    {
        // Set default name if empty
        if (string.IsNullOrEmpty(buildingName))
            buildingName = buildingType.ToString();
        
        // Initialize durability
        currentDurability = Mathf.Clamp(currentDurability, 0, maxDurability);
        
        // Create health bar
        if (showHealthBar && healthBarPrefab != null)
        {
            CreateHealthBar();
        }
        
        // Set foundation flag
        isFoundation = (buildingType == BuildingType.Foundation);
        
        // Apply material properties
        ApplyMaterialProperties();
        
        // Play build sound
        PlaySound(buildSound);
        
        if (currentDurability > 0)
        {
            Debug.Log($"🏗️ {buildingName} initialized | Material: {primaryMaterial} | HP: {currentDurability}/{maxDurability}");
        }
    }
    
    void ApplyMaterialProperties()
    {
        // Adjust durability based on material
        switch (primaryMaterial)
        {
            case BuildingMaterial.Wood:
                // Base durability
                break;
            case BuildingMaterial.Stone:
                maxDurability = Mathf.RoundToInt(maxDurability * 1.5f);
                break;
            case BuildingMaterial.Metal:
                maxDurability = Mathf.RoundToInt(maxDurability * 2f);
                break;
            case BuildingMaterial.Composite:
                maxDurability = Mathf.RoundToInt(maxDurability * 2.5f);
                break;
            case BuildingMaterial.Crystal:
                maxDurability = Mathf.RoundToInt(maxDurability * 3f);
                break;
        }
        
        // Update current durability proportionally
        currentDurability = maxDurability;
    }
    
    #endregion
    
    #region Damage System
    
    public void TakeDamage(int damage)
    {
        if (!canBeDestroyed || isDestroyed) return;
        
        int actualDamage = Mathf.Max(0, damage);
        int oldDurability = currentDurability;
        
        currentDurability -= actualDamage;
        currentDurability = Mathf.Max(0, currentDurability);
        
        int finalDamage = oldDurability - currentDurability;
        
        // Visual and audio feedback
        if (showDamageEffects)
        {
            StartCoroutine(DamageFlashEffect());
        }
        PlaySound(damageSound);
        
        // Update health bar
        UpdateHealthBar();
        
        // Fire event
        OnDamageTaken?.Invoke(this, finalDamage);
        
        Debug.Log($"💥 {buildingName} took {finalDamage} damage | HP: {currentDurability}/{maxDurability} | Status: {GetBuildingStatus()}");
        
        // Check for destruction
        if (currentDurability <= 0)
        {
            StartCoroutine(DestroyBuildingDelayed());
        }
    }
    
    public void TakeDamageOverTime(int damage, float duration)
    {
        if (!canBeDestroyed || isDestroyed) return;
        StartCoroutine(DamageOverTimeCoroutine(damage, duration));
    }
    
    IEnumerator DamageOverTimeCoroutine(int totalDamage, float duration)
    {
        float damagePerSecond = totalDamage / duration;
        float timer = 0f;
        
        while (timer < duration && currentDurability > 0)
        {
            int frameDamage = Mathf.RoundToInt(damagePerSecond * Time.deltaTime);
            if (frameDamage > 0)
            {
                TakeDamage(frameDamage);
            }
            
            timer += Time.deltaTime;
            yield return null;
        }
    }
    
    #endregion
    
    #region Repair System
    
    public void Repair(int repairAmount)
    {
        RepairBuilding(repairAmount);
    }
    
    public void RepairBuilding(int repairAmount)
    {
        if (!canBeRepaired || isDestroyed) return;
        
        int oldDurability = currentDurability;
        currentDurability += repairAmount;
        currentDurability = Mathf.Min(maxDurability, currentDurability);
        
        int actualRepair = currentDurability - oldDurability;
        
        if (actualRepair > 0)
        {
            // Visual and audio feedback
            if (showDamageEffects)
            {
                StartCoroutine(RepairGlowEffect());
            }
            PlaySound(repairSound);
            
            // Update health bar
            UpdateHealthBar();
            
            // Fire event
            OnRepaired?.Invoke(this, actualRepair);
            
            Debug.Log($"🔧 {buildingName} repaired by {actualRepair} | HP: {currentDurability}/{maxDurability} | Status: {GetBuildingStatus()}");
        }
    }
    
    public void RepairToFull()
    {
        RepairBuilding(maxDurability - currentDurability);
    }
    
    public bool CanBeRepaired()
    {
        return canBeRepaired && !isDestroyed && currentDurability < maxDurability;
    }
    
    public int GetRepairNeeded()
    {
        return maxDurability - currentDurability;
    }
    
    #endregion
    
    #region Destruction System
    
    IEnumerator DestroyBuildingDelayed()
    {
        if (isDestroyed) yield break;
        
        isDestroyed = true;
        
        // Visual destruction effect
        if (showDamageEffects)
        {
            StartCoroutine(DestructionEffect());
        }
        
        // Play destroy sound
        PlaySound(destroySound);
        
        // Wait for effect
        yield return new WaitForSeconds(0.5f);
        
        // Resource recovery
        if (enableResourceRecovery)
        {
            RecoverMaterials();
        }
        
        // Clear grid position in BuildingManager
        ClearGridPosition();
        
        // Fire event
        OnDestroyed?.Invoke(this);
        
        Debug.Log($"💥 {buildingName} completely destroyed! Grid position cleared for rebuilding.");
        
        // Destroy GameObject
        Destroy(gameObject);
    }
    
    void ClearGridPosition()
    {
        // Find BuildingManager and clear this building's grid position
        BuildingManager buildingManager = FindFirstObjectByType<BuildingManager>();
        if (buildingManager != null)
        {
            // Use extension method to clear grid position
            buildingManager.ClearGridPosition(transform.position);
        }
        else
        {
            Debug.LogWarning("⚠️ BuildingManager not found - grid position not cleared");
        }
    }
    
    public void ForceDestroy()
    {
        if (!isDestroyed)
        {
            StartCoroutine(DestroyBuildingDelayed());
        }
    }
    
    #endregion
    
    #region Resource Recovery System
    
    void RecoverMaterials()
    {
        SimpleInventory inventory = FindFirstObjectByType<SimpleInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("⚠️ SimpleInventory not found! Materials lost.");
            return;
        }
        
        Debug.Log($"🔄 === RESOURCE RECOVERY ({resourceRecoveryRate * 100:F0}%) ===");
        Debug.Log($"Recovering materials from {buildingName} ({primaryMaterial})");
        
        // Get base materials for this building type
        var materials = GetBuildingMaterials();
        
        foreach (var material in materials)
        {
            int recoveredAmount = Mathf.RoundToInt(material.Value * resourceRecoveryRate);
            if (recoveredAmount > 0)
            {
                inventory.AddItem(material.Key, recoveredAmount);
                Debug.Log($"✅ Recovered {recoveredAmount}x {material.Key} to inventory");
            }
        }
        
        // Chance for bonus materials based on material type
        CheckBonusMaterials(inventory);
    }
    
    System.Collections.Generic.Dictionary<string, int> GetBuildingMaterials()
    {
        var materials = new System.Collections.Generic.Dictionary<string, int>();
        
        // Base materials by building type
        switch (buildingType)
        {
            case BuildingType.Foundation:
                materials["Wood"] = 1;
                materials["Stone"] = 2;
                break;
                
            case BuildingType.Wall:
                materials["Wood"] = 2;
                materials["Stone"] = 1;
                break;
                
            case BuildingType.Workbench:
                materials["Wood"] = 4;
                materials["Stone"] = 1;
                break;
                
            case BuildingType.Door:
                materials["Wood"] = 3;
                break;
                
            case BuildingType.Window:
                materials["Wood"] = 2;
                materials["Stone"] = 1;
                break;
                
            case BuildingType.Storage:
                materials["Wood"] = 5;
                materials["Metal"] = 1;
                break;
                
            case BuildingType.Defense:
                materials["Stone"] = 3;
                materials["Metal"] = 2;
                break;
                
            default:
                materials["Wood"] = 2;
                break;
        }
        
        // Modify based on primary material
        ModifyMaterialsByType(materials);
        
        return materials;
    }
    
    void ModifyMaterialsByType(System.Collections.Generic.Dictionary<string, int> materials)
    {
        switch (primaryMaterial)
        {
            case BuildingMaterial.Stone:
                // Convert some wood to stone
                if (materials.ContainsKey("Wood"))
                {
                    int woodToConvert = materials["Wood"] / 2;
                    materials["Wood"] -= woodToConvert;
                    if (materials.ContainsKey("Stone"))
                        materials["Stone"] += woodToConvert;
                    else
                        materials["Stone"] = woodToConvert;
                }
                break;
                
            case BuildingMaterial.Metal:
                // Add metal materials
                if (materials.ContainsKey("Metal"))
                    materials["Metal"] += 1;
                else
                    materials["Metal"] = 1;
                break;
                
            case BuildingMaterial.Composite:
                // Add advanced materials
                if (materials.ContainsKey("Metal"))
                    materials["Metal"] += 1;
                else
                    materials["Metal"] = 1;
                if (materials.ContainsKey("Crystal"))
                    materials["Crystal"] += 1;
                else
                    materials["Crystal"] = 1;
                break;
                
            case BuildingMaterial.Crystal:
                // Add rare materials
                if (materials.ContainsKey("Crystal"))
                    materials["Crystal"] += 2;
                else
                    materials["Crystal"] = 2;
                break;
        }
    }
    
    void CheckBonusMaterials(SimpleInventory inventory)
    {
        // 10% chance for bonus material based on building material
        if (Random.Range(0f, 1f) < 0.1f)
        {
            string bonusMaterial = GetBonusMaterial();
            if (!string.IsNullOrEmpty(bonusMaterial))
            {
                inventory.AddItem(bonusMaterial, 1);
                Debug.Log($"✨ BONUS! Found 1x {bonusMaterial} while recovering materials!");
            }
        }
    }
    
    string GetBonusMaterial()
    {
        switch (primaryMaterial)
        {
            case BuildingMaterial.Wood: return "Stone";
            case BuildingMaterial.Stone: return "Metal";
            case BuildingMaterial.Metal: return "Crystal";
            case BuildingMaterial.Crystal: return "Gem";
            default: return "Metal";
        }
    }
    
    #endregion
    
    #region Tool Interaction
    
    public float GetToolEffectiveness(string toolName)
    {
        if (string.IsNullOrEmpty(toolName)) return 1f;
        
        string lowerTool = toolName.ToLower();
        
        // Tool effectiveness vs material type
        switch (primaryMaterial)
        {
            case BuildingMaterial.Wood:
                if (lowerTool.Contains("axe")) return 1.5f;
                if (lowerTool.Contains("hammer")) return 1.0f;
                if (lowerTool.Contains("pickaxe")) return 0.5f;
                break;
                
            case BuildingMaterial.Stone:
                if (lowerTool.Contains("pickaxe")) return 1.5f;
                if (lowerTool.Contains("hammer")) return 1.0f;
                if (lowerTool.Contains("axe")) return 0.3f;
                break;
                
            case BuildingMaterial.Metal:
                if (lowerTool.Contains("hammer")) return 1.2f;
                if (lowerTool.Contains("pickaxe")) return 0.8f;
                if (lowerTool.Contains("axe")) return 0.2f;
                break;
                
            case BuildingMaterial.Composite:
                // Resistant to all tools
                return 0.6f;
                
            case BuildingMaterial.Crystal:
                // Very resistant
                return 0.3f;
        }
        
        return 1f;
    }
    
    public string GetMaterialType()
    {
        return primaryMaterial.ToString();
    }
    
    public string GetRequiredRepairMaterial()
    {
        switch (primaryMaterial)
        {
            case BuildingMaterial.Wood: return "Wood";
            case BuildingMaterial.Stone: return "Stone";
            case BuildingMaterial.Metal: return "Metal";
            case BuildingMaterial.Composite: return "Metal";
            case BuildingMaterial.Crystal: return "Crystal";
            default: return "Wood";
        }
    }
    
    public int GetRepairMaterialCost(int repairAmount)
    {
        // Cost scales with repair amount and material type
        float baseCost = repairAmount / 25f; // 1 material per 25 HP
        
        switch (primaryMaterial)
        {
            case BuildingMaterial.Wood: return Mathf.Max(1, Mathf.RoundToInt(baseCost));
            case BuildingMaterial.Stone: return Mathf.Max(1, Mathf.RoundToInt(baseCost * 1.2f));
            case BuildingMaterial.Metal: return Mathf.Max(1, Mathf.RoundToInt(baseCost * 1.5f));
            case BuildingMaterial.Composite: return Mathf.Max(1, Mathf.RoundToInt(baseCost * 2f));
            case BuildingMaterial.Crystal: return Mathf.Max(1, Mathf.RoundToInt(baseCost * 3f));
            default: return 1;
        }
    }
    
    #endregion
    
    #region Health Bar System
    
    void CreateHealthBar()
    {
        if (healthBarPrefab == null) return;
        
        Vector3 healthBarPosition = transform.position + Vector3.up * 1.2f;
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
        if (healthBarFill == null)
        {
            healthBarFill = healthBarInstance.transform.GetChild(0);
        }
        
        UpdateHealthBar();
    }
    
    void UpdateHealthBar()
    {
        if (healthBarFill == null) return;
        
        float healthPercent = GetDurabilityPercentage();
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
        
        // Hide health bar if at full health
        if (healthBarInstance != null)
        {
            healthBarInstance.SetActive(healthPercent < 1f);
        }
    }
    
    #endregion
    
    #region Visual Effects
    
    IEnumerator DamageFlashEffect()
    {
        if (spriteRenderer == null) yield break;
        
        Color flashColor = Color.red;
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
    
    IEnumerator RepairGlowEffect()
    {
        if (spriteRenderer == null) yield break;
        
        Color glowColor = Color.green;
        spriteRenderer.color = glowColor;
        yield return new WaitForSeconds(0.2f);
        spriteRenderer.color = originalColor;
    }
    
    IEnumerator DestructionEffect()
    {
        if (spriteRenderer == null) yield break;
        
        // Flash between red and original color
        for (int i = 0; i < 5; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    public void ShowDamageEffect()
    {
        if (showDamageEffects)
        {
            StartCoroutine(DamageFlashEffect());
        }
    }
    
    public void ShowRepairEffect()
    {
        if (showDamageEffects)
        {
            StartCoroutine(RepairGlowEffect());
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    public bool IsFoundation() => buildingType == BuildingType.Foundation;
    public bool IsWall() => buildingType == BuildingType.Wall;
    public bool IsWorkstation() => buildingType == BuildingType.Workbench;
    public bool IsDefensive() => buildingType == BuildingType.Defense;
    public bool IsStorage() => buildingType == BuildingType.Storage;
    public bool IsUtility() => buildingType == BuildingType.Utility;
    
    public float GetDurabilityPercentage()
    {
        return maxDurability > 0 ? (float)currentDurability / maxDurability : 0f;
    }
    
    public bool IsDestroyed() => isDestroyed || currentDurability <= 0;
    public bool IsDamaged() => currentDurability < maxDurability;
    public bool IsHealthy() => currentDurability == maxDurability;
    public bool IsCritical() => currentDurability < maxDurability * 0.25f;
    public bool IsHeavilyDamaged() => currentDurability < maxDurability * 0.5f;
    
    public string GetBuildingStatus()
    {
        if (IsDestroyed()) return "Destroyed";
        if (IsCritical()) return "Critical";
        if (IsHeavilyDamaged()) return "Heavily Damaged";
        if (IsDamaged()) return "Damaged";
        return "Healthy";
    }
    
    public string GetBuildingInfo()
    {
        string status = GetBuildingStatus();
        
        return $"{buildingName}\n" +
               $"Type: {buildingType}\n" +
               $"Material: {primaryMaterial}\n" +
               $"Health: {currentDurability}/{maxDurability}\n" +
               $"Status: {status}\n" +
               $"Durability: {GetDurabilityPercentage():P0}";
    }
    
    #endregion
    
    #region Building Rules
    
    public bool CanPlaceOnTop(BuildingType newBuildingType)
    {
        switch (buildingType)
        {
            case BuildingType.Foundation:
                return newBuildingType == BuildingType.Wall ||
                       newBuildingType == BuildingType.Workbench ||
                       newBuildingType == BuildingType.Door ||
                       newBuildingType == BuildingType.Window ||
                       newBuildingType == BuildingType.Storage;
                       
            case BuildingType.Wall:
                return newBuildingType == BuildingType.Window ||
                       newBuildingType == BuildingType.Door ||
                       newBuildingType == BuildingType.Defense;
                       
            default:
                return false;
        }
    }
    
    public bool CanSupportWeight(BuildingType newBuildingType)
    {
        if (IsCritical()) return false;
        
        switch (primaryMaterial)
        {
            case BuildingMaterial.Wood: return true;
            case BuildingMaterial.Stone: return true;
            case BuildingMaterial.Metal: return true;
            case BuildingMaterial.Composite: return true;
            case BuildingMaterial.Crystal: return true;
            default: return true;
        }
    }
    
    #endregion
    
    #region Audio System
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
    
    #endregion
    
    #region Testing Methods
    
    [ContextMenu("Test Damage (25)")]
    public void TestDamage()
    {
        TakeDamage(25);
    }
    
    [ContextMenu("Test Repair (50)")]
    public void TestRepair()
    {
        RepairBuilding(50);
    }
    
    [ContextMenu("Test Destroy")]
    public void TestDestroy()
    {
        TakeDamage(currentDurability);
    }
    
    [ContextMenu("Test Repair Full")]
    public void TestRepairFull()
    {
        RepairToFull();
    }
    
    [ContextMenu("Show Building Info")]
    public void ShowInfo()
    {
        Debug.Log($"📋 === BUILDING INFO ===\n{GetBuildingInfo()}");
    }
    
    [ContextMenu("Show Tool Effectiveness")]
    public void ShowToolEffectiveness()
    {
        Debug.Log($"🔧 === TOOL EFFECTIVENESS ===");
        Debug.Log($"Material: {primaryMaterial}");
        Debug.Log($"Hammer: {GetToolEffectiveness("Hammer")}x");
        Debug.Log($"Axe: {GetToolEffectiveness("Axe")}x");
        Debug.Log($"Pickaxe: {GetToolEffectiveness("Pickaxe")}x");
    }
    
    #endregion
    
    #region Debug Visualization
    
    void OnDrawGizmosSelected()
    {
        // Building health color
        if (IsDestroyed())
            Gizmos.color = Color.black;
        else if (IsCritical())
            Gizmos.color = Color.red;
        else if (IsHeavilyDamaged())
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange color
        else if (IsDamaged())
            Gizmos.color = Color.yellow;
        else
            Gizmos.color = Color.green;
            
        // Building outline
        Gizmos.DrawWireCube(transform.position, Vector3.one);
        
        // Material type indicator
        Vector3 materialPos = transform.position + Vector3.up * 0.7f;
        switch (primaryMaterial)
        {
            case BuildingMaterial.Wood:
                Gizmos.color = new Color(0.6f, 0.4f, 0.2f);
                break;
            case BuildingMaterial.Stone:
                Gizmos.color = Color.gray;
                break;
            case BuildingMaterial.Metal:
                Gizmos.color = new Color(0.7f, 0.7f, 0.8f);
                break;
            case BuildingMaterial.Composite:
                Gizmos.color = Color.cyan;
                break;
            case BuildingMaterial.Crystal:
                Gizmos.color = Color.magenta;
                break;
        }
        Gizmos.DrawWireSphere(materialPos, 0.2f);
        
        // Health bar visualization
        if (!IsDestroyed())
        {
            Vector3 healthBarPos = transform.position + Vector3.up * 1.5f;
            float healthPercent = GetDurabilityPercentage();
            
            // Background bar
            Gizmos.color = Color.red;
            Gizmos.DrawLine(healthBarPos - Vector3.right * 0.5f, healthBarPos + Vector3.right * 0.5f);
            
            // Health bar
            Gizmos.color = Color.green;
            Vector3 healthEnd = healthBarPos - Vector3.right * 0.5f + Vector3.right * healthPercent;
            Gizmos.DrawLine(healthBarPos - Vector3.right * 0.5f, healthEnd);
        }
    }
    
    #endregion
}

// Extension Methods for BuildingManager
public static class BuildingManagerExtensions
{
    public static void ClearGridPosition(this BuildingManager buildingManager, Vector3 position)
    {
        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(position.x), 
            Mathf.RoundToInt(position.y)
        );
        
        // Clear the grid position
        Debug.Log($"🗑️ Clearing grid position: {gridPos}");
        
        // Note: This assumes BuildingManager has a method to clear positions
        // You may need to modify BuildingManager to add this functionality
    }
}