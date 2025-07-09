using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SurvivalToolSystem : MonoBehaviour
{
    [Header("Tool Configuration")]
    public List<SurvivalToolData> availableTools = new List<SurvivalToolData>();
    public int currentToolIndex = 0;
    
    [Header("🔨 Destruction Settings")]
    [Range(0.5f, 5f)]
    public float maxDestructionRange = 1.5f;
    public bool showRangeIndicator = true;
    public Color inRangeColor = Color.green;
    public Color outOfRangeColor = Color.red;
    
    [Header("Input Settings")]
    public KeyCode tool1Key = KeyCode.Alpha1;
    public KeyCode tool2Key = KeyCode.Alpha2;
    public KeyCode tool3Key = KeyCode.Alpha3;
    public bool enableMouseWheelSwitching = true;
    public float switchCooldown = 0.2f;
    
    [Header("UI Settings")]
    public bool showToolUI = true;
    public bool showToolInfo = true;
    public bool showDebugInfo = false; // ✅ DISABLED: Was true

    // Private variables
    private SurvivalToolData currentTool;
    private float lastUseTime = 0f;
    private float lastSwitchTime = 0f;
    private Dictionary<SurvivalToolData, float> toolDurabilities = new Dictionary<SurvivalToolData, float>();
    
    // Component references
    private Camera mainCamera;
    private SimpleInventory inventory;
    private BuildingManager buildingManager;
    
    // Gathering system
    private bool isGathering = false;
    private Coroutine gatherCoroutine;
    private SurvivalResourceItem currentTarget;
    
    // Build mode protection
    private float lastBuildModeTime = 0f;
    private bool wasBuildModeActive = false;

    #region Initialization

    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        // Get component references
        mainCamera = Camera.main;
        inventory = GetComponent<SimpleInventory>();
        buildingManager = GetComponent<BuildingManager>();
        
        // Initialize tool durabilities
        InitializeToolDurabilities();
        
        // ✅ MINIMAL DEBUG: Only essential initialization logging
        if (showDebugInfo)
        {
            Debug.Log($"🔧 SurvivalToolSystem: Initialized with {availableTools.Count} tools");
        }
        
        // Set default tool
        if (availableTools.Count > 0 && availableTools[0] != null)
        {
            SwitchToTool(0);
        }
    }
    
    void InitializeToolDurabilities()
    {
        toolDurabilities.Clear();
        
        foreach (SurvivalToolData tool in availableTools)
        {
            if (tool != null)
            {
                toolDurabilities[tool] = tool.maxDurability;
            }
        }
    }
    
    #endregion
    
    #region Update and Input
    
    void Update()
    {
        if (currentTool != null && currentTool.toolType == SurvivalToolType.Hammer)
        {
            TrackBuildMode();
            
            if (showRangeIndicator)
            {
                ShowRangeIndicator();
            }
        }
        
        HandleInput();
    }
    
    void HandleInput()
    {
        HandleToolSwitching();
        
        if (currentTool != null)
        {
            HandleToolUsage();
        }
    }
    
    void HandleToolSwitching()
    {
        if (Time.time - lastSwitchTime < switchCooldown) return;
        
        // Number key switching
        if (Input.GetKeyDown(tool1Key))
        {
            SwitchToTool(0);
        }
        else if (Input.GetKeyDown(tool2Key))
        {
            SwitchToTool(1);
        }
        else if (Input.GetKeyDown(tool3Key))
        {
            SwitchToTool(2);
        }
        
        // Mouse wheel switching
        if (enableMouseWheelSwitching)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.1f)
            {
                int direction = scroll > 0 ? -1 : 1;
                int newIndex = (currentToolIndex + direction + availableTools.Count) % availableTools.Count;
                SwitchToTool(newIndex);
            }
        }
    }
    
    void HandleToolUsage()
    {
        if (Input.GetMouseButtonDown(0))
        {
            UseTool();
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            UseToolSecondary();
        }
        
        if (Input.GetMouseButtonUp(0) && isGathering)
        {
            StopGathering();
        }
    }
    
    #endregion
    
    #region Tool Management
    
    public void SwitchToTool(int toolIndex)
    {
        if (toolIndex < 0 || toolIndex >= availableTools.Count) return;
        if (availableTools[toolIndex] == null) return;
        
        if (isGathering)
        {
            StopGathering();
        }
        
        currentToolIndex = toolIndex;
        currentTool = availableTools[toolIndex];
        lastSwitchTime = Time.time;
        
        // ✅ REMOVED: Debug log for tool switching
    }
    
    public SurvivalToolData GetCurrentTool()
    {
        return currentTool;
    }
    
    public string GetCurrentToolName()
    {
        return currentTool != null ? currentTool.toolName : "None";
    }
    
    public float GetCurrentToolDurability()
    {
        if (currentTool == null) return 0f;
        return toolDurabilities.ContainsKey(currentTool) ? toolDurabilities[currentTool] : 0f;
    }
    
    public bool IsCurrentToolBroken()
    {
        return GetCurrentToolDurability() <= 0f;
    }
    
    #endregion
    
    #region Tool Usage
    
    void UseTool()
    {
        if (!CanUseTool()) return;
        
        switch (currentTool.toolType)
        {
            case SurvivalToolType.Hammer:
                UseHammer();
                break;
            case SurvivalToolType.Axe:
                UseAxe();
                break;
            case SurvivalToolType.Pickaxe:
                UsePickaxe();
                break;
        }
        
        ConsumeDurability();
        lastUseTime = Time.time;
    }
    
    void UseToolSecondary()
    {
        if (!CanUseTool()) return;
        
        switch (currentTool.toolType)
        {
            case SurvivalToolType.Hammer:
                if (currentTool.canRepairBuildings)
                {
                    RepairBuilding();
                }
                break;
            case SurvivalToolType.Pickaxe:
                if (currentTool.canFindRareResources)
                {
                    ScanForRareResources();
                }
                break;
        }
        
        lastUseTime = Time.time;
    }
    
    bool CanUseTool()
    {
        if (currentTool == null) return false;
        if (IsOnCooldown()) return false;
        if (IsCurrentToolBroken()) return false;
        
        if (currentTool.toolType == SurvivalToolType.Hammer && ShouldBlockDestruction())
        {
            return false;
        }
        
        return true;
    }
    
    bool IsOnCooldown()
    {
        return Time.time - lastUseTime < currentTool.cooldownTime;
    }
    
    void ConsumeDurability()
    {
        if (currentTool == null) return;
        
        float currentDurability = GetCurrentToolDurability();
        currentDurability = Mathf.Max(0f, currentDurability - currentTool.durabilityLossPerUse);
        toolDurabilities[currentTool] = currentDurability;
        
        // ✅ REMOVED: Debug log for durability consumption
    }
    
    #endregion
    
    #region Hammer Tool
    
    void UseHammer()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        
        float distanceToClick = Vector3.Distance(transform.position, mousePos);
        
        if (distanceToClick > maxDestructionRange)
        {
            ShowOutOfRangeEffect(mousePos);
            return;
        }
        
        BuildingComponent target = FindNearestBuilding(mousePos);
        
        if (target != null)
        {
            float distanceToBuilding = Vector3.Distance(transform.position, target.transform.position);
            
            if (distanceToBuilding <= maxDestructionRange)
            {
                AttackBuilding(target);
            }
            else
            {
                ShowOutOfRangeEffect(target.transform.position);
            }
        }
    }
    
    void AttackBuilding(BuildingComponent building)
    {
        float effectiveness = building.GetToolEffectiveness(currentTool.toolName);
        int finalDamage = Mathf.RoundToInt(currentTool.damagePerHit * effectiveness);
        
        // Critical hit chance (10%)
        bool isCritical = Random.Range(0f, 1f) < 0.1f;
        if (isCritical)
        {
            finalDamage = Mathf.RoundToInt(finalDamage * 1.5f);
        }
        
        building.TakeDamage(finalDamage);
        building.ShowDamageEffect();
        
        PlayToolEffects(building.transform.position, isCritical);
        
        // ✅ MINIMAL DEBUG: Only log if debug is enabled
        if (showDebugInfo)
        {
            float distance = Vector3.Distance(transform.position, building.transform.position);
            Debug.Log($"🔨 {currentTool.toolName} hit {building.buildingName} for {finalDamage} damage" + 
                     (isCritical ? " (CRITICAL!)" : "") + 
                     $" | Distance: {distance:F1}m");
        }
    }
    
    void RepairBuilding()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        
        float distanceToClick = Vector3.Distance(transform.position, mousePos);
        if (distanceToClick > maxDestructionRange)
        {
            return;
        }
        
        BuildingComponent target = FindNearestBuilding(mousePos);
        
        if (target != null && target.CanBeRepaired())
        {
            float distanceToBuilding = Vector3.Distance(transform.position, target.transform.position);
            if (distanceToBuilding > maxDestructionRange)
            {
                return;
            }
            
            string materialNeeded = target.GetMaterialType();
            int materialCost = currentTool.repairCostWood;
            
            if (inventory != null && inventory.GetItemCount(materialNeeded) >= materialCost)
            {
                int repairAmount = Mathf.RoundToInt(currentTool.damagePerHit * 0.5f);
                target.Repair(repairAmount);
                target.ShowRepairEffect();
                inventory.RemoveItem(materialNeeded, materialCost);
                
                // ✅ MINIMAL DEBUG: Only if debug enabled
                if (showDebugInfo)
                    Debug.Log($"🔧 Repaired {target.buildingName} for {repairAmount} HP using {materialCost} {materialNeeded}");
            }
        }
    }
    
    #endregion
    
    #region Axe and Pickaxe Tools
    
    void UseAxe()
    {
        if (isGathering) return;
        
        Vector3 mousePos = GetMouseWorldPosition();
        SurvivalResourceItem target = FindNearestResource(mousePos, "wood");
        
        if (target != null)
        {
            StartGathering(target);
        }
    }
    
    void UsePickaxe()
    {
        if (isGathering) return;
        
        Vector3 mousePos = GetMouseWorldPosition();
        SurvivalResourceItem target = FindNearestResource(mousePos, "stone");
        
        if (target != null)
        {
            StartGathering(target);
        }
    }
    
    void ScanForRareResources()
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, currentTool.attackRange * 2f);
        
        int foundCount = 0;
        foreach (Collider2D col in nearbyColliders)
        {
            SurvivalResourceItem resource = col.GetComponent<SurvivalResourceItem>();
            if (resource != null && IsValuableResource(resource.resourceType))
            {
                StartCoroutine(HighlightResource(resource.gameObject));
                foundCount++;
            }
        }
        
        // ✅ MINIMAL DEBUG: Only if debug enabled
        if (showDebugInfo)
            Debug.Log($"🔍 Scanned for rare resources - found {foundCount} valuable resources");
    }
    
    #endregion
    
    #region Resource Gathering
    
    void StartGathering(SurvivalResourceItem target)
    {
        if (isGathering) return;
        
        isGathering = true;
        currentTarget = target;
        gatherCoroutine = StartCoroutine(GatheringCoroutine(target));
        
        // ✅ REMOVED: Debug log for gathering start
    }
    
    IEnumerator GatheringCoroutine(SurvivalResourceItem target)
    {
        float timer = 0f;
        float gatherTime = currentTool.gatherTime;
        
        while (timer < gatherTime && target != null && isGathering)
        {
            float distance = Vector2.Distance(transform.position, target.transform.position);
            if (distance > currentTool.attackRange)
            {
                break;
            }
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (target != null && isGathering)
        {
            CompleteGathering(target);
        }
        
        StopGathering();
    }
    
    void CompleteGathering(SurvivalResourceItem target)
    {
        int yield = currentTool.resourcesPerHit;
        string resourceType = currentTool.primaryResourceType;
        
        bool bonusYield = Random.Range(0f, 1f) < currentTool.bonusYieldChance;
        if (bonusYield)
        {
            yield = Mathf.RoundToInt(yield * currentTool.bonusYieldMultiplier);
            // ✅ REMOVED: Bonus yield debug log
        }
        
        if (inventory != null)
        {
            inventory.AddItem(resourceType, yield);
        }
        
        target.OnHarvested();
        
        // ✅ MINIMAL DEBUG: Only if debug enabled
        if (showDebugInfo)
            Debug.Log($"{GetToolIcon()} Gathered {yield} {resourceType}");
    }
    
    void StopGathering()
    {
        if (gatherCoroutine != null)
        {
            StopCoroutine(gatherCoroutine);
            gatherCoroutine = null;
        }
        
        isGathering = false;
        currentTarget = null;
    }
    
    #endregion
    
    #region Finding Methods
    
    BuildingComponent FindNearestBuilding(Vector3 position)
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(position, 0.5f);
        
        BuildingComponent closestBuilding = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider2D col in nearbyColliders)
        {
            BuildingComponent building = col.GetComponent<BuildingComponent>();
            if (building != null && !building.IsDestroyed())
            {
                SpriteRenderer sr = col.GetComponent<SpriteRenderer>();
                if (sr != null && sr.color.a < 1f) continue;
                
                float distance = Vector2.Distance(position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBuilding = building;
                }
            }
        }
        
        return closestBuilding;
    }
    
    SurvivalResourceItem FindNearestResource(Vector3 position, string resourceCategory)
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(position, currentTool.attackRange);
        
        SurvivalResourceItem closestResource = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider2D col in nearbyColliders)
        {
            SurvivalResourceItem resource = col.GetComponent<SurvivalResourceItem>();
            if (resource != null && resource.IsResourceType(resourceCategory))
            {
                float distance = Vector2.Distance(position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestResource = resource;
                }
            }
        }
        
        return closestResource;
    }
    
    #endregion
    
    #region Visual Effects
    
    void ShowRangeIndicator()
    {
        if (currentTool.toolType != SurvivalToolType.Hammer) return;
        
        Vector3 mousePos = GetMouseWorldPosition();
        float distanceToClick = Vector3.Distance(transform.position, mousePos);
        
        Color lineColor = distanceToClick <= maxDestructionRange ? inRangeColor : outOfRangeColor;
        Debug.DrawLine(transform.position, mousePos, lineColor, 0.1f);
        
        // ✅ REMOVED: Range check debug logs
    }
    
    void ShowOutOfRangeEffect(Vector3 position)
    {
        GameObject effect = new GameObject("OutOfRangeEffect");
        effect.transform.position = position;
        
        SpriteRenderer sr = effect.AddComponent<SpriteRenderer>();
        sr.color = outOfRangeColor;
        sr.sortingOrder = 10;
        
        Destroy(effect, 0.5f);
        
        // ✅ REMOVED: Out of range debug log
    }
    
    #endregion
    
    #region Utility Methods
    
    bool IsValuableResource(string resourceType)
    {
        string lowerType = resourceType.ToLower();
        return lowerType.Contains("gold") || lowerType.Contains("gem") || 
               lowerType.Contains("crystal") || lowerType.Contains("precious");
    }
    
    Vector3 GetMouseWorldPosition()
    {
        if (mainCamera != null)
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            return mousePos;
        }
        return Vector3.zero;
    }
    
    string GetToolIcon()
    {
        if (currentTool == null) return "🔧";
        
        switch (currentTool.toolType)
        {
            case SurvivalToolType.Hammer: return "🔨";
            case SurvivalToolType.Axe: return "🪓";
            case SurvivalToolType.Pickaxe: return "⛏️";
            default: return "🔧";
        }
    }
    
    #endregion
    
    #region Build Mode Protection
    
    void TrackBuildMode()
    {
        bool isCurrentlyInBuildMode = IsInBuildMode();
        
        if (wasBuildModeActive && !isCurrentlyInBuildMode)
        {
            lastBuildModeTime = Time.time;
            // ✅ REMOVED: Build mode debug log
        }
        wasBuildModeActive = isCurrentlyInBuildMode;
    }
    
    bool ShouldBlockDestruction()
    {
        if (!currentTool.respectBuildMode) return false;
        
        if (IsInBuildMode())
        {
            return true;
        }
        
        if (Time.time - lastBuildModeTime < currentTool.buildModeProtectionDelay)
        {
            return true;
        }
        
        return false;
    }
    
    bool IsInBuildMode()
    {
        if (buildingManager == null) return false;
        
        var field = buildingManager.GetType().GetField("isBuildingMode", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return (bool)field.GetValue(buildingManager);
        }
        
        return false;
    }
    
    #endregion
    
    #region Effects and Audio
    
    void PlayToolEffects(Vector3 position, bool isSpecial = false)
    {
        GameObject effectToPlay = null;
        
        if (isSpecial && currentTool.criticalHitEffect != null)
        {
            effectToPlay = currentTool.criticalHitEffect;
        }
        else if (currentTool.hitEffect != null)
        {
            effectToPlay = currentTool.hitEffect;
        }
        
        if (effectToPlay != null)
        {
            Instantiate(effectToPlay, position, Quaternion.identity);
        }
        
        AudioClip soundToPlay = currentTool.useSound;
        if (soundToPlay != null)
        {
            AudioSource.PlayClipAtPoint(soundToPlay, position);
        }
    }
    
    IEnumerator HighlightResource(GameObject resource)
    {
        SpriteRenderer sr = resource.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color originalColor = sr.color;
            
            for (float t = 0; t < 2f; t += Time.deltaTime)
            {
                float glow = Mathf.Sin(t * Mathf.PI * 4f) * 0.5f + 0.5f;
                sr.color = Color.Lerp(originalColor, Color.yellow, glow * 0.5f);
                yield return null;
            }
            
            sr.color = originalColor;
        }
    }

    #endregion

    #region UI Display System

    void OnGUI()
    {
        // ✅ REMOVED: Debug OnGUI logging
        
        if (!showToolUI) return;

        // Draw UI elements in order
        DrawImprovedToolSlots();
        DrawImprovedToolStatus();
        DrawImprovedRangeInfo();
        
        // Check for UI conflicts
        if (HasCombatUIConflict())
        {
            DrawConflictWarning();
        }
        
        // ✅ REMOVED: Debug test box
    }
    
    bool HasCombatUIConflict()
    {
        GameObject combatUI = GameObject.Find("CombatManager");
        if (combatUI != null)
        {
            var combatManager = combatUI.GetComponent<MonoBehaviour>();
            if (combatManager != null)
            {
                var showUIField = combatManager.GetType().GetField("showCombatUI", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (showUIField != null)
                {
                    bool showCombatUI = (bool)showUIField.GetValue(combatManager);
                    return showCombatUI;
                }
            }
        }
        
        return false;
    }
    
    void DrawConflictWarning()
    {
        float warningWidth = 300f;
        float warningHeight = 50f;
        float startX = Screen.width - warningWidth - 20f;
        float startY = 150f;
        
        Rect warningRect = new Rect(startX, startY, warningWidth, warningHeight);
        
        GUI.color = new Color(1f, 0f, 0f, 0.8f);
        GUI.Box(warningRect, "");
        
        GUI.color = Color.white;
        GUIStyle warningStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.yellow }
        };
        
        GUI.Label(warningRect, "⚠️ UI CONFLICT DETECTED\nAdjust Combat UI position!", warningStyle);
        GUI.color = Color.white;
    }
    
    void DrawImprovedToolStatus()
    {
        if (!showToolInfo || currentTool == null) return;
        
        float startX = 20f;
        float startY = Screen.height - 350f;
        
        if (HasCombatUIConflict())
        {
            startX = Screen.width - 320f;
            startY = 200f;
        }
        
        float panelWidth = 280f;
        float panelHeight = 140f;
        
        Rect panelRect = new Rect(startX, startY, panelWidth, panelHeight);
        
        // Background
        GUI.color = new Color(0f, 0f, 0f, 0.9f);
        GUI.Box(panelRect, "");
        
        // Border
        GUI.color = HasCombatUIConflict() ? Color.red : Color.green;
        GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(panelRect.x, panelRect.y + panelRect.height - 2, panelRect.width, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, 2, panelRect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(panelRect.x + panelRect.width - 2, panelRect.y, 2, panelRect.height), Texture2D.whiteTexture);
        
        GUI.color = Color.white;
        
        DrawToolStatusContent(startX, startY, panelWidth, panelHeight);
    }
    
    void DrawToolStatusContent(float startX, float startY, float panelWidth, float panelHeight)
    {
        float contentX = startX + 10f;
        float contentY = startY + 8f;
        float contentWidth = panelWidth - 20f;
        
        // Tool name
        string toolIcon = GetToolIcon();
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        
        GUI.Label(new Rect(contentX, contentY, contentWidth, 22), 
                 $"{toolIcon} Current: {currentTool.toolName}", titleStyle);
        
        // Durability
        float durability = GetCurrentToolDurability();
        float durabilityPercent = durability / currentTool.maxDurability;
        
        GUIStyle durabilityStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.white }
        };
        
        GUI.Label(new Rect(contentX, contentY + 25, contentWidth, 18), 
                 $"Durability: {durability:F0}/{currentTool.maxDurability}", durabilityStyle);
        
        // Durability bar
        DrawDurabilityBar(contentX, contentY + 45, contentWidth - 80, durabilityPercent);
        
        // Status
        DrawToolStatusText(contentX + contentWidth - 70, contentY + 45);
        
        // Range info
        DrawRangeText(contentX, contentY + 68, contentWidth);
        
        // Additional info
        if (currentTool.toolType == SurvivalToolType.Hammer)
        {
            DrawHammerInfo(contentX, contentY + 88, contentWidth);
        }
        else
        {
            DrawGatheringInfo(contentX, contentY + 88, contentWidth);
        }
    }
    
    void DrawImprovedRangeInfo()
    {
        if (currentTool == null || currentTool.toolType != SurvivalToolType.Hammer) return;
        
        Vector3 mousePos = GetMouseWorldPosition();
        float distance = Vector3.Distance(transform.position, mousePos);
        bool inRange = distance <= maxDestructionRange;
        
        float panelWidth = 220f;
        float panelHeight = 25f;
        float startX = (Screen.width - panelWidth) / 2f;
        float startY = 30f;
        
        Rect rangeRect = new Rect(startX, startY, panelWidth, panelHeight);
        
        Color bgColor = inRange ? new Color(0f, 0.8f, 0f, 0.8f) : new Color(0.8f, 0f, 0f, 0.8f);
        
        GUI.color = bgColor;
        GUI.Box(rangeRect, "");
        
        string rangeText = inRange ? "✅ IN RANGE" : "❌ TOO FAR";
        string fullText = $"🔨 {rangeText} ({distance:F1}m/{maxDestructionRange}m)";
        
        GUIStyle compactStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        
        GUI.color = Color.white;
        GUI.Label(new Rect(rangeRect.x + 5, rangeRect.y + 4, rangeRect.width - 10, rangeRect.height - 8), 
                 fullText, compactStyle);
        
        GUI.color = Color.white;
    }
    
    void DrawImprovedToolSlots()
    {
        float startX = 20f;
        float startY = Screen.height - 140f;
        float slotSize = 70f;
        float spacing = 8f;
        
        Rect slotsPanel = new Rect(startX - 8, startY - 8, (slotSize + spacing) * 3 + 8, slotSize + 45);
        
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.Box(slotsPanel, "");
        GUI.color = Color.white;
        
        for (int i = 0; i < 3; i++)
        {
            Rect slotRect = new Rect(startX + i * (slotSize + spacing), startY, slotSize, slotSize);
            
            if (i == currentToolIndex)
            {
                GUI.color = new Color(1f, 1f, 0f, 0.4f);
                Rect glowRect = new Rect(slotRect.x - 4, slotRect.y - 4, slotRect.width + 8, slotRect.height + 8);
                GUI.Box(glowRect, "");
                
                GUI.color = new Color(1f, 1f, 0f, 0.8f);
            }
            else
            {
                GUI.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            }
            
            GUI.Box(slotRect, "");
            GUI.color = Color.white;
            
            if (i < availableTools.Count && availableTools[i] != null)
            {
                string toolIcon = "";
                switch (availableTools[i].toolType)
                {
                    case SurvivalToolType.Hammer: toolIcon = "🔨"; break;
                    case SurvivalToolType.Axe: toolIcon = "🪓"; break;
                    case SurvivalToolType.Pickaxe: toolIcon = "⛏️"; break;
                    default: toolIcon = "🔧"; break;
                }
                
                GUIStyle iconStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 28,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                
                GUI.Label(slotRect, toolIcon, iconStyle);
                
                // Durability bar
                if (toolDurabilities.ContainsKey(availableTools[i]))
                {
                    float currentDurability = toolDurabilities[availableTools[i]];
                    float maxDurability = availableTools[i].maxDurability;
                    float toolDurabilityPercent = currentDurability / maxDurability;
                    
                    Rect miniBarRect = new Rect(slotRect.x + 4, slotRect.y + slotSize - 12, slotSize - 8, 8);
                    
                    GUI.color = Color.black;
                    GUI.DrawTexture(miniBarRect, Texture2D.whiteTexture);
                    
                    Color durabilityColor = toolDurabilityPercent >= 0.7f ? Color.green :
                                           toolDurabilityPercent >= 0.3f ? Color.yellow : Color.red;
                    
                    GUI.color = durabilityColor;
                    Rect fillRect = new Rect(miniBarRect.x + 1, miniBarRect.y + 1, 
                                           (miniBarRect.width - 2) * toolDurabilityPercent, miniBarRect.height - 2);
                    GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
                    
                    GUI.color = Color.black;
                    GUIStyle durabilityTextStyle = new GUIStyle()
                    {
                        fontSize = 9,
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = Color.black }
                    };
                    
                    GUI.Label(new Rect(miniBarRect.x + 1, miniBarRect.y + 1, miniBarRect.width, miniBarRect.height), 
                             $"{currentDurability:F0}", durabilityTextStyle);
                    
                    GUI.color = Color.white;
                    durabilityTextStyle.normal.textColor = Color.white;
                    GUI.Label(miniBarRect, $"{currentDurability:F0}", durabilityTextStyle);
                }
            }
            else
            {
                GUIStyle emptyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.gray }
                };
                GUI.Label(slotRect, $"{i + 1}", emptyStyle);
            }
            
            // Tool name
            string toolName = "Empty";
            if (i < availableTools.Count && availableTools[i] != null)
            {
                toolName = availableTools[i].toolName;
            }
            
            GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = i == currentToolIndex ? Color.yellow : Color.white }
            };
            
            GUI.Label(new Rect(slotRect.x, slotRect.y + slotRect.height + 3, slotRect.width, 18), 
                     toolName, nameStyle);
        }
        
        GUI.color = Color.white;
    }
    
    void DrawDurabilityBar(float x, float y, float width, float percent)
    {
        Rect barRect = new Rect(x, y, width, 12);
        
        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        GUI.Box(barRect, "");
        
        Color durabilityColor = percent > 0.6f ? Color.green : 
                               percent > 0.3f ? Color.yellow : Color.red;
        
        GUI.color = durabilityColor;
        Rect fillRect = new Rect(barRect.x + 1, barRect.y + 1, 
                               (barRect.width - 2) * percent, barRect.height - 2);
        GUI.Box(fillRect, "");
        
        GUI.color = Color.white;
        GUIStyle percentStyle = new GUIStyle()
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUI.Label(barRect, $"{percent * 100:F0}%", percentStyle);
    }
    
    void DrawToolStatusText(float x, float y)
    {
        string statusText = "";
        
        if (IsCurrentToolBroken())
        {
            statusText = "BROKEN!";
        }
        else if (IsOnCooldown())
        {
            float cooldownRemaining = currentTool.cooldownTime - (Time.time - lastUseTime);
            statusText = $"CD: {cooldownRemaining:F1}s";
        }
        else
        {
            statusText = "Ready";
        }
        
        GUIStyle statusStyle = new GUIStyle()
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.green },
            alignment = TextAnchor.MiddleCenter
        };
        
        GUI.Label(new Rect(x, y, 65, 15), statusText, statusStyle);
    }
    
    void DrawRangeText(float x, float y, float width)
    {
        string rangeText = "";
        
        if (currentTool.toolType == SurvivalToolType.Hammer)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            float currentDistance = Vector3.Distance(transform.position, mousePos);
            rangeText = $"🔨 Max: {maxDestructionRange:F1}m | Now: {currentDistance:F1}m";
        }
        else
        {
            rangeText = $"📦 Range: {currentTool.attackRange:F1}m";
        }
        
        GUIStyle rangeStyle = new GUIStyle()
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow },
            alignment = TextAnchor.MiddleLeft
        };
        
        GUI.Label(new Rect(x, y, width, 15), rangeText, rangeStyle);
    }
    
    void DrawHammerInfo(float x, float y, float width)
    {
        string effectivenessText = $"⚡ Wood: {currentTool.woodEffectiveness}x | Stone: {currentTool.stoneEffectiveness}x | Metal: {currentTool.metalEffectiveness}x";
        
        GUIStyle effectivenessStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };
        
        GUI.Label(new Rect(x, y, width, 15), effectivenessText, effectivenessStyle);
        
        string damageText = $"💥 Damage: {currentTool.damagePerHit} | Cooldown: {currentTool.cooldownTime}s";
        
        GUIStyle damageStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            normal = { textColor = new Color(1f, 0.8f, 0.6f) }
        };
        
        GUI.Label(new Rect(x, y + 20, width, 15), damageText, damageStyle);
    }
    
    void DrawGatheringInfo(float x, float y, float width)
    {
        string gatherText = $"📦 Gather: {currentTool.gatherTime}s | Yield: {currentTool.resourcesPerHit}";
        
        GUIStyle gatherStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            normal = { textColor = new Color(0.6f, 1f, 0.8f) }
        };
        
        GUI.Label(new Rect(x, y, width, 15), gatherText, gatherStyle);
        
        string bonusText = $"🌟 Bonus: {currentTool.bonusYieldChance * 100:F0}% | x{currentTool.bonusYieldMultiplier}";
        
        GUIStyle bonusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            normal = { textColor = new Color(1f, 1f, 0.6f) }
        };
        
        GUI.Label(new Rect(x, y + 20, width, 15), bonusText, bonusStyle);
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Test Destruction Range")]
    public void TestDestructionRange()
    {
        Debug.Log($"🎯 === DESTRUCTION RANGE TEST ===");
        Debug.Log($"Max destruction range: {maxDestructionRange}m");
        Debug.Log($"Player position: {transform.position}");
        
        Vector3 mousePos = GetMouseWorldPosition();
        float distance = Vector3.Distance(transform.position, mousePos);
        
        Debug.Log($"Mouse position: {mousePos}");
        Debug.Log($"Distance to mouse: {distance:F1}m");
        Debug.Log($"Can destroy: {(distance <= maxDestructionRange ? "YES ✅" : "NO ❌")}");
        
        BuildingComponent[] nearbyBuildings = FindObjectsByType<BuildingComponent>(FindObjectsSortMode.None);
        Debug.Log($"\n🏠 Nearby buildings within {maxDestructionRange}m:");
        
        foreach (BuildingComponent building in nearbyBuildings)
        {
            if (building != null)
            {
                float buildingDistance = Vector3.Distance(transform.position, building.transform.position);
                string status = buildingDistance <= maxDestructionRange ? "✅ CAN DESTROY" : "❌ TOO FAR";
                Debug.Log($"  {building.buildingName}: {buildingDistance:F1}m - {status}");
            }
        }
    }
    
    #endregion
    
    #region Debug Visualization
    
    void OnDrawGizmosSelected()
    {
        if (currentTool == null) return;
        
        if (currentTool.toolType == SurvivalToolType.Hammer)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, maxDestructionRange);
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
            Gizmos.DrawSphere(transform.position, maxDestructionRange);
        }
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, currentTool.attackRange);
        
        if (Application.isPlaying && mainCamera != null)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            float distance = Vector3.Distance(transform.position, mousePos);
            
            Gizmos.color = distance <= maxDestructionRange ? Color.green : Color.red;
            Gizmos.DrawWireCube(mousePos, Vector3.one * 0.3f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, mousePos);
        }
    }
    
    #endregion
}