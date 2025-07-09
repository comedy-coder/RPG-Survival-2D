using UnityEngine;

[CreateAssetMenu(fileName = "Building_", menuName = "RPG Survival/Building Item", order = 2)]
public class BuildableItem : ScriptableObject
{
    [Header("🏗️ Building Information")]
    [SerializeField] public string buildingName = "New Building";
    [TextArea(2, 4)]
    [SerializeField] public string description = "A building structure";
    [SerializeField] public GameObject buildingPrefab;
    [SerializeField] public Sprite buildingIcon; // For UI display
    
    [Header("📈 Building Statistics")]
    [SerializeField] [Range(50, 1000)] public int maxDurability = 100;
    [SerializeField] [Range(0.5f, 10f)] public float constructionTime = 2f;
    [SerializeField] [Range(1, 10)] public int buildingTier = 1;
    [SerializeField] public bool requiresFoundation = false;
    
    [Header("🎒 Required Materials")]
    [SerializeField] public CraftingIngredient[] requiredMaterials;
    
    [Header("📂 Building Category")]
    [SerializeField] public BuildingCategory category = BuildingCategory.Structure;
    [SerializeField] public BuildingType buildingType = BuildingType.Foundation;
    
    [Header("🎯 Building Properties")]
    [SerializeField] public bool blocksMovement = true;
    [SerializeField] public bool blocksBuilding = true;
    [SerializeField] public bool canBeDestroyed = true;
    [SerializeField] public bool providesDefense = false;
    [SerializeField] [Range(0, 100)] public int defenseValue = 0;
    
    [Header("💰 Economic Values")]
    [SerializeField] [Range(1, 1000)] public int buildCost = 50;
    [SerializeField] [Range(0.1f, 1f)] public float resourceRecoveryRate = 0.5f;
    [SerializeField] public bool canBeUpgraded = false;
    [SerializeField] public BuildableItem upgradeTo; // Next tier building
    
    // Enhanced Building Categories
    public enum BuildingCategory
    {
        Structure,      // Basic structures (Foundation, Wall)
        Workstation,    // Crafting stations (Workbench, Forge)
        Storage,        // Storage containers (Chest, Warehouse)
        Defense,        // Defensive structures (Turret, Barricade)
        Decoration,     // Decorative items (Window, Door)
        Production,     // Resource production (Farm, Mine)
        Utility,        // Utility buildings (Well, Generator)
        Housing         // Living spaces (House, Shelter)
    }
    
    // Enhanced Building Types
    public enum BuildingType
    {
        Foundation,     // Base structure
        Wall,          // Blocking wall
        Door,          // Openable entrance
        Window,        // Decorative opening
        Workbench,     // Basic crafting
        Forge,         // Advanced crafting
        Chest,         // Storage container
        Turret,        // Defense structure
        Farm,          // Food production
        Well,          // Water source
        House,         // Player shelter
        Bridge         // Path connector
    }
    
