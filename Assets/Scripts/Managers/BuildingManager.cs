using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BuildingManager : MonoBehaviour
{
    [Header("Building System")]
    public BuildableItem[] availableBuildings = new BuildableItem[3];
    public float gridSize = 1f;
    public bool snapToGrid = true;
    
    [Header("Building Preview")]
    public Color validColor = Color.green;
    public Color invalidColor = Color.red;
    
    [Header("🔲 SIMPLE GRID SYSTEM")]
    public bool showGridInGame = true;
    public Color gridColor = new Color(1f, 1f, 0f, 0.8f);
    public int gridRenderDistance = 10;
    public bool allowAdjacentBuilding = true; // Allow buildings to be next to each other
    
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    public bool showDebugGizmos = true;
    
    [Header("TEMPORARY BYPASS")]
    public bool bypassMaterialsCheck = true;
    
    // Events
    public static System.Action<BuildingComponent> OnBuildingPlaced;
    public static System.Action<BuildingComponent> OnBuildingDestroyed;
    
    // Private variables
    private bool isBuildingMode = false;
    private int selectedBuildingIndex = 0;
    private GameObject currentPreview;
    private Camera mainCamera;
    private SimpleInventory playerInventory;
    
    // Simple grid-based tracking - one building per grid cell
    private Dictionary<Vector2Int, BuildingComponent> gridOccupancy = new Dictionary<Vector2Int, BuildingComponent>();
    private List<BuildingComponent> allBuildings = new List<BuildingComponent>();
    
    // Grid visualization
    private GameObject gridContainer;
    private List<GameObject> gridDots = new List<GameObject>();
    private bool isGridVisible = false;
    
    public bool IsBuildingMode => isBuildingMode;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        playerInventory = FindFirstObjectByType<SimpleInventory>();
        
        // Create and show grid
        CreateSimpleGridVisualization();
        if (showGridInGame)
        {
            ShowGrid();
        }
        
        FindAndRegisterExistingBuildings();
        
        // Auto cleanup destroyed buildings
        StartCoroutine(CleanupDestroyedBuildings());
        
        Debug.Log("🏗️ SIMPLE GRID Building System Initialized!");
        Debug.Log($"🔧 Grid Size: {gridSize}");
        Debug.Log($"🔲 Adjacent Building: {(allowAdjacentBuilding ? "ALLOWED" : "BLOCKED")}");
        Debug.Log($"🎮 Controls: B=Build Mode, H=Toggle Grid, 1/2/3=Select");
        Debug.Log($"🔧 Registered Buildings: {allBuildings.Count}");
    }
    
    void Update()
    {
        HandleInput();
        if (isBuildingMode) 
            UpdateBuildingMode();
    }
    
    IEnumerator CleanupDestroyedBuildings()
    {
        yield return new WaitForSeconds(1f); // Wait for scene to load
        
        // Find all positions that should be cleared
        List<Vector2Int> positionsToRemove = new List<Vector2Int>();
        
        foreach (var kvp in gridOccupancy)
        {
            Vector2Int pos = kvp.Key;
            BuildingComponent building = kvp.Value;
            
            if (building == null || building.gameObject == null)
            {
                positionsToRemove.Add(pos);
            }
        }
        
        // Remove positions without buildings
        foreach (Vector2Int pos in positionsToRemove)
        {
            gridOccupancy.Remove(pos);
            Debug.Log($"🧹 Auto-cleared orphaned grid position: {pos}");
        }
        
        if (positionsToRemove.Count > 0)
        {
            Debug.Log($"✅ Cleaned up {positionsToRemove.Count} orphaned grid positions");
        }
    }
    
    void CreateSimpleGridVisualization()
    {
        // Create container for grid - fixed in world space
        gridContainer = new GameObject("SimpleGridVisualization");
        gridContainer.transform.position = Vector3.zero;
        
        // Create larger, more visible grid dots
        for (int x = -gridRenderDistance; x <= gridRenderDistance; x++)
        {
            for (int y = -gridRenderDistance; y <= gridRenderDistance; y++)
            {
                Vector3 gridPos = new Vector3(x * gridSize, y * gridSize, -0.1f);
                CreateVisibleGridDot(gridPos, $"GridDot_{x}_{y}");
            }
        }
        
        Debug.Log($"🔲 Created simple grid with {gridDots.Count} visible dots");
    }
    
    void CreateVisibleGridDot(Vector3 position, string name)
    {
        // Create dot object
        GameObject dot = new GameObject(name);
        dot.transform.SetParent(gridContainer.transform);
        dot.transform.position = position;
        
        // Add SpriteRenderer with larger, more visible sprite
        SpriteRenderer sr = dot.AddComponent<SpriteRenderer>();
        
        // Create larger texture for better visibility
        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        
        Sprite dotSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 20);
        sr.sprite = dotSprite;
        sr.color = gridColor;
        sr.sortingOrder = -50;
        
        // Make dots larger and more visible
        dot.transform.localScale = Vector3.one * 0.2f;
        
        gridDots.Add(dot);
    }
    
    void ShowGrid()
    {
        if (gridContainer != null)
        {
            gridContainer.SetActive(true);
            isGridVisible = true;
            Debug.Log("🔲 Simple Grid: VISIBLE");
        }
    }
    
    void HideGrid()
    {
        if (gridContainer != null)
        {
            gridContainer.SetActive(false);
            isGridVisible = false;
            Debug.Log("🔲 Simple Grid: HIDDEN");
        }
    }
    
    void ToggleGrid()
    {
        if (isGridVisible)
            HideGrid();
        else
            ShowGrid();
    }
    
    void FindAndRegisterExistingBuildings()
    {
        gridOccupancy.Clear();
        allBuildings.Clear();
        
        Debug.Log("🔍 === SCANNING FOR EXISTING BUILDINGS (SIMPLE GRID) ===");
        
        BuildingComponent[] buildingComponents = FindObjectsByType<BuildingComponent>(FindObjectsSortMode.None);
        Debug.Log($"Found {buildingComponents.Length} BuildingComponents");
        
        foreach (BuildingComponent building in buildingComponents)
        {
            RegisterBuildingOnGrid(building);
        }
        
        // Also find buildings by name pattern
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int foundByName = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj == null || !obj.activeInHierarchy) 
                continue;
            
            string name = obj.name.ToLower();
            if ((name.Contains("foundation") || name.Contains("wall") || name.Contains("workbench")) 
                && !name.Contains("clone") && !name.Contains("preview"))
            {
                BuildingComponent comp = obj.GetComponent<BuildingComponent>();
                if (comp == null)
                {
                    comp = obj.AddComponent<BuildingComponent>();
                    comp.buildingName = obj.name;
                    Debug.Log($"🔧 Added BuildingComponent to {obj.name}");
                }
                
                if (!allBuildings.Contains(comp))
                {
                    RegisterBuildingOnGrid(comp);
                    foundByName++;
                }
            }
        }
        
        Debug.Log($"Found {foundByName} additional buildings by name pattern");
        Debug.Log($"🏠 TOTAL REGISTERED BUILDINGS: {allBuildings.Count}");
        Debug.Log($"🔲 Grid cells occupied: {gridOccupancy.Count}");
    }
    
    void RegisterBuildingOnGrid(BuildingComponent building)
    {
        if (building == null) return;
        
        Vector3 worldPos = building.transform.position;
        Vector2Int gridPos = WorldToGrid(worldPos);
        
        Debug.Log($"📍 Registering building:");
        Debug.Log($"   → Name: {building.buildingName}");
        Debug.Log($"   → World Position: {worldPos}");
        Debug.Log($"   → Grid Position: {gridPos}");
        
        // FORCE BUILDING TO EXACT GRID POSITION
        Vector3 snappedWorldPos = GridToWorld(gridPos);
        building.transform.position = snappedWorldPos;
        
        // FORCE ALL BUILDINGS TO SAME SIZE (1x1 grid cell)
        building.transform.localScale = Vector3.one;
        
        // Find a free grid position if current is occupied
        Vector2Int finalGridPos = FindNearestFreeGridPosition(gridPos);
        if (finalGridPos != gridPos)
        {
            Vector3 finalWorldPos = GridToWorld(finalGridPos);
            building.transform.position = finalWorldPos;
            Debug.Log($"   → Moved to free position: grid {finalGridPos}, world {finalWorldPos}");
        }
        
        // Register in grid
        gridOccupancy[finalGridPos] = building;
        allBuildings.Add(building);
        
        Debug.Log($"   → ✅ Registered at grid {finalGridPos}, exact world {GridToWorld(finalGridPos)}");
    }
    
    Vector2Int FindNearestFreeGridPosition(Vector2Int startPos)
    {
        // Check if start position is free
        if (!gridOccupancy.ContainsKey(startPos))
        {
            return startPos;
        }
        
        // Search in expanding square pattern
        for (int radius = 1; radius <= 10; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) == radius || Mathf.Abs(y) == radius) // Only perimeter
                    {
                        Vector2Int testPos = startPos + new Vector2Int(x, y);
                        if (!gridOccupancy.ContainsKey(testPos))
                        {
                            return testPos;
                        }
                    }
                }
            }
        }
        
        // Fallback
        return startPos;
    }
    
    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / gridSize);
        int y = Mathf.RoundToInt(worldPos.y / gridSize);
        return new Vector2Int(x, y);
    }
    
    Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * gridSize, gridPos.y * gridSize, 0);
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildMode();
        }
        
        if (Input.GetKeyDown(KeyCode.H))
        {
            ToggleGrid();
        }
        
        if (isBuildingMode)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SelectBuilding(0);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                SelectBuilding(1);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                SelectBuilding(2);
            
            if (Input.GetMouseButtonDown(0))
                TryPlaceBuilding();
            
            if (Input.GetKeyDown(KeyCode.X))
                ShowGridInfo();
                
            // NEW: Fix all overlaps
            if (Input.GetKeyDown(KeyCode.R))
                FixAllOverlaps();
        }
    }
    
    void FixAllOverlaps()
    {
        Debug.Log("🔧 === FIXING ALL OVERLAPS - FORCE STANDARDIZATION ===");
        
        // Clear current grid
        gridOccupancy.Clear();
        
        // Re-register all buildings with standardization
        int fixedCount = 0;
        foreach (BuildingComponent building in allBuildings.ToArray()) // ToArray to avoid modification during iteration
        {
            if (building == null) continue;
            
            Vector3 originalPos = building.transform.position;
            
            // FORCE STANDARD SIZE
            building.transform.localScale = Vector3.one;
            
            // Find free grid position
            Vector2Int gridPos = WorldToGrid(originalPos);
            Vector2Int freeGridPos = FindNearestFreeGridPosition(gridPos);
            
            // Move to exact grid position
            Vector3 exactWorldPos = GridToWorld(freeGridPos);
            building.transform.position = exactWorldPos;
            
            // Register in clean grid
            gridOccupancy[freeGridPos] = building;
            
            if (Vector3.Distance(originalPos, exactWorldPos) > 0.1f)
            {
                fixedCount++;
                Debug.Log($"🔧 Fixed {building.buildingName}: {originalPos} → {exactWorldPos}");
            }
        }
        
        Debug.Log($"✅ OVERLAP FIX COMPLETE! Fixed {fixedCount} buildings.");
        Debug.Log($"📊 All buildings now at exact grid positions with standard size.");
    }
    
    void ShowGridInfo()
    {
        Debug.Log("🔍 === SIMPLE GRID INFO ===");
        Debug.Log($"Total Buildings: {allBuildings.Count}");
        Debug.Log($"Grid cells occupied: {gridOccupancy.Count}");
        Debug.Log($"Grid Size: {gridSize}");
        Debug.Log($"Adjacent Building: {allowAdjacentBuilding}");
        
        foreach (var kvp in gridOccupancy)
        {
            Vector2Int gridPos = kvp.Key;
            BuildingComponent building = kvp.Value;
            Vector3 worldPos = GridToWorld(gridPos);
            Debug.Log($"  Grid {gridPos}: {building.buildingName} at world {worldPos}");
        }
    }
    
    void UpdateBuildingMode()
    {
        if (currentPreview != null && mainCamera != null)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            Vector2Int gridPos = WorldToGrid(mousePos);
            Vector3 snapPos = GridToWorld(gridPos);
            
            // Update preview position to exact grid position
            currentPreview.transform.position = snapPos;
            
            // Check if grid cell is available
            bool canPlace = CanPlaceBuildingAtGrid(gridPos);
            UpdatePreviewColor(canPlace);
            
            if (enableDebugLogs && Input.GetKey(KeyCode.LeftShift))
            {
                Debug.Log($"🎭 Preview: Mouse {mousePos} → Grid {gridPos} → World {snapPos}, Can Place: {canPlace}");
            }
        }
    }
    
    bool CanPlaceBuildingAtGrid(Vector2Int gridPos)
    {
        if (selectedBuildingIndex >= availableBuildings.Length) 
        {
            if (enableDebugLogs) Debug.Log("❌ BLOCKED: Invalid building index");
            return false;
        }
        
        BuildableItem building = availableBuildings[selectedBuildingIndex];
        if (building == null) 
        {
            if (enableDebugLogs) Debug.Log("❌ BLOCKED: Building is null");
            return false;
        }
        
        if (!bypassMaterialsCheck && !HasRequiredMaterials(building)) 
        {
            if (enableDebugLogs) Debug.Log("❌ BLOCKED: Not enough materials");
            return false;
        }
        
        // Simple check: is this grid cell occupied?
        if (gridOccupancy.ContainsKey(gridPos))
        {
            if (enableDebugLogs) 
                Debug.Log($"❌ BLOCKED: Grid cell {gridPos} is occupied by {gridOccupancy[gridPos].buildingName}");
            return false;
        }
        
        if (enableDebugLogs)
            Debug.Log($"✅ CAN PLACE: Grid cell {gridPos} is free");
        
        return true;
    }
    
    void TryPlaceBuilding()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Vector2Int gridPos = WorldToGrid(mousePos);
        Vector3 finalPos = GridToWorld(gridPos);
        
        Debug.Log($"🎯 === SIMPLE GRID PLACEMENT ===");
        Debug.Log($"Building: {availableBuildings[selectedBuildingIndex]?.buildingName}");
        Debug.Log($"Mouse Position: {mousePos}");
        Debug.Log($"Grid Position: {gridPos}");
        Debug.Log($"Final World Position: {finalPos}");
        
        if (CanPlaceBuildingAtGrid(gridPos))
        {
            PlaceBuilding(gridPos, finalPos);
        }
        else
        {
            Debug.Log("❌ PLACEMENT BLOCKED");
        }
    }
    
    void PlaceBuilding(Vector2Int gridPos, Vector3 worldPos)
    {
        BuildableItem building = availableBuildings[selectedBuildingIndex];
        
        if (!bypassMaterialsCheck)
        {
            ConsumeMaterials(building);
        }
        
        GameObject newBuilding = Instantiate(building.buildingPrefab, worldPos, Quaternion.identity);
        
        // FORCE STANDARD SIZE AND POSITION
        newBuilding.transform.localScale = Vector3.one;
        newBuilding.transform.position = worldPos; // Ensure exact position
        
        BuildingComponent buildingComp = SetupNewBuilding(newBuilding, building);
        
        // Register in grid system
        gridOccupancy[gridPos] = buildingComp;
        allBuildings.Add(buildingComp);
        
        OnBuildingPlaced?.Invoke(buildingComp);
        
        Debug.Log($"🎉 SUCCESS: {building.buildingName} placed at grid {gridPos}, world {worldPos}");
        Debug.Log($"📊 Total buildings: {allBuildings.Count}");
        Debug.Log($"📊 Grid cells occupied: {gridOccupancy.Count}");
    }
    
    BuildingComponent SetupNewBuilding(GameObject newBuilding, BuildableItem buildingData)
    {
        BuildingComponent buildingComp = newBuilding.GetComponent<BuildingComponent>();
        if (buildingComp == null)
            buildingComp = newBuilding.AddComponent<BuildingComponent>();
        
        buildingComp.buildingName = buildingData.buildingName;
        buildingComp.buildingType = GetBuildingType(buildingData);
        
        // Set durability if fields exist
        if (buildingData.maxDurability > 0)
        {
            try
            {
                buildingComp.maxDurability = buildingData.maxDurability;
                buildingComp.currentDurability = buildingData.maxDurability;
            }
            catch
            {
                // Ignore if these fields don't exist
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"🏗️ {buildingData.buildingName} setup complete");
        
        return buildingComp;
    }
    
    BuildingComponent.BuildingType GetBuildingType(BuildableItem building)
    {
        string name = building.buildingName.ToLower();
        if (name.Contains("foundation")) return BuildingComponent.BuildingType.Foundation;
        if (name.Contains("wall")) return BuildingComponent.BuildingType.Wall;
        if (name.Contains("workbench")) return BuildingComponent.BuildingType.Workbench;
        return BuildingComponent.BuildingType.Foundation;
    }
    
    public void OnBuildingDestroyedCallback(BuildingComponent building)
    {
        if (building == null) return;
        
        Vector2Int gridPos = WorldToGrid(building.transform.position);
        
        // Remove from grid tracking
        gridOccupancy.Remove(gridPos);
        allBuildings.Remove(building);
        
        OnBuildingDestroyed?.Invoke(building);
        
        Debug.Log($"🏗️ Building destroyed: {building.buildingName} at grid {gridPos}");
        Debug.Log($"📊 Remaining buildings: {allBuildings.Count}");
    }
    
    // ==================== GRID POSITION MANAGEMENT ====================
    
    public void ClearGridPosition(Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGrid(worldPosition);
        
        if (gridOccupancy.ContainsKey(gridPos))
        {
            BuildingComponent building = gridOccupancy[gridPos];
            gridOccupancy.Remove(gridPos);
            allBuildings.Remove(building);
            Debug.Log($"🗑️ Cleared grid position: {gridPos} at world position: {worldPosition}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Grid position {gridPos} was not found in occupied positions");
        }
    }
    
    // Debug method để xem grid positions
    [ContextMenu("Debug Grid Positions")]
    public void DebugGridPositions()
    {
        Debug.Log($"📍 === OCCUPIED GRID POSITIONS ===");
        Debug.Log($"Total occupied positions: {gridOccupancy.Count}");
        
        foreach (var kvp in gridOccupancy)
        {
            Vector2Int pos = kvp.Key;
            BuildingComponent building = kvp.Value;
            string buildingName = building != null ? building.buildingName : "NULL";
            Debug.Log($"Grid: {pos} | Building: {buildingName} | World: ({pos.x}, {pos.y})");
        }
    }
    
    // Method để clear tất cả positions (emergency use)
    [ContextMenu("Clear All Grid Positions")]
    public void ClearAllGridPositions()
    {
        int count = gridOccupancy.Count;
        gridOccupancy.Clear();
        allBuildings.Clear();
        Debug.Log($"🧹 Cleared all {count} grid positions");
        Debug.Log($"✅ You can now rebuild at any position!");
    }
    
    // Method để validate và fix grid positions
    [ContextMenu("Validate Grid Positions")]
    public void ValidateGridPositions()
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        List<BuildingComponent> validBuildings = new List<BuildingComponent>();
        
        foreach (var kvp in gridOccupancy)
        {
            Vector2Int pos = kvp.Key;
            BuildingComponent building = kvp.Value;
            
            if (building != null && building.gameObject != null)
            {
                validPositions.Add(pos);
                validBuildings.Add(building);
            }
        }
        
        int removedCount = gridOccupancy.Count - validPositions.Count;
        gridOccupancy.Clear();
        allBuildings.Clear();
        
        // Re-add valid buildings
        for (int i = 0; i < validPositions.Count; i++)
        {
            gridOccupancy[validPositions[i]] = validBuildings[i];
            allBuildings.Add(validBuildings[i]);
        }
        
        Debug.Log($"🔍 Validated grid positions: {validPositions.Count} valid, {removedCount} removed");
        
        if (removedCount > 0)
        {
            Debug.Log($"✅ Fixed {removedCount} orphaned positions - you can now rebuild there!");
        }
    }
    
    // ==================== END GRID POSITION MANAGEMENT ====================
    
    bool HasRequiredMaterials(BuildableItem building)
    {
        if (playerInventory == null) return true;
        if (building.requiredMaterials == null) return true;
        
        foreach (var material in building.requiredMaterials)
        {
            if (material == null || !material.IsValid()) continue;
            if (playerInventory.GetItemCount(material.materialName) < material.amount)
                return false;
        }
        return true;
    }
    
    void ConsumeMaterials(BuildableItem building)
    {
        if (playerInventory == null || building.requiredMaterials == null) return;
        
        foreach (var material in building.requiredMaterials)
        {
            if (material != null && material.IsValid())
            {
                playerInventory.RemoveItem(material.materialName, material.amount);
            }
        }
    }
    
    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;
        return worldPos;
    }
    
    void ToggleBuildMode()
    {
        isBuildingMode = !isBuildingMode;
        
        if (isBuildingMode)
        {
            CreatePreview();
            Debug.Log($"🏗️ SIMPLE GRID BUILD MODE: ON");
            Debug.Log("🎮 Controls:");
            Debug.Log("   1=Foundation, 2=Wall, 3=Workbench");
            Debug.Log("   H=Toggle Grid, X=Grid Info");
            Debug.Log("   Buildings will snap to grid and can be adjacent!");
        }
        else
        {
            DestroyPreview();
            Debug.Log("🏗️ BUILD MODE: OFF");
        }
    }
    
    void SelectBuilding(int index)
    {
        if (index >= 0 && index < availableBuildings.Length && availableBuildings[index] != null)
        {
            selectedBuildingIndex = index;
            CreatePreview();
            Debug.Log($"✅ Selected: {availableBuildings[index].buildingName}");
        }
    }
    
    void CreatePreview()
    {
        DestroyPreview();
        
        if (selectedBuildingIndex < availableBuildings.Length && 
            availableBuildings[selectedBuildingIndex] != null &&
            availableBuildings[selectedBuildingIndex].buildingPrefab != null)
        {
            currentPreview = Instantiate(availableBuildings[selectedBuildingIndex].buildingPrefab);
            SetupPreview(currentPreview);
        }
    }
    
    void SetupPreview(GameObject preview)
    {
        // Disable all components for preview
        MonoBehaviour[] scripts = preview.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
            if (script != null) script.enabled = false;
        
        Collider2D[] colliders = preview.GetComponents<Collider2D>();
        foreach (var col in colliders)
            if (col != null) col.enabled = false;
        
        // Make transparent
        SpriteRenderer sr = preview.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color color = sr.color;
            color.a = 0.7f;
            sr.color = color;
        }
    }
    
    void UpdatePreviewColor(bool canPlace)
    {
        if (currentPreview != null)
        {
            SpriteRenderer sr = currentPreview.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color color = canPlace ? validColor : invalidColor;
                color.a = 0.7f;
                sr.color = color;
            }
        }
    }
    
    void DestroyPreview()
    {
        if (currentPreview != null)
        {
            DestroyImmediate(currentPreview);
            currentPreview = null;
        }
    }
    
    void OnDestroy()
    {
        DestroyPreview();
        
        // Clean up grid visualization
        if (gridContainer != null)
        {
            DestroyImmediate(gridContainer);
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw occupied grid cells as red squares
        if (Application.isPlaying && gridOccupancy != null)
        {
            Gizmos.color = Color.red;
            foreach (var kvp in gridOccupancy)
            {
                Vector2Int gridPos = kvp.Key;
                Vector3 worldPos = GridToWorld(gridPos);
                Gizmos.DrawWireCube(worldPos, Vector3.one * gridSize * 0.9f);
            }
        }
        
        // Draw grid in Scene View
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        for (int x = -10; x <= 10; x++)
        {
            for (int y = -10; y <= 10; y++)
            {
                Vector3 gridPos = new Vector3(x * gridSize, y * gridSize, 0);
                Gizmos.DrawWireCube(gridPos, Vector3.one * 0.05f);
            }
        }
    }
}