using UnityEngine;

[System.Serializable]
public class CraftingIngredient
{
    public string materialName = "";
    public int amount = 1;
    
    // Default constructor
    public CraftingIngredient()
    {
        materialName = "";
        amount = 1;
    }
    
    // Constructor with parameters
    public CraftingIngredient(string name, int qty)
    {
        materialName = name;
        amount = qty;
    }
    
    // Basic validation
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(materialName) && amount > 0;
    }
}