    // ✅ ENHANCED VALIDATION
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(buildingName)) return false;
        if (buildingPrefab == null) return false;
        if (maxDurability <= 0) return false;
        if (constructionTime <= 0) return false;
        
        // Validate materials
        if (requiredMaterials != null)
        {
            foreach (var material in requiredMaterials)
            {
                if (material != null && !material.IsValid())
                    return false;
            }
        }
        
        return true;
    }
    
    // ✅ ENHANCED MATERIALS STRING
    public string GetMaterialsString()
    {
        if (requiredMaterials == null || requiredMaterials.Length == 0)
            return "No materials required";
        
        string result = "";
        int validMaterials = 0;
        
        for (int i = 0; i < requiredMaterials.Length; i++)
        {
            if (requiredMaterials[i] != null && requiredMaterials[i].IsValid())
            {
                if (validMaterials > 0) result += ", ";
                result += $"{requiredMaterials[i].materialName} x{requiredMaterials[i].amount}";
                validMaterials++;
            }
        }
        
        return validMaterials > 0 ? result : "No valid materials";
    }
    
    // ✅ GET TOTAL MATERIAL COST
    public int GetTotalMaterialCost()
    {
        int totalCost = 0;
        if (requiredMaterials != null)
        {
            foreach (var material in requiredMaterials)
            {
                if (material != null && material.IsValid())
                {
                    totalCost += material.amount;
                }
            }
        }
        return totalCost;
    }
    
    // ✅ BUILDING TYPE HELPERS
    public bool IsFoundation()
    {
        return buildingType == BuildingType.Foundation || 
               buildingName.ToLower().Contains("foundation");
    }
    
    public bool IsWall()
    {
        return buildingType == BuildingType.Wall || 
               buildingName.ToLower().Contains("wall");
    }
    
    public bool IsWorkbench()
    {
        return buildingType == BuildingType.Workbench || 
               buildingName.ToLower().Contains("workbench");
    }
    
    public bool IsDoor()
    {
        return buildingType == BuildingType.Door || 
               buildingName.ToLower().Contains("door");
    }
    
    public bool IsWindow()
    {
        return buildingType == BuildingType.Window || 
               buildingName.ToLower().Contains("window");
    }
    
    public bool IsDefensive()
    {
        return category == BuildingCategory.Defense || providesDefense;
    }
    
    public bool IsStorage()
    {
        return category == BuildingCategory.Storage || 
               buildingType == BuildingType.Chest;
    }
    
    public bool IsProduction()
    {
        return category == BuildingCategory.Production;
    }
    
    // ✅ BUILDING PLACEMENT RULES
    public bool CanPlaceOnFoundation()
    {
        return IsWall() || IsWorkbench() || IsDoor() || IsWindow() || IsStorage();
    }
    
    public bool CanPlaceOnGround()
    {
        return IsFoundation() || category == BuildingCategory.Production || 
               category == BuildingCategory.Utility;
    }
    
    public bool CanPlaceOnWall()
    {
        return IsWindow() || IsDoor();
    }
    
    public bool RequiresFoundation()
    {
        return requiresFoundation || IsWall() || IsWorkbench();
    }
    
    // ✅ BUILDING TIER SYSTEM
    public string GetTierName()
    {
        switch (buildingTier)
        {
            case 1: return "Basic";
            case 2: return "Improved";
            case 3: return "Advanced";
            case 4: return "Master";
            case 5: return "Legendary";
            default: return "Unknown";
        }
    }
    
    public Color GetTierColor()
    {
        switch (buildingTier)
        {
            case 1: return Color.white;        // Basic
            case 2: return Color.green;        // Improved
            case 3: return Color.blue;         // Advanced
            case 4: return Color.magenta;      // Master
            case 5: return Color.yellow;       // Legendary
            default: return Color.gray;
        }
    }
    
    // ✅ UPGRADE SYSTEM
    public bool CanBeUpgraded()
    {
        return canBeUpgraded && upgradeTo != null;
    }
    
    public BuildableItem GetUpgradeTarget()
    {
        return canBeUpgraded ? upgradeTo : null;
    }
    
    public CraftingIngredient[] GetUpgradeCost()
    {
        if (CanBeUpgraded())
        {
            return upgradeTo.requiredMaterials;
        }
        return null;
    }
    
    // ✅ RESOURCE RECOVERY CALCULATION
    public CraftingIngredient[] GetRecoveredMaterials()
    {
        if (requiredMaterials == null) return null;
        
        CraftingIngredient[] recovered = new CraftingIngredient[requiredMaterials.Length];
        for (int i = 0; i < requiredMaterials.Length; i++)
        {
            if (requiredMaterials[i] != null && requiredMaterials[i].IsValid())
            {
                recovered[i] = new CraftingIngredient();
                recovered[i].materialName = requiredMaterials[i].materialName;
                recovered[i].amount = Mathf.RoundToInt(requiredMaterials[i].amount * resourceRecoveryRate);
            }
        }
        return recovered;
    }
    
    // ✅ COMPREHENSIVE BUILDING INFO
    public string GetDetailedBuildingInfo()
    {
        string info = $"<b>{buildingName}</b> ({GetTierName()})\n";
        info += $"<i>{description}</i>\n\n";
        
        info += $"<b>Statistics:</b>\n";
        info += $"• Durability: {maxDurability} HP\n";
        info += $"• Construction Time: {constructionTime}s\n";
        info += $"• Category: {category}\n";
        info += $"• Type: {buildingType}\n\n";
        
        info += $"<b>Required Materials:</b>\n";
        info += $"• {GetMaterialsString()}\n\n";
        
        info += $"<b>Properties:</b>\n";
        if (blocksMovement) info += "• Blocks Movement\n";
        if (blocksBuilding) info += "• Blocks Building\n";
        if (providesDefense) info += $"• Defense: +{defenseValue}\n";
        if (requiresFoundation) info += "• Requires Foundation\n";
        if (canBeUpgraded) info += "• Can Be Upgraded\n";
        
        return info;
    }
    
    // ✅ SIMPLE BUILDING INFO
    public string GetSimpleBuildingInfo()
    {
        return $"{buildingName}\n" +
               $"HP: {maxDurability} | Time: {constructionTime}s\n" +
               $"Materials: {GetMaterialsString()}";
    }
    
    // ✅ BUILDING COMPARISON
    public bool IsBetterThan(BuildableItem otherBuilding)
    {
        if (otherBuilding == null) return true;
        
        // Compare based on tier and durability
        if (buildingTier > otherBuilding.buildingTier) return true;
        if (buildingTier == otherBuilding.buildingTier && maxDurability > otherBuilding.maxDurability) return true;
        
        return false;
    }
    
    // ✅ VALIDATION IN EDITOR
    void OnValidate()
    {
        // Auto-generate building name from asset name
        if (string.IsNullOrEmpty(buildingName))
        {
            buildingName = name.Replace("Building_", "").Replace("_", " ");
        }
        
        // Clamp values to reasonable ranges
        maxDurability = Mathf.Clamp(maxDurability, 50, 1000);
        constructionTime = Mathf.Clamp(constructionTime, 0.5f, 10f);
        buildingTier = Mathf.Clamp(buildingTier, 1, 5);
        defenseValue = Mathf.Clamp(defenseValue, 0, 100);
        resourceRecoveryRate = Mathf.Clamp01(resourceRecoveryRate);
        
        // Auto-set properties based on building type
        switch (buildingType)
        {
            case BuildingType.Foundation:
                category = BuildingCategory.Structure;
                blocksMovement = false;
                blocksBuilding = false;
                break;
                
            case BuildingType.Wall:
                category = BuildingCategory.Structure;
                blocksMovement = true;
                blocksBuilding = true;
                requiresFoundation = true;
                break;
                
            case BuildingType.Door:
                category = BuildingCategory.Decoration;
                blocksMovement = false; // Can be opened
                blocksBuilding = true;
                requiresFoundation = true;
                break;
                
            case BuildingType.Workbench:
                category = BuildingCategory.Workstation;
                blocksMovement = true;
                blocksBuilding = true;
                requiresFoundation = true;
                break;
                
            case BuildingType.Turret:
                category = BuildingCategory.Defense;
                providesDefense = true;
                if (defenseValue == 0) defenseValue = 25;
                break;
        }
    }
    
    // ✅ CONTEXT MENU ACTIONS
    [ContextMenu("Apply Foundation Settings")]
    void SetupAsFoundation()
    {
        buildingType = BuildingType.Foundation;
        category = BuildingCategory.Structure;
        maxDurability = 150;
        constructionTime = 3f;
        blocksMovement = false;
        blocksBuilding = false;
        requiresFoundation = false;
        description = "A solid foundation for building structures.";
    }
    
    [ContextMenu("Apply Wall Settings")]
    void SetupAsWall()
    {
        buildingType = BuildingType.Wall;
        category = BuildingCategory.Structure;
        maxDurability = 100;
        constructionTime = 2f;
        blocksMovement = true;
        blocksBuilding = true;
        requiresFoundation = true;
        description = "A sturdy wall for protection and structure.";
    }
    
    [ContextMenu("Apply Workbench Settings")]
    void SetupAsWorkbench()
    {
        buildingType = BuildingType.Workbench;
        category = BuildingCategory.Workstation;
        maxDurability = 80;
        constructionTime = 4f;
        blocksMovement = true;
        blocksBuilding = true;
        requiresFoundation = true;
        description = "A crafting station for creating tools and items.";
    }
    
    [ContextMenu("Show Building Info")]
    void ShowBuildingInfo()
    {
        Debug.Log($"=== {buildingName} INFO ===\n{GetDetailedBuildingInfo()}");
    }
}