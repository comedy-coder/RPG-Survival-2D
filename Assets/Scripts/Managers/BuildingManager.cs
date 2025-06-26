using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("Building System")]
    public BuildableItem[] availableBuildings = new BuildableItem[8];
    public float gridSize = 1f;
    public bool snapToGrid = true;
    
    [Header("Building Preview")]
    public Color validColor = Color.green;
    public Color invalidColor = Color.red;
    
    [Header("Debug Settings")]
    public bool enableDebugLogs = true; // ⭐ Changed to true by default
    
    [Header("Collision Settings")]
    public float collisionRadius = 0.2f; // ⭐ Reduced collision radius
    public LayerMask obstacleLayerMask = -1; // ⭐ Layer mask for obstacles
    public bool allowAdjacentBuildings = true; // ⭐ Allow buildings next to each other
    
    private bool isBuildingMode = false;
    private int selectedBuildingIndex = 0;
    private GameObject currentPreview;
    private Camera mainCamera;
    private SimpleInventory playerInventory;
    
    // ⭐ PUBLIC PROPERTY để other scripts có thể check
    public bool IsBuildingMode => isBuildingMode;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("❌ Main Camera not found!");
        }
        
        // ⭐ TRY MULTIPLE WAYS TO FIND SIMPLEINVENTORY
        playerInventory = GetComponent<SimpleInventory>();
        
        if (playerInventory == null)
        {
            // Try to find on Player GameObject
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerInventory = player.GetComponent<SimpleInventory>();
                if (playerInventory != null)
                {
                    Debug.Log("✅ Found SimpleInventory on Player GameObject!");
                }
            }
        }
        
        if (playerInventory == null)
        {
            // Try to find anywhere in scene
            playerInventory = FindObjectOfType<SimpleInventory>();
            if (playerInventory != null)
            {
                Debug.Log("✅ Found SimpleInventory in scene!");
            }
        }
        
        if (playerInventory == null)
        {
            Debug.LogWarning("⚠️ SimpleInventory not found anywhere! Materials checking will be disabled.");
        }
        
        if (enableDebugLogs)
        {
            Debug.Log("🏗️ Building Manager Started");
            Debug.Log($"📋 Available Buildings: {availableBuildings.Length}");
            
            // Check building setup
            for (int i = 0; i < availableBuildings.Length && i < 3; i++)
            {
                if (availableBuildings[i] != null)
                {
                    Debug.Log($"   Slot {i+1}: {availableBuildings[i].buildingName}");
                }
                else
                {
                    Debug.Log($"   Slot {i+1}: EMPTY");
                }
            }
        }
    }
    
    void Update()
    {
        HandleInput();
        
        if (isBuildingMode)
        {
            UpdatePreview();
            HandlePlacement();
        }
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildMode();
        }
        
        if (isBuildingMode)
        {
            // ⭐ SIMPLE & RELIABLE INPUT DETECTION
            
            // Check each key individually with debug
            if (Input.GetKeyDown(KeyCode.Alpha1)) 
            {
                if (enableDebugLogs) Debug.Log("🔑 Key 1 pressed!");
                SelectBuilding(0);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (enableDebugLogs) Debug.Log("🔑 Key 2 pressed!");
                SelectBuilding(1);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                if (enableDebugLogs) Debug.Log("🔑 Key 3 pressed!");
                SelectBuilding(2);
            }
            
            // Alternative keys (Q, W, E)
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (enableDebugLogs) Debug.Log("🔑 Key Q pressed (Foundation)!");
                SelectBuilding(0);
            }
            
            if (Input.GetKeyDown(KeyCode.W))
            {
                if (enableDebugLogs) Debug.Log("🔑 Key W pressed (Wall)!");
                SelectBuilding(1);
            }
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (enableDebugLogs) Debug.Log("🔑 Key E pressed (Workbench)!");
                SelectBuilding(2);
            }
            
            // Mouse wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f) CycleBuilding(1);
            if (scroll < 0f) CycleBuilding(-1);
        }
    }
    
    void ToggleBuildMode()
    {
        isBuildingMode = !isBuildingMode;
        
        if (isBuildingMode)
        {
            CreatePreview();
            Debug.Log("🏗️ BUILD MODE: ON");
            Debug.Log("📋 Controls: 1=Foundation, 2=Wall, 3=Workbench (or Q/W/E)");
            
            // ⭐ SHOW CURRENT INVENTORY STATUS
            if (playerInventory != null)
            {
                Debug.Log("📦 Current Inventory:");
                Debug.Log($"   Wood: {playerInventory.GetItemCount("Wood")}");
                Debug.Log($"   Stone: {playerInventory.GetItemCount("Stone")}");
                Debug.Log($"   Metal: {playerInventory.GetItemCount("Metal")}");
                Debug.Log("💡 Press T to add test materials if needed!");
            }
            
            // Show available buildings
            if (enableDebugLogs)
            {
                for (int i = 0; i < 3 && i < availableBuildings.Length; i++)
                {
                    if (availableBuildings[i] != null)
                    {
                        Debug.Log($"   {i+1}: {availableBuildings[i].buildingName}");
                    }
                }
            }
        }
        else
        {
            DestroyPreview();
            Debug.Log("🏗️ BUILD MODE: OFF");
        }
    }
    
    void SelectBuilding(int index)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 SelectBuilding called: index={index}");
        }
        
        if (index >= 0 && index < availableBuildings.Length)
        {
            if (availableBuildings[index] != null)
            {
                selectedBuildingIndex = index;
                CreatePreview(); // ⭐ FORCE RECREATE PREVIEW
                
                Debug.Log($"✅ Selected: {availableBuildings[index].buildingName}");
            }
            else
            {
                Debug.Log($"❌ Slot {index + 1} is empty!");
            }
        }
        else
        {
            Debug.Log($"❌ Invalid index: {index}");
        }
    }
    
    void CycleBuilding(int direction)
    {
        int startIndex = selectedBuildingIndex;
        int attempts = 0;
        
        do
        {
            selectedBuildingIndex += direction;
            if (selectedBuildingIndex >= availableBuildings.Length) selectedBuildingIndex = 0;
            if (selectedBuildingIndex < 0) selectedBuildingIndex = availableBuildings.Length - 1;
            attempts++;
        }
        while (availableBuildings[selectedBuildingIndex] == null && attempts < availableBuildings.Length);
        
        if (availableBuildings[selectedBuildingIndex] != null)
        {
            CreatePreview(); // ⭐ RECREATE PREVIEW
            Debug.Log($"🔄 Cycled to: {availableBuildings[selectedBuildingIndex].buildingName}");
        }
        else
        {
            selectedBuildingIndex = startIndex; // Revert if no valid building found
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
            
            if (enableDebugLogs)
                Debug.Log($"🎨 Created preview for: {availableBuildings[selectedBuildingIndex].buildingName}");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"❌ Cannot create preview - invalid building at index {selectedBuildingIndex}");
        }
    }
    
    void SetupPreview(GameObject preview)
    {
        // Disable scripts
        MonoBehaviour[] scripts = preview.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null) script.enabled = false;
        }
        
        // Disable colliders
        Collider2D[] colliders = preview.GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            if (col != null) col.enabled = false;
        }
        
        // Make transparent
        SpriteRenderer sr = preview.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color color = sr.color;
            color.a = 0.7f;
            sr.color = color;
        }
    }
    
    void UpdatePreview()
    {
        if (currentPreview != null && mainCamera != null)
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            
            if (snapToGrid)
            {
                mousePos = SnapToGrid(mousePos);
            }
            
            currentPreview.transform.position = mousePos;
            
            bool canPlace = CanPlace(mousePos);
            UpdatePreviewColor(canPlace);
        }
    }
    
    Vector3 SnapToGrid(Vector3 position)
    {
        float x = Mathf.Round(position.x / gridSize) * gridSize;
        float y = Mathf.Round(position.y / gridSize) * gridSize;
        return new Vector3(x, y, 0);
    }
    
    // ⭐ IMPROVED CANPLACE LOGIC WITH CONTROLLED DEBUG
    bool CanPlace(Vector3 position)
    {
        // ⭐ ONLY DEBUG ON MOUSE CLICK, NOT CONSTANTLY
        bool shouldDebug = Input.GetMouseButtonDown(0) && enableDebugLogs;
        
        if (shouldDebug) Debug.Log($"=== CAN PLACE DEBUG for {availableBuildings[selectedBuildingIndex]?.buildingName} ===");
        
        if (selectedBuildingIndex >= availableBuildings.Length)
        {
            if (shouldDebug) Debug.Log("❌ Invalid building index");
            return false;
        }
        
        BuildableItem building = availableBuildings[selectedBuildingIndex];
        if (building == null)
        {
            if (shouldDebug) Debug.Log("❌ No building selected");
            return false;
        }
        
        if (shouldDebug) Debug.Log($"🏗️ Checking: {building.buildingName}");
        
        // ⭐ MATERIALS CHECK (ONLY DEBUG ON CLICK)
        if (!HasMaterials(building, shouldDebug))
        {
            if (shouldDebug) Debug.Log($"❌ FAILED: Not enough materials for {building.buildingName}");
            return false;
        }
        else
        {
            if (shouldDebug) Debug.Log($"✅ PASSED: Materials check for {building.buildingName}");
        }
        
        // ⭐ ENHANCED COLLISION CHECK - MUCH LARGER AREA
        Vector2 checkSize = Vector2.one * 1.2f; // ⭐ Even larger check - should definitely overlap
        
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(position, checkSize, 0f);
        
        // ⭐ ALWAYS DEBUG COLLISION (temporarily)
        Debug.Log($"📐 COLLISION DEBUG: Checking at {position} with size {checkSize}");
        Debug.Log($"🔍 COLLISION DEBUG: Found {overlaps.Length} overlapping colliders");
        
        foreach (Collider2D col in overlaps)
        {
            if (col == null) continue;
            
            Debug.Log($"   → COLLISION DEBUG: Found collider: {col.gameObject.name} (Layer: {col.gameObject.layer}, Tag: {col.gameObject.tag})");
            
            // ⭐ SKIP PLAYER COLLIDER (ENHANCED)
            if (col.CompareTag("Player") || col.gameObject.name.Contains("Player")) 
            {
                Debug.Log($"   → SKIPPED: Player collider ({col.gameObject.name})");
                continue;
            }
            
            // ⭐ SKIP TRIGGER COLLIDERS
            if (col.isTrigger)
            {
                Debug.Log($"   → SKIPPED: Trigger collider ({col.gameObject.name})");
                continue;
            }
            
            // ⭐ SKIP RESOURCE ITEMS
            if (col.gameObject.name.Contains("Resource") || col.gameObject.name.Contains("Item"))
            {
                Debug.Log($"   → SKIPPED: Resource item ({col.gameObject.name})");
                continue;
            }
            
            // ⭐ PRIORITY 1: CHECK BY NAME (MOST RELIABLE)
            string objName = col.gameObject.name;
            if (objName.Contains("Foundation") || objName.Contains("Wall") || objName.Contains("Workbench"))
            {
                Debug.Log($"❌ BLOCKED: Building detected by name: {objName}");
                Debug.Log($"❌ COLLISION CHECK FAILED - SHOULD NOT PLACE HERE!");
                return false;
            }
            
            // ⭐ PRIORITY 2: CHECK FOR BUILDINGCOMPONENT
            var buildingComp = col.GetComponent<BuildingComponent>();
            if (buildingComp != null)
            {
                Debug.Log($"❌ BLOCKED: Building with BuildingComponent: {buildingComp.buildingName}");
                return false;
            }
            
            // ⭐ LOG ALL OTHER COLLIDERS
            Debug.Log($"   → UNKNOWN COLLIDER: {col.gameObject.name} - not blocking");
        }
        
        Debug.Log($"✅ COLLISION CHECK PASSED - ALLOWING PLACEMENT");
        return true;
        
        if (shouldDebug) Debug.Log($"✅ FINAL RESULT: Can place {building.buildingName} at {position}");
        return true;
    }
    
    // ⭐ HELPER METHOD FOR LAYER CHECKING
    bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) > 0;
    }
    
    // ⭐ ENHANCED MATERIALS CHECKING WITH CONTROLLED DEBUG + TEMP BYPASS
    bool HasMaterials(BuildableItem building, bool shouldDebug = false)
    {
        if (shouldDebug) Debug.Log($"🔍 === MATERIALS CHECK for {building.buildingName} ===");
        
        // ⭐ TEMPORARY BYPASS FOR WALL AND WORKBENCH
        if (building.buildingName.Contains("Wall") || building.buildingName.Contains("Workbench"))
        {
            if (shouldDebug) Debug.Log($"🔧 TEMPORARY BYPASS: Allowing {building.buildingName} without materials check");
            return true;
        }
        
        if (playerInventory == null) 
        {
            if (shouldDebug) Debug.Log("⚠️ No inventory found - allowing placement for testing");
            return true;
        }
        
        if (building.requiredMaterials == null) 
        {
            if (shouldDebug) Debug.Log("⚠️ requiredMaterials is NULL - allowing placement");
            return true;
        }
        
        if (building.requiredMaterials.Length == 0)
        {
            if (shouldDebug) Debug.Log("⚠️ requiredMaterials array is EMPTY - allowing placement");
            return true;
        }
        
        if (shouldDebug) Debug.Log($"📋 Checking {building.requiredMaterials.Length} required materials:");
        
        foreach (CraftingIngredient ingredient in building.requiredMaterials)
        {
            if (ingredient == null)
            {
                if (shouldDebug) Debug.Log("   ⚠️ NULL ingredient found - skipping");
                continue;
            }
            
            if (!ingredient.IsValid())
            {
                if (shouldDebug) Debug.Log($"   ⚠️ Invalid ingredient: {ingredient.materialName} - skipping");
                continue;
            }
            
            int currentAmount = playerInventory.GetItemCount(ingredient.materialName);
            bool hasEnough = currentAmount >= ingredient.amount;
            
            if (shouldDebug)
            {
                Debug.Log($"   📦 {ingredient.materialName}: Have {currentAmount}, Need {ingredient.amount} → {(hasEnough ? "✅ OK" : "❌ MISSING")}");
            }
            
            if (!hasEnough)
            {
                if (shouldDebug) 
                    Debug.Log($"❌ FAILED: Missing {ingredient.materialName} x{ingredient.amount - currentAmount}");
                return false;
            }
        }
        
        if (shouldDebug) Debug.Log("✅ PASSED: All materials available!");
        return true;
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
    
    // ⭐ ENHANCED PLACEMENT HANDLING WITH DETAILED DEBUG
    void HandlePlacement()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            
            if (snapToGrid)
            {
                mousePos = SnapToGrid(mousePos);
            }
            
            // ⭐ DETAILED DEBUG INFO
            if (enableDebugLogs)
            {
                Debug.Log($"=== PLACEMENT ATTEMPT ===");
                Debug.Log($"Building: {availableBuildings[selectedBuildingIndex]?.buildingName}");
                Debug.Log($"Position: {mousePos}");
                Debug.Log($"Grid Position: {mousePos}");
            }
            
            bool canPlace = CanPlace(mousePos);
            
            if (enableDebugLogs)
            {
                Debug.Log($"Can Place Result: {canPlace}");
            }
            
            if (canPlace)
            {
                PlaceBuilding(mousePos);
            }
            else
            {
                Debug.Log("❌ Cannot place building here!");
                
                // ⭐ ADDITIONAL DEBUG INFO FOR FAILED PLACEMENT
                if (enableDebugLogs)
                {
                    BuildableItem building = availableBuildings[selectedBuildingIndex];
                    
                    if (!HasMaterials(building))
                    {
                        Debug.Log("   → Reason: Not enough materials");
                    }
                    else
                    {
                        Debug.Log("   → Reason: Position blocked");
                        
                        // Show what's blocking
                        Vector2 checkSize;
                        if (allowAdjacentBuildings)
                        {
                            checkSize = Vector2.one * 0.5f; // ⭐ Updated to match collision check
                        }
                        else
                        {
                            checkSize = Vector2.one * (collisionRadius * 2f);
                        }
                        Collider2D[] overlaps = Physics2D.OverlapBoxAll(mousePos, checkSize, 0f);
                        
                        Debug.Log($"   → Found {overlaps.Length} blocking objects:");
                        foreach (Collider2D col in overlaps)
                        {
                            if (col != null && !col.CompareTag("Player") && !col.isTrigger)
                            {
                                var buildingComp = col.GetComponent<BuildingComponent>();
                                if (buildingComp != null)
                                {
                                    Debug.Log($"      • Building: {buildingComp.buildingName}");
                                }
                                else
                                {
                                    Debug.Log($"      • Object: {col.gameObject.name} (Layer: {col.gameObject.layer})");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    void PlaceBuilding(Vector3 position)
    {
        BuildableItem building = availableBuildings[selectedBuildingIndex];
        
        if (building.buildingPrefab == null)
        {
            Debug.Log("❌ No prefab assigned!");
            return;
        }
        
        // ⭐ CONSUME MATERIALS WITH DEBUG
        if (playerInventory != null && building.requiredMaterials != null)
        {
            if (enableDebugLogs) Debug.Log("💰 Consuming materials:");
            
            foreach (CraftingIngredient ingredient in building.requiredMaterials)
            {
                if (ingredient != null && ingredient.IsValid())
                {
                    playerInventory.RemoveItem(ingredient.materialName, ingredient.amount);
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"   - {ingredient.materialName} x{ingredient.amount}");
                    }
                }
            }
        }
        
        // ⭐ CREATE BUILDING
        GameObject newBuilding = Instantiate(building.buildingPrefab, position, Quaternion.identity);
        
        // ⭐ SET BUILDING TAG (SAFE)
        try
        {
            newBuilding.tag = "Building";
            if (enableDebugLogs) Debug.Log($"🏷️ Building tag set: {newBuilding.tag}");
        }
        catch (UnityEngine.UnityException)
        {
            if (enableDebugLogs) Debug.Log($"⚠️ Building tag not defined - skipping tag assignment");
        }
        
        // ⭐ Setup BuildingComponent
        var buildingComponent = newBuilding.GetComponent<BuildingComponent>();
        if (buildingComponent == null)
        {
            buildingComponent = newBuilding.AddComponent<BuildingComponent>();
        }
        
        if (buildingComponent != null)
        {
            buildingComponent.buildingName = building.buildingName;
            buildingComponent.maxDurability = building.maxDurability;
            buildingComponent.currentDurability = building.maxDurability;
            
            if (enableDebugLogs)
            {
                Debug.Log($"🏗️ BuildingComponent setup: {building.buildingName} ({building.maxDurability} HP)");
            }
        }
        
        Debug.Log($"✅ Successfully placed: {building.buildingName} at {position}");
        
        // ⭐ SHOW CURRENT INVENTORY STATUS
        if (enableDebugLogs && playerInventory != null)
        {
            Debug.Log("📦 Current Inventory:");
            Debug.Log($"   Wood: {playerInventory.GetItemCount("Wood")}");
            Debug.Log($"   Stone: {playerInventory.GetItemCount("Stone")}");
            Debug.Log($"   Metal: {playerInventory.GetItemCount("Metal")}");
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
    
    // ⭐ DEBUG GIZMOS FOR VISUAL DEBUGGING
    void OnDrawGizmos()
    {
        if (isBuildingMode && mainCamera != null && enableDebugLogs)
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            
            if (snapToGrid)
            {
                mousePos = SnapToGrid(mousePos);
            }
            
            // Draw placement area
            bool canPlace = CanPlace(mousePos);
            Gizmos.color = canPlace ? Color.green : Color.red;
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);
            
            // Draw collision check area
            Vector2 checkSize;
            if (allowAdjacentBuildings)
            {
                checkSize = Vector2.one * 0.5f; // ⭐ Updated to match collision check
            }
            else
            {
                checkSize = Vector2.one * (collisionRadius * 2f);
            }
            Gizmos.DrawCube(mousePos, checkSize);
            
            // Draw grid
            Gizmos.color = Color.white;
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
            Gizmos.DrawWireCube(mousePos, Vector3.one * gridSize);
        }
    }
    
    void OnDestroy()
    {
        DestroyPreview();
    }
}