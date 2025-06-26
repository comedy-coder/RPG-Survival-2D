using UnityEngine;

[System.Serializable]
public enum BuildingType
{
    Foundation,
    Wall,
    Workbench,
    Structure,      // Để tương thích với code cũ
    Functional,     // Để tương thích với code cũ  
    Decorative,     // Để tương thích với code cũ
    Defense,        // Để tương thích với code cũ
    Utility         // Để tương thích với code cũ
}

[System.Serializable]  
public enum ToolType
{
    Hammer,     // Good for wood, decent for stone
    Axe,        // Excellent for wood, poor for stone/metal
    Pickaxe     // Excellent for stone/metal, poor for wood
}