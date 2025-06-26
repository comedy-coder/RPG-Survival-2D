using UnityEngine;

[System.Serializable]
public class CraftingIngredient
{
    [Header("Material Information")]
    public string materialName = "";
    public int amount = 1;
    
    [Header("Optional Settings")]
    [Tooltip("Is this ingredient optional for the recipe?")]
    public bool isOptional = false;
    
    [Tooltip("Alternative materials that can be used instead")]
    public string[] alternativeMaterials;
    
    // Default constructor
    public CraftingIngredient()
    {
        materialName = "";
        amount = 1;
        isOptional = false;
        alternativeMaterials = new string[0];
    }
    
    // Basic constructor
    public CraftingIngredient(string name, int count)
    {
        materialName = name;
        amount = count;
        isOptional = false;
        alternativeMaterials = new string[0];
    }
    
    // Advanced constructor with optional flag
    public CraftingIngredient(string name, int count, bool optional)
    {
        materialName = name;
        amount = count;
        isOptional = optional;
        alternativeMaterials = new string[0];
    }
    
    // Full constructor with alternatives
    public CraftingIngredient(string name, int count, bool optional, string[] alternatives)
    {
        materialName = name;
        amount = count;
        isOptional = optional;
        alternativeMaterials = alternatives ?? new string[0];
    }
    
    // Validation method
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(materialName) && amount > 0;
    }
    
    // Check if ingredient is satisfied by available materials
    public bool IsSatisfiedBy(string availableMaterial, int availableAmount)
    {
        // Check main material
        if (materialName == availableMaterial && availableAmount >= amount)
        {
            return true;
        }
        
        // Check alternative materials
        if (alternativeMaterials != null)
        {
            foreach (string alternative in alternativeMaterials)
            {
                if (alternative == availableMaterial && availableAmount >= amount)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    // Get the material name to use (main or alternative)
    public string GetUsableMaterial(string[] availableMaterials)
    {
        // Check if main material is available
        if (availableMaterials != null)
        {
            foreach (string available in availableMaterials)
            {
                if (available == materialName)
                {
                    return materialName;
                }
            }
            
            // Check alternatives
            if (alternativeMaterials != null)
            {
                foreach (string alternative in alternativeMaterials)
                {
                    foreach (string available in availableMaterials)
                    {
                        if (available == alternative)
                        {
                            return alternative;
                        }
                    }
                }
            }
        }
        
        return materialName; // Return main material as fallback
    }
    
    // Check if this ingredient has alternatives
    public bool HasAlternatives()
    {
        return alternativeMaterials != null && alternativeMaterials.Length > 0;
    }
    
    // Get all possible material names (main + alternatives)
    public string[] GetAllPossibleMaterials()
    {
        if (!HasAlternatives())
        {
            return new string[] { materialName };
        }
        
        string[] allMaterials = new string[1 + alternativeMaterials.Length];
        allMaterials[0] = materialName;
        
        for (int i = 0; i < alternativeMaterials.Length; i++)
        {
            allMaterials[i + 1] = alternativeMaterials[i];
        }
        
        return allMaterials;
    }
    
    // Enhanced ToString with alternatives info
    public override string ToString()
    {
        string result = $"{materialName} x{amount}";
        
        if (isOptional)
        {
            result += " (Optional)";
        }
        
        if (HasAlternatives())
        {
            result += " [Alternatives: " + string.Join(", ", alternativeMaterials) + "]";
        }
        
        return result;
    }
    
    // Simple display for UI
    public string ToSimpleString()
    {
        return $"{materialName} x{amount}";
    }
    
    // Debug information
    public string GetDebugInfo()
    {
        string info = $"Material: {materialName}, Amount: {amount}";
        info += $", Optional: {isOptional}";
        info += $", Valid: {IsValid()}";
        
        if (HasAlternatives())
        {
            info += $", Alternatives: [{string.Join(", ", alternativeMaterials)}]";
        }
        
        return info;
    }
    
    // Create a copy of this ingredient
    public CraftingIngredient Clone()
    {
        return new CraftingIngredient(
            materialName, 
            amount, 
            isOptional, 
            alternativeMaterials != null ? (string[])alternativeMaterials.Clone() : null
        );
    }
    
    // Static method to create common ingredients quickly
    public static CraftingIngredient Wood(int amount = 1)
    {
        return new CraftingIngredient("Wood", amount);
    }
    
    public static CraftingIngredient Stone(int amount = 1)
    {
        return new CraftingIngredient("Stone", amount);
    }
    
    public static CraftingIngredient Metal(int amount = 1)
    {
        return new CraftingIngredient("Metal", amount);
    }
    
    public static CraftingIngredient Food(int amount = 1)
    {
        return new CraftingIngredient("Food", amount);
    }
}