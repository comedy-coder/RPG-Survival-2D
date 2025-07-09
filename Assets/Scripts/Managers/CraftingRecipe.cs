using UnityEngine;

[System.Serializable]
public class CraftingRecipe
{
    [Header("Recipe Information")]
    public string recipeName = "";
    public CraftingIngredient[] requiredMaterials; // ✅ ADDED: This was missing!
    public string resultItemName = "";
    public int resultAmount = 1;
    public float craftingTime = 2f;
    
    // ✅ Default constructor
    public CraftingRecipe()
    {
        recipeName = "";
        requiredMaterials = new CraftingIngredient[0]; // ✅ Initialize empty array
        resultItemName = "";
        resultAmount = 1;
        craftingTime = 2f;
    }
    
    // ✅ Constructor with parameters
    public CraftingRecipe(string name, string result, int amount)
    {
        recipeName = name;
        resultItemName = result;
        resultAmount = amount;
        craftingTime = 2f;
        requiredMaterials = new CraftingIngredient[0]; // ✅ Initialize empty array
    }
    
    // ✅ Constructor with materials
    public CraftingRecipe(string name, CraftingIngredient[] materials, string result, int amount, float time = 2f)
    {
        recipeName = name;
        requiredMaterials = materials ?? new CraftingIngredient[0]; // ✅ Null safety
        resultItemName = result;
        resultAmount = amount;
        craftingTime = time;
    }
    
    // ✅ Basic validation
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(recipeName) && 
               !string.IsNullOrEmpty(resultItemName) && 
               resultAmount > 0 &&
               craftingTime > 0;
    }
    
    // ✅ Get materials string for UI
    public string GetMaterialsString()
    {
        if (requiredMaterials == null || requiredMaterials.Length == 0)
            return "No materials required";
        
        string result = "";
        for (int i = 0; i < requiredMaterials.Length; i++)
        {
            if (requiredMaterials[i] != null && requiredMaterials[i].IsValid())
            {
                result += $"{requiredMaterials[i].materialName} x{requiredMaterials[i].amount}";
                if (i < requiredMaterials.Length - 1) result += ", ";
            }
        }
        
        return string.IsNullOrEmpty(result) ? "No valid materials" : result;
    }
    
    // ✅ Get total material cost
    public int GetTotalMaterialCost()
    {
        int total = 0;
        if (requiredMaterials != null)
        {
            foreach (var material in requiredMaterials)
            {
                if (material != null && material.IsValid())
                {
                    total += material.amount;
                }
            }
        }
        return total;
    }
    
    // ✅ Check if player has required materials
    public bool HasRequiredMaterials(SimpleInventory inventory)
    {
        if (inventory == null || requiredMaterials == null) return false;
        
        foreach (var material in requiredMaterials)
        {
            if (material != null && material.IsValid())
            {
                if (!inventory.HasItem(material.materialName, material.amount))
                {
                    return false;
                }
            }
        }
        return true;
    }
    
    // ✅ Debug info
    public string GetDebugInfo()
    {
        string info = $"Recipe: {recipeName} → {resultAmount}x {resultItemName} (Time: {craftingTime}s)";
        if (requiredMaterials != null && requiredMaterials.Length > 0)
        {
            info += "\nMaterials: ";
            foreach (var mat in requiredMaterials)
            {
                if (mat != null && mat.IsValid())
                {
                    info += $"{mat.amount}x {mat.materialName}, ";
                }
            }
            info = info.TrimEnd(',', ' ');
        }
        return info;
    }
}