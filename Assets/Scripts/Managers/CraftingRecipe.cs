using UnityEngine;

[System.Serializable]
public class CraftingRecipe
{
    [Header("Recipe Information")]
    public string recipeName = "";
    public CraftingIngredient[] requiredMaterials;
    public string resultItemName = "";
    public int resultAmount = 1;
    public float craftingTime = 2f;
    
    // Default constructor
    public CraftingRecipe()
    {
        recipeName = "";
        requiredMaterials = new CraftingIngredient[0];
        resultItemName = "";
        resultAmount = 1;
        craftingTime = 2f;
    }
    
    // Constructor with parameters
    public CraftingRecipe(string name, string result, int amount)
    {
        recipeName = name;
        resultItemName = result;
        resultAmount = amount;
        craftingTime = 2f;
        requiredMaterials = new CraftingIngredient[0];
    }
    
    // Constructor with full parameters
    public CraftingRecipe(string name, CraftingIngredient[] materials, string result, int amount, float time = 2f)
    {
        recipeName = name;
        requiredMaterials = materials ?? new CraftingIngredient[0];
        resultItemName = result;
        resultAmount = amount;
        craftingTime = time;
    }
    
    // Validation method
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(recipeName) && 
               !string.IsNullOrEmpty(resultItemName) &&
               resultAmount > 0 &&
               craftingTime > 0;
    }
    
    // Check if recipe matches criteria
    public bool CanCraft(string name, string result, int amount)
    {
        return recipeName == name && 
               resultItemName == result && 
               amount <= resultAmount;
    }
    
    // Get total material cost
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
    
    // Debug info
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