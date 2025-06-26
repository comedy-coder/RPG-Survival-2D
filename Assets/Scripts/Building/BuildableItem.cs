using UnityEngine;

[CreateAssetMenu(fileName = "New Building", menuName = "Building/BuildableItem")]
public class BuildableItem : ScriptableObject
{
    [Header("Building Information")]
    public string buildingName = "New Building";
    public string description = "A new building";
    public Sprite buildingIcon;
    
    [Header("Construction")]
    public GameObject buildingPrefab;
    public CraftingIngredient[] requiredMaterials;
    public float constructionTime = 2f;
    
    [Header("Building Properties")]
    public int maxDurability = 100;
    public bool canBeDestroyed = true;
    public bool canBeUpgraded = false;
    
    [Header("Building Category")]
    public BuildingType buildingType = BuildingType.Structure;
    
    public enum BuildingType
    {
        Structure,    // Foundation, Wall
        Functional,   // Workbench, Door
        Decorative,   // Torch, Chair
        Defense,      // Tower, Fence
        Utility       // Storage, Crafting
    }
    
    // Validation method
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(buildingName))
            return false;
            
        if (buildingPrefab == null)
            return false;
            
        if (constructionTime <= 0)
            return false;
            
        return true;
    }
    
    // Check if player can build this item
    public bool CanBuild(SimpleInventory inventory)
    {
        if (!IsValid())
            return false;
            
        if (inventory == null)
            return false;
            
        if (requiredMaterials == null || requiredMaterials.Length == 0)
            return true; // No materials required
            
        foreach (CraftingIngredient ingredient in requiredMaterials)
        {
            if (ingredient != null && ingredient.IsValid())
            {
                if (!inventory.HasItem(ingredient.materialName, ingredient.amount))
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    // Get materials cost as formatted string
    public string GetCostString()
    {
        if (requiredMaterials == null || requiredMaterials.Length == 0)
            return "No materials required";
            
        string cost = "";
        bool first = true;
        
        foreach (CraftingIngredient ingredient in requiredMaterials)
        {
            if (ingredient != null && ingredient.IsValid())
            {
                if (!first)
                    cost += ", ";
                    
                cost += $"{ingredient.materialName} x{ingredient.amount}";
                first = false;
            }
        }
        
        return string.IsNullOrEmpty(cost) ? "No materials required" : cost;
    }
    
    // Get missing materials for this building
    public string[] GetMissingMaterials(SimpleInventory inventory)
    {
        if (inventory == null || requiredMaterials == null)
            return new string[0];
            
        System.Collections.Generic.List<string> missing = new System.Collections.Generic.List<string>();
        
        foreach (CraftingIngredient ingredient in requiredMaterials)
        {
            if (ingredient != null && ingredient.IsValid())
            {
                if (!inventory.HasItem(ingredient.materialName, ingredient.amount))
                {
                    int have = inventory.GetItemCount(ingredient.materialName);
                    int need = ingredient.amount - have;
                    missing.Add($"{ingredient.materialName} x{need}");
                }
            }
        }
        
        return missing.ToArray();
    }
    
    // Calculate total material cost (for sorting/comparison)
    public int GetTotalCost()
    {
        if (requiredMaterials == null)
            return 0;
            
        int total = 0;
        foreach (CraftingIngredient ingredient in requiredMaterials)
        {
            if (ingredient != null && ingredient.IsValid())
            {
                total += ingredient.amount;
            }
        }
        
        return total;
    }
    
    // Get building tier based on materials
    public int GetBuildingTier()
    {
        if (requiredMaterials == null || requiredMaterials.Length == 0)
            return 0;
            
        // Simple tier calculation based on materials
        int tier = 0;
        
        foreach (CraftingIngredient ingredient in requiredMaterials)
        {
            if (ingredient != null && ingredient.IsValid())
            {
                string material = ingredient.materialName.ToLower();
                if (material.Contains("wood"))
                    tier = Mathf.Max(tier, 1);
                else if (material.Contains("stone"))
                    tier = Mathf.Max(tier, 2);
                else if (material.Contains("metal") || material.Contains("iron"))
                    tier = Mathf.Max(tier, 3);
                else if (material.Contains("steel") || material.Contains("advanced"))
                    tier = Mathf.Max(tier, 4);
            }
        }
        
        return tier;
    }
    
    // Create a copy of this building item
    public BuildableItem Clone()
    {
        BuildableItem clone = CreateInstance<BuildableItem>();
        
        clone.buildingName = buildingName;
        clone.description = description;
        clone.buildingIcon = buildingIcon;
        clone.buildingPrefab = buildingPrefab;
        clone.constructionTime = constructionTime;
        clone.maxDurability = maxDurability;
        clone.canBeDestroyed = canBeDestroyed;
        clone.canBeUpgraded = canBeUpgraded;
        clone.buildingType = buildingType;
        
        if (requiredMaterials != null)
        {
            clone.requiredMaterials = new CraftingIngredient[requiredMaterials.Length];
            for (int i = 0; i < requiredMaterials.Length; i++)
            {
                if (requiredMaterials[i] != null)
                {
                    clone.requiredMaterials[i] = new CraftingIngredient(
                        requiredMaterials[i].materialName,
                        requiredMaterials[i].amount
                    );
                }
            }
        }
        
        return clone;
    }
    
    // Display method for debugging
    public override string ToString()
    {
        return $"{buildingName} ({buildingType}) - {GetCostString()}";
    }
    
    // Validation method for editor
    void OnValidate()
    {
        // Ensure construction time is positive
        if (constructionTime <= 0)
            constructionTime = 1f;
            
        // Ensure max durability is positive
        if (maxDurability <= 0)
            maxDurability = 100;
            
        // Clean up building name
        if (!string.IsNullOrEmpty(buildingName))
        {
            buildingName = buildingName.Trim();
        }
        
        // Validate required materials
        if (requiredMaterials != null)
        {
            foreach (CraftingIngredient ingredient in requiredMaterials)
            {
                if (ingredient != null)
                {
                    if (string.IsNullOrEmpty(ingredient.materialName))
                    {
                        Debug.LogWarning($"Empty material name in {buildingName}");
                    }
                    
                    if (ingredient.amount <= 0)
                    {
                        ingredient.amount = 1;
                        Debug.LogWarning($"Fixed invalid amount in {buildingName}");
                    }
                }
            }
        }
    }
}