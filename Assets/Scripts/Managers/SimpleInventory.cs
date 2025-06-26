using UnityEngine;
using System.Collections.Generic;

public class SimpleInventory : MonoBehaviour
{
    [Header("Inventory System")]
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    
    [Header("Debug Display")]
    public bool showDebugInfo = true;
    
    void Start()
    {
        // Initialize with some basic materials for testing
        AddItem("Wood", 10);
        AddItem("Stone", 8);
        AddItem("Metal", 5);
        
        Debug.Log("Simple Inventory initialized");
    }
    
    void Update()
    {
        // Test controls
        if (Input.GetKeyDown(KeyCode.T))
        {
            AddTestMaterials();
        }
        
        if (Input.GetKeyDown(KeyCode.I))
        {
            ShowInventory();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetInventory();
        }
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            AddRandomResources();
        }
    }
    
    // Core inventory methods
    public void AddItem(string itemName, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0)
            return;
            
        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName] += amount;
        }
        else
        {
            inventory[itemName] = amount;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Added {amount}x {itemName}. Total: {inventory[itemName]}");
        }
    }
    
    public bool RemoveItem(string itemName, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0)
            return false;
            
        if (!inventory.ContainsKey(itemName))
        {
            Debug.Log($"Item {itemName} not found in inventory");
            return false;
        }
        
        if (inventory[itemName] < amount)
        {
            Debug.Log($"Not enough {itemName}. Have: {inventory[itemName]}, Need: {amount}");
            return false;
        }
        
        inventory[itemName] -= amount;
        
        // Remove from dictionary if amount reaches 0
        if (inventory[itemName] <= 0)
        {
            inventory.Remove(itemName);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Removed {amount}x {itemName}");
        }
        
        return true;
    }
    
    public bool HasItem(string itemName, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0)
            return false;
            
        if (!inventory.ContainsKey(itemName))
            return false;
            
        return inventory[itemName] >= amount;
    }
    
    public int GetItemCount(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return 0;
            
        if (inventory.ContainsKey(itemName))
            return inventory[itemName];
            
        return 0;
    }
    
    public void ClearInventory()
    {
        inventory.Clear();
        Debug.Log("Inventory cleared");
    }
    
    // Test and utility methods
    void AddTestMaterials()
    {
        AddItem("Wood", 5);
        AddItem("Stone", 3);
        AddItem("Metal", 2);
        AddItem("Food", 1);
        
        Debug.Log("=== TEST MATERIALS ADDED ===");
        ShowInventory();
    }
    
    void ShowInventory()
    {
        Debug.Log("=== INVENTORY ===");
        
        if (inventory.Count == 0)
        {
            Debug.Log("Inventory is empty");
            return;
        }
        
        foreach (KeyValuePair<string, int> item in inventory)
        {
            Debug.Log($"• {item.Key}: {item.Value}");
        }
        
        Debug.Log($"Total items: {inventory.Count} types");
    }
    
    void ResetInventory()
    {
        ClearInventory();
        
        // Add basic starting materials
        AddItem("Wood", 10);
        AddItem("Stone", 8);
        AddItem("Metal", 5);
        
        Debug.Log("Inventory reset to starting materials");
    }
    
    void AddRandomResources()
    {
        string[] materials = { "Wood", "Stone", "Metal", "Food", "Water", "Cloth", "Iron", "Coal" };
        
        for (int i = 0; i < 3; i++)
        {
            string randomMaterial = materials[Random.Range(0, materials.Length)];
            int randomAmount = Random.Range(1, 6);
            AddItem(randomMaterial, randomAmount);
        }
        
        Debug.Log("Random resources added!");
    }
    
    // Methods for compatibility with existing systems
    public bool ConsumeItems(CraftingIngredient[] ingredients)
    {
        if (ingredients == null)
            return true;
            
        // Check if we have all required items first
        foreach (CraftingIngredient ingredient in ingredients)
        {
            if (ingredient != null && ingredient.IsValid())
            {
                if (!HasItem(ingredient.materialName, ingredient.amount))
                {
                    Debug.Log($"Missing {ingredient.materialName} x{ingredient.amount}");
                    return false;
                }
            }
        }
        
        // Consume all items
        foreach (CraftingIngredient ingredient in ingredients)
        {
            if (ingredient != null && ingredient.IsValid())
            {
                RemoveItem(ingredient.materialName, ingredient.amount);
            }
        }
        
        return true;
    }
    
    public bool CanAfford(CraftingIngredient[] ingredients)
    {
        if (ingredients == null)
            return true;
            
        foreach (CraftingIngredient ingredient in ingredients)
        {
            if (ingredient != null && ingredient.IsValid())
            {
                if (!HasItem(ingredient.materialName, ingredient.amount))
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    // Get all items as a list for UI display
    public List<KeyValuePair<string, int>> GetAllItems()
    {
        List<KeyValuePair<string, int>> items = new List<KeyValuePair<string, int>>();
        
        foreach (KeyValuePair<string, int> item in inventory)
        {
            items.Add(item);
        }
        
        return items;
    }
    
    // Check if inventory contains any items
    public bool IsEmpty()
    {
        return inventory.Count == 0;
    }
    
    // Get total number of different item types
    public int GetItemTypeCount()
    {
        return inventory.Count;
    }
    
    // Get total number of all items
    public int GetTotalItemCount()
    {
        int total = 0;
        foreach (KeyValuePair<string, int> item in inventory)
        {
            total += item.Value;
        }
        return total;
    }
    
    // Save/Load methods (basic implementation)
    public string GetInventoryData()
    {
        // Simple serialization for save/load
        string data = "";
        foreach (KeyValuePair<string, int> item in inventory)
        {
            data += item.Key + ":" + item.Value + ";";
        }
        return data;
    }
    
    public void LoadInventoryData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return;
            
        ClearInventory();
        
        string[] items = data.Split(';');
        foreach (string itemData in items)
        {
            if (string.IsNullOrEmpty(itemData))
                continue;
                
            string[] parts = itemData.Split(':');
            if (parts.Length == 2)
            {
                string itemName = parts[0];
                if (int.TryParse(parts[1], out int amount))
                {
                    AddItem(itemName, amount);
                }
            }
        }
        
        Debug.Log("Inventory data loaded");
    }
}