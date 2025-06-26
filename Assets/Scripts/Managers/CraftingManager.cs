using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    [Header("Crafting System")]
    public CraftingRecipe[] availableRecipes;
    
    private SimpleInventory playerInventory;
    
    void Start()
    {
        playerInventory = GetComponent<SimpleInventory>();
        Debug.Log("Crafting Manager Started");
    }
    
    void Update()
    {
        HandleCraftingInput();
    }
    
    void HandleCraftingInput()
    {
        // Basic crafting controls
        if (Input.GetKeyDown(KeyCode.C))
        {
            ShowAvailableRecipes();
        }
        
        // Quick craft with number keys (while holding C)
        if (Input.GetKey(KeyCode.C))
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryCraft(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryCraft(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryCraft(2);
        }
    }
    
    void ShowAvailableRecipes()
    {
        Debug.Log("=== CRAFTING RECIPES ===");
        
        if (availableRecipes == null || availableRecipes.Length == 0)
        {
            Debug.Log("No recipes available");
            return;
        }
        
        for (int i = 0; i < availableRecipes.Length; i++)
        {
            CraftingRecipe recipe = availableRecipes[i];
            if (recipe != null && recipe.IsValid())
            {
                bool canCraft = CanCraft(recipe);
                string status = canCraft ? "✓" : "✗";
                Debug.Log($"{status} {i + 1}. {recipe.recipeName}");
                
                if (recipe.requiredMaterials != null)
                {
                    foreach (CraftingIngredient ingredient in recipe.requiredMaterials)
                    {
                        if (ingredient != null && ingredient.IsValid())
                        {
                            int have = GetMaterialCount(ingredient.materialName);
                            int need = ingredient.amount;
                            string matStatus = have >= need ? "✓" : "✗";
                            Debug.Log($"   {matStatus} {ingredient.materialName}: {have}/{need}");
                        }
                    }
                }
            }
        }
        
        Debug.Log("Hold C + 1-3 to craft");
    }
    
    void TryCraft(int recipeIndex)
    {
        if (recipeIndex < 0 || recipeIndex >= availableRecipes.Length)
        {
            Debug.Log("Invalid recipe index");
            return;
        }
        
        CraftingRecipe recipe = availableRecipes[recipeIndex];
        if (recipe == null || !recipe.IsValid())
        {
            Debug.Log("Invalid recipe");
            return;
        }
        
        if (!CanCraft(recipe))
        {
            Debug.Log($"Cannot craft {recipe.recipeName} - missing materials");
            ShowMissingMaterials(recipe);
            return;
        }
        
        // Consume materials
        if (ConsumeMaterials(recipe))
        {
            // Give result
            GiveResult(recipe);
            Debug.Log($"✓ Crafted {recipe.resultAmount}x {recipe.resultItemName}");
        }
        else
        {
            Debug.Log($"Failed to craft {recipe.recipeName}");
        }
    }
    
    bool CanCraft(CraftingRecipe recipe)
    {
        if (recipe == null || !recipe.IsValid())
            return false;
            
        if (playerInventory == null)
            return false;
            
        if (recipe.requiredMaterials == null)
            return true;
            
        foreach (CraftingIngredient ingredient in recipe.requiredMaterials)
        {
            if (ingredient != null && ingredient.IsValid())
            {
                if (!playerInventory.HasItem(ingredient.materialName, ingredient.amount))
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    bool ConsumeMaterials(CraftingRecipe recipe)
    {
        if (playerInventory == null || recipe.requiredMaterials == null)
            return false;
            
        foreach (CraftingIngredient ingredient in recipe.requiredMaterials)
        {
            if (ingredient != null && ingredient.IsValid())
            {
                if (!playerInventory.RemoveItem(ingredient.materialName, ingredient.amount))
                {
                    Debug.LogError($"Failed to remove {ingredient.materialName} x{ingredient.amount}");
                    return false;
                }
            }
        }
        
        return true;
    }
    
    void GiveResult(CraftingRecipe recipe)
    {
        if (playerInventory != null)
        {
            playerInventory.AddItem(recipe.resultItemName, recipe.resultAmount);
        }
    }
    
    void ShowMissingMaterials(CraftingRecipe recipe)
    {
        if (recipe.requiredMaterials == null)
            return;
            
        Debug.Log("Missing materials:");
        foreach (CraftingIngredient ingredient in recipe.requiredMaterials)
        {
            if (ingredient != null && ingredient.IsValid())
            {
                int have = GetMaterialCount(ingredient.materialName);
                int need = ingredient.amount;
                if (have < need)
                {
                    Debug.Log($"  - {ingredient.materialName}: need {need - have} more");
                }
            }
        }
    }
    
    int GetMaterialCount(string materialName)
    {
        if (playerInventory == null)
            return 0;
            
        // Assume SimpleInventory has a method to get item count
        if (playerInventory.HasItem(materialName, 1))
        {
            // Simple check - in real implementation, would get exact count
            return 999; // Placeholder - assume we have enough for basic checking
        }
        
        return 0;
    }
    
    // Public method for other systems to check if recipe can be crafted
    public bool CanCraftRecipe(string recipeName)
    {
        if (availableRecipes == null)
            return false;
            
        foreach (CraftingRecipe recipe in availableRecipes)
        {
            if (recipe != null && recipe.recipeName == recipeName)
            {
                return CanCraft(recipe);
            }
        }
        
        return false;
    }
    
    // Public method to craft by name
    public bool CraftByName(string recipeName)
    {
        if (availableRecipes == null)
            return false;
            
        for (int i = 0; i < availableRecipes.Length; i++)
        {
            CraftingRecipe recipe = availableRecipes[i];
            if (recipe != null && recipe.recipeName == recipeName)
            {
                if (CanCraft(recipe))
                {
                    if (ConsumeMaterials(recipe))
                    {
                        GiveResult(recipe);
                        return true;
                    }
                }
                return false;
            }
        }
        
        return false;
    }
}