using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BuildingDebugInfo
{
    public string buildingName;
    public Vector3 transformPosition;
    public Vector3 boundsCenter;
    public Vector2 boundsSize;
    public Vector2 spritePixelSize;
    public Vector2 spritePivot;
    public float pixelsPerUnit;
    public Vector3 localScale;
    public bool hasConsistentSettings;
    public string issues;
}

public class BuildingDebugInspector : MonoBehaviour
{
    [Header("🔍 BUILDING DEBUG INSPECTOR")]
    [Space(10)]
    
    [Header("Debug Controls")]
    public KeyCode debugKey = KeyCode.F1; // F1 để debug
    public KeyCode visualizeKey = KeyCode.F2; // F2 để toggle visualization
    public bool autoDebugOnStart = true;
    
    [Header("Debug Results")]
    public List<BuildingDebugInfo> allBuildingsInfo = new List<BuildingDebugInfo>();
    public bool hasInconsistentSettings = false;
    public string overallIssues = "";
    
    [Header("Visualization")]
    public bool showDebugGizmos = true;
    public bool showGridLines = true;
    public bool showBuildingBounds = true;
    public bool showPivotPoints = true;
    public Color gridColor = Color.cyan;
    public Color boundsColor = Color.green;
    public Color pivotColor = Color.red;
    public Color problemColor = Color.magenta;
    
    [Header("Grid Settings Reference")]
    public float gridSize = 0.5f;
    public Vector2 expectedBuildingSize = new Vector2(1f, 1f);
    
    private BuildingManager buildingManager;
    private bool debugVisualizationActive = false;
    
    void Start()
    {
        buildingManager = FindObjectOfType<BuildingManager>();
        
        if (autoDebugOnStart)
        {
            Invoke("PerformFullDebugCheck", 0.5f); // Delay để đảm bảo all objects loaded
        }
        
        Debug.Log("🔍 BUILDING DEBUG INSPECTOR READY");
        Debug.Log($"🎮 Controls: {debugKey} = Full Debug Check, {visualizeKey} = Toggle Visualization");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            PerformFullDebugCheck();
        }
        
        if (Input.GetKeyDown(visualizeKey))
        {
            ToggleDebugVisualization();
        }
    }
    
    [ContextMenu("🔍 Perform Full Debug Check")]
    public void PerformFullDebugCheck()
    {
        Debug.Log("🔍 =================== BUILDING DEBUG CHECK ===================");
        
        // Clear previous results
        allBuildingsInfo.Clear();
        hasInconsistentSettings = false;
        overallIssues = "";
        
        // Step 1: Find all buildings
        BuildingComponent[] allBuildings = FindObjectsOfType<BuildingComponent>();
        Debug.Log($"📊 Found {allBuildings.Length} buildings to analyze");
        
        if (allBuildings.Length == 0)
        {
            Debug.LogWarning("⚠️ NO BUILDINGS FOUND! Make sure buildings have BuildingComponent attached.");
            return;
        }
        
        // Step 2: Analyze each building
        List<string> globalIssues = new List<string>();
        Dictionary<float, int> pixelsPerUnitCount = new Dictionary<float, int>();
        Dictionary<string, int> pivotTypeCount = new Dictionary<string, int>();
        
        foreach (BuildingComponent building in allBuildings)
        {
            if (building == null) continue;
            
            BuildingDebugInfo info = AnalyzeBuilding(building);
            allBuildingsInfo.Add(info);
            
            // Track global patterns
            if (!pixelsPerUnitCount.ContainsKey(info.pixelsPerUnit))
                pixelsPerUnitCount[info.pixelsPerUnit] = 0;
            pixelsPerUnitCount[info.pixelsPerUnit]++;
            
            string pivotType = GetPivotTypeDescription(info.spritePivot, info.spritePixelSize);
            if (!pivotTypeCount.ContainsKey(pivotType))
                pivotTypeCount[pivotType] = 0;
            pivotTypeCount[pivotType]++;
            
            if (!info.hasConsistentSettings)
            {
                hasInconsistentSettings = true;
                globalIssues.Add($"{info.buildingName}: {info.issues}");
            }
        }
        
        // Step 3: Check global consistency
        CheckGlobalConsistency(pixelsPerUnitCount, pivotTypeCount, globalIssues);
        
        // Step 4: Grid alignment check
        CheckGridAlignment();
        
        // Step 5: Generate report
        GenerateDebugReport();
        
        Debug.Log("🔍 =================== DEBUG CHECK COMPLETE ===================");
    }
    
    BuildingDebugInfo AnalyzeBuilding(BuildingComponent building)
    {
        BuildingDebugInfo info = new BuildingDebugInfo();
        
        // Basic info
        info.buildingName = building.name;
        info.transformPosition = building.transform.position;
        
        SpriteRenderer sr = building.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            info.issues = "Missing SpriteRenderer or Sprite";
            info.hasConsistentSettings = false;
            return info;
        }
        
        // Sprite info
        info.boundsCenter = sr.bounds.center;
        info.boundsSize = sr.bounds.size;
        info.spritePixelSize = new Vector2(sr.sprite.texture.width, sr.sprite.texture.height);
        info.spritePivot = sr.sprite.pivot;
        info.pixelsPerUnit = sr.sprite.pixelsPerUnit;
        info.localScale = building.transform.localScale;
        
        // Check for issues
        List<string> issues = new List<string>();
        
        // Check pivot consistency
        string pivotType = GetPivotTypeDescription(info.spritePivot, info.spritePixelSize);
        if (!pivotType.Contains("Center"))
        {
            issues.Add($"Pivot not centered ({pivotType})");
        }
        
        // Check scale consistency
        if (info.localScale != Vector3.one)
        {
            issues.Add($"Non-standard scale ({info.localScale})");
        }
        
        // Check size consistency
        Vector2 expectedSize = expectedBuildingSize;
        float sizeTolerance = 0.1f;
        if (Mathf.Abs(info.boundsSize.x - expectedSize.x) > sizeTolerance ||
            Mathf.Abs(info.boundsSize.y - expectedSize.y) > sizeTolerance)
        {
            issues.Add($"Size mismatch (Expected: {expectedSize}, Actual: {info.boundsSize})");
        }
        
        // Check grid alignment
        Vector3 gridAlignedPos = SnapToGrid(info.transformPosition);
        if (Vector3.Distance(info.transformPosition, gridAlignedPos) > 0.01f)
        {
            issues.Add($"Not grid-aligned (Off by {Vector3.Distance(info.transformPosition, gridAlignedPos):F3})");
        }
        
        info.issues = string.Join(", ", issues);
        info.hasConsistentSettings = issues.Count == 0;
        
        // Debug log for this building
        Debug.Log($"🏠 {info.buildingName}:");
        Debug.Log($"   📍 Position: {info.transformPosition}");
        Debug.Log($"   📦 Bounds: Center={info.boundsCenter}, Size={info.boundsSize}");
        Debug.Log($"   🎭 Sprite: {info.spritePixelSize}px, Pivot={info.spritePivot}, PPU={info.pixelsPerUnit}");
        Debug.Log($"   ⚖️ Scale: {info.localScale}");
        Debug.Log($"   ✅ Pivot Type: {pivotType}");
        
        if (!info.hasConsistentSettings)
        {
            Debug.LogWarning($"   ⚠️ Issues: {info.issues}");
        }
        else
        {
            Debug.Log($"   ✅ All settings consistent");
        }
        
        return info;
    }
    
    string GetPivotTypeDescription(Vector2 pivot, Vector2 spriteSize)
    {
        Vector2 normalizedPivot = new Vector2(pivot.x / spriteSize.x, pivot.y / spriteSize.y);
        
        // Check horizontal
        string horizontal = "";
        if (normalizedPivot.x < 0.1f) horizontal = "Left";
        else if (normalizedPivot.x > 0.9f) horizontal = "Right";
        else if (normalizedPivot.x > 0.4f && normalizedPivot.x < 0.6f) horizontal = "Center";
        else horizontal = "Custom";
        
        // Check vertical
        string vertical = "";
        if (normalizedPivot.y < 0.1f) vertical = "Bottom";
        else if (normalizedPivot.y > 0.9f) vertical = "Top";
        else if (normalizedPivot.y > 0.4f && normalizedPivot.y < 0.6f) vertical = "Center";
        else vertical = "Custom";
        
        return $"{vertical}-{horizontal}";
    }
    
    void CheckGlobalConsistency(Dictionary<float, int> pixelsPerUnitCount, Dictionary<string, int> pivotTypeCount, List<string> globalIssues)
    {
        Debug.Log("🔍 === GLOBAL CONSISTENCY CHECK ===");
        
        // Check pixels per unit consistency
        Debug.Log($"📊 Pixels Per Unit distribution:");
        foreach (var kvp in pixelsPerUnitCount)
        {
            Debug.Log($"   {kvp.Value} buildings use {kvp.Key} PPU");
        }
        
        if (pixelsPerUnitCount.Count > 1)
        {
            globalIssues.Add("Inconsistent Pixels Per Unit values across buildings");
            Debug.LogWarning("⚠️ Multiple Pixels Per Unit values detected - this can cause size inconsistencies!");
        }
        
        // Check pivot consistency
        Debug.Log($"📊 Pivot Type distribution:");
        foreach (var kvp in pivotTypeCount)
        {
            Debug.Log($"   {kvp.Value} buildings use {kvp.Key} pivot");
        }
        
        if (pivotTypeCount.Count > 1)
        {
            globalIssues.Add("Inconsistent Pivot types across buildings");
            Debug.LogWarning("⚠️ Multiple Pivot types detected - this causes horizontal/vertical placement inconsistencies!");
        }
        
        // Recommend fixes
        if (globalIssues.Count > 0)
        {
            Debug.LogWarning("🛠️ RECOMMENDED FIXES:");
            Debug.LogWarning("   1. Set all building sprites to use 'Center' pivot");
            Debug.LogWarning("   2. Use consistent Pixels Per Unit (recommended: 32 or 64)");
            Debug.LogWarning("   3. Ensure all buildings have the same target size");
        }
        
        overallIssues = string.Join("; ", globalIssues);
    }
    
    void CheckGridAlignment()
    {
        Debug.Log("🔍 === GRID ALIGNMENT CHECK ===");
        Debug.Log($"Grid Size: {gridSize}");
        
        int misalignedCount = 0;
        float maxMisalignment = 0f;
        
        foreach (BuildingDebugInfo info in allBuildingsInfo)
        {
            Vector3 gridPos = SnapToGrid(info.transformPosition);
            float misalignment = Vector3.Distance(info.transformPosition, gridPos);
            
            if (misalignment > 0.01f)
            {
                misalignedCount++;
                maxMisalignment = Mathf.Max(maxMisalignment, misalignment);
                
                Debug.Log($"🏠 {info.buildingName} misaligned by {misalignment:F3} units");
                Debug.Log($"   Current: {info.transformPosition}");
                Debug.Log($"   Should be: {gridPos}");
            }
        }
        
        if (misalignedCount > 0)
        {
            Debug.LogWarning($"⚠️ {misalignedCount} buildings are not grid-aligned!");
            Debug.LogWarning($"⚠️ Maximum misalignment: {maxMisalignment:F3} units");
            Debug.LogWarning("🛠️ Consider running a grid alignment fix script");
        }
        else
        {
            Debug.Log("✅ All buildings are properly grid-aligned");
        }
    }
    
    Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize,
            position.z
        );
    }
    
    void GenerateDebugReport()
    {
        Debug.Log("📋 === FINAL DEBUG REPORT ===");
        Debug.Log($"Total Buildings Analyzed: {allBuildingsInfo.Count}");
        
        int problematicBuildings = 0;
        foreach (var info in allBuildingsInfo)
        {
            if (!info.hasConsistentSettings) problematicBuildings++;
        }
        
        Debug.Log($"Buildings with Issues: {problematicBuildings}");
        Debug.Log($"Overall Health: {(problematicBuildings == 0 ? "✅ HEALTHY" : "⚠️ NEEDS ATTENTION")}");
        
        if (!string.IsNullOrEmpty(overallIssues))
        {
            Debug.LogWarning($"Global Issues: {overallIssues}");
        }
        
        // Specific recommendations
        if (hasInconsistentSettings)
        {
            Debug.LogWarning("🛠️ STEP-BY-STEP FIX GUIDE:");
            Debug.LogWarning("   Step 1: Select all building sprites in Project window");
            Debug.LogWarning("   Step 2: In Inspector, set Sprite Mode = Single, Pivot = Center");
            Debug.LogWarning("   Step 3: Set consistent Pixels Per Unit (32 recommended)");
            Debug.LogWarning("   Step 4: Apply settings and re-run debug check");
        }
    }
    
    void ToggleDebugVisualization()
    {
        debugVisualizationActive = !debugVisualizationActive;
        showDebugGizmos = debugVisualizationActive;
        
        string status = debugVisualizationActive ? "ON" : "OFF";
        Debug.Log($"🎨 Debug Visualization: {status}");
    }
    
    [ContextMenu("🛠️ Auto-Fix Grid Alignment")]
    public void AutoFixGridAlignment()
    {
        Debug.Log("🛠️ === AUTO-FIXING GRID ALIGNMENT ===");
        
        BuildingComponent[] allBuildings = FindObjectsOfType<BuildingComponent>();
        int fixedCount = 0;
        
        foreach (BuildingComponent building in allBuildings)
        {
            if (building == null) continue;
            
            Vector3 currentPos = building.transform.position;
            Vector3 gridPos = SnapToGrid(currentPos);
            
            if (Vector3.Distance(currentPos, gridPos) > 0.01f)
            {
                building.transform.position = gridPos;
                fixedCount++;
                Debug.Log($"🔧 Fixed {building.name}: {currentPos} → {gridPos}");
            }
        }
        
        Debug.Log($"✅ Fixed {fixedCount} buildings grid alignment");
        
        // Re-run debug check to verify
        if (fixedCount > 0)
        {
            Invoke("PerformFullDebugCheck", 0.1f);
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !debugVisualizationActive) return;
        
        // Draw grid
        if (showGridLines)
        {
            Gizmos.color = gridColor;
            Vector3 center = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            
            for (int x = -20; x <= 20; x++)
            {
                for (int y = -20; y <= 20; y++)
                {
                    Vector3 gridPoint = new Vector3(x * gridSize, y * gridSize, 0);
                    if (Vector3.Distance(gridPoint, center) < 15f) // Only draw nearby grid points
                    {
                        Gizmos.DrawWireCube(gridPoint, Vector3.one * 0.05f);
                    }
                }
            }
        }
        
        // Draw building debug info
        foreach (BuildingDebugInfo info in allBuildingsInfo)
        {
            // Draw bounds
            if (showBuildingBounds)
            {
                Gizmos.color = info.hasConsistentSettings ? boundsColor : problemColor;
                Gizmos.DrawWireCube(info.boundsCenter, info.boundsSize);
            }
            
            // Draw pivot point
            if (showPivotPoints)
            {
                Gizmos.color = info.hasConsistentSettings ? pivotColor : problemColor;
                Gizmos.DrawWireSphere(info.transformPosition, 0.1f);
            }
            
            // Draw grid alignment indicator
            Vector3 gridPos = SnapToGrid(info.transformPosition);
            float misalignment = Vector3.Distance(info.transformPosition, gridPos);
            
            if (misalignment > 0.01f)
            {
                Gizmos.color = problemColor;
                Gizmos.DrawLine(info.transformPosition, gridPos);
                Gizmos.DrawWireCube(gridPos, Vector3.one * 0.15f);
            }
        }
    }
}