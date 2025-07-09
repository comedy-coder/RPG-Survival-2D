using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WeaponData
{
    [Header("Weapon Info")]
    public string weaponName = "Basic Weapon";
    public WeaponType weaponType = WeaponType.Melee;
    public Sprite weaponIcon;
    public GameObject weaponPrefab;
    
    [Header("Combat Stats")]
    public int baseDamage = 10;
    public float attackSpeed = 1f;
    public float range = 1.5f;
    public float criticalChance = 0.05f;
    public float criticalMultiplier = 1.5f;
    
    [Header("Special Properties")]
    public bool canBlock = false;
    public float blockChance = 0.2f;
    public float blockReduction = 0.5f;
    public bool hasPiercing = false;
    public int pierceCount = 1;
    
    [Header("Durability")]
    public int maxDurability = 100;
    public int durabilityLossPerHit = 1;
    public bool isBreakable = true;
    
    [Header("Effects")]
    public GameObject hitEffect;
    public GameObject criticalEffect;
    public AudioClip swingSound;
    public AudioClip hitSound;
    
    [Header("Requirements")]
    public int requiredLevel = 1;
    public string requiredSkill = "";
    
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(weaponName) && baseDamage > 0;
    }
}

public enum WeaponType
{
    Melee,
    Ranged,
    Magic,
    Tool
}

public class WeaponSystem : MonoBehaviour
{
    [Header("Weapon Configuration")]
    public List<WeaponData> availableWeapons = new List<WeaponData>();
    public int currentWeaponIndex = 0;
    public Transform weaponHoldPoint;
    
    [Header("Input Settings")]
    public KeyCode weaponSlot1 = KeyCode.Q;
    public KeyCode weaponSlot2 = KeyCode.E;
    public KeyCode weaponSlot3 = KeyCode.R;
    public bool enableScrollSwitching = true;
    
    [Header("Combat Settings")]
    public float combatRange = 2f;
    public LayerMask targetLayers = -1;
    public bool autoAim = true;
    public float autoAimAngle = 45f;
    
    [Header("Visual Settings")]
    public bool showWeaponUI = true;
    public bool showWeaponRange = true;
    public bool showDamagePreview = true;
    
    [Header("Audio")]
    public AudioClip weaponSwitchSound;
    public AudioClip weaponBreakSound;
    
    // Private variables
    private WeaponData currentWeapon;
    private GameObject currentWeaponObject;
    private Dictionary<WeaponData, int> weaponDurabilities = new Dictionary<WeaponData, int>();
    
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    
    // Component references
    private CombatEntity combatEntity;
    private CombatManager combatManager;
    private SimpleInventory inventory;
    private Camera mainCamera;
    
    // Events
    public static System.Action<WeaponData> OnWeaponChanged;
    public static System.Action<WeaponData> OnWeaponBroken;
    public static System.Action<WeaponData, int> OnWeaponDurabilityChanged;
    
    public WeaponData GetCurrentWeapon() => currentWeapon;
    public bool IsAttacking() => isAttacking;
    public float GetCurrentRange() => currentWeapon != null ? currentWeapon.range : combatRange;
    
    #region Initialization
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        // Get components
        combatEntity = GetComponent<CombatEntity>();
        combatManager = FindFirstObjectByType<CombatManager>();
        inventory = GetComponent<SimpleInventory>();
        mainCamera = Camera.main;
        
        // Initialize durabilities
        InitializeWeaponDurabilities();
        
        // Set default weapon
        if (availableWeapons.Count > 0)
        {
            EquipWeapon(0);
        }
        
        // Create weapon hold point if not assigned
        if (weaponHoldPoint == null)
        {
            GameObject holdPoint = new GameObject("WeaponHoldPoint");
            holdPoint.transform.SetParent(transform);
            holdPoint.transform.localPosition = Vector3.right * 0.5f;
            weaponHoldPoint = holdPoint.transform;
        }
        
        Debug.Log($"🗡️ Weapon System initialized with {availableWeapons.Count} weapons");
    }
    
    void InitializeWeaponDurabilities()
    {
        weaponDurabilities.Clear();
        
        foreach (WeaponData weapon in availableWeapons)
        {
            if (weapon != null && weapon.IsValid())
            {
                weaponDurabilities[weapon] = weapon.maxDurability;
            }
        }
    }
    
    #endregion
    
    #region Input & Update
    
    void Update()
    {
        HandleInput();
        UpdateWeaponRotation();
    }
    
    void HandleInput()
    {
        // Weapon switching
        if (Input.GetKeyDown(weaponSlot1))
        {
            EquipWeapon(0);
        }
        else if (Input.GetKeyDown(weaponSlot2))
        {
            EquipWeapon(1);
        }
        else if (Input.GetKeyDown(weaponSlot3))
        {
            EquipWeapon(2);
        }
        
        // Scroll wheel switching
        if (enableScrollSwitching)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.1f)
            {
                int direction = scroll > 0 ? 1 : -1;
                int newIndex = (currentWeaponIndex + direction + availableWeapons.Count) % availableWeapons.Count;
                EquipWeapon(newIndex);
            }
        }
        
        // Combat input
        if (Input.GetMouseButtonDown(0))
        {
            TryAttack();
        }
        
        // Block input (if weapon supports it)
        if (currentWeapon != null && currentWeapon.canBlock)
        {
            if (Input.GetMouseButtonDown(1))
            {
                StartBlocking();
            }
            else if (Input.GetMouseButtonUp(1))
            {
                StopBlocking();
            }
        }
    }
    
    void UpdateWeaponRotation()
    {
        if (currentWeaponObject == null || mainCamera == null) return;
        
        // Rotate weapon to face mouse
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        
        Vector3 direction = (mousePos - weaponHoldPoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        currentWeaponObject.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        // Flip weapon sprite if needed
        SpriteRenderer weaponRenderer = currentWeaponObject.GetComponent<SpriteRenderer>();
        if (weaponRenderer != null)
        {
            weaponRenderer.flipY = direction.x < 0;
        }
    }
    
    #endregion
    
    #region Weapon Management
    
    public void EquipWeapon(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= availableWeapons.Count) return;
        if (availableWeapons[weaponIndex] == null) return;
        
        WeaponData newWeapon = availableWeapons[weaponIndex];
        
        // Check if weapon is broken
        if (IsWeaponBroken(newWeapon))
        {
            Debug.Log($"❌ Cannot equip {newWeapon.weaponName} - weapon is broken!");
            return;
        }
        
        // Destroy current weapon object
        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
        }
        
        // Set new weapon
        currentWeaponIndex = weaponIndex;
        currentWeapon = newWeapon;
        
        // Create weapon object
        CreateWeaponObject();
        
        // Update combat stats
        UpdateCombatStats();
        
        // Play sound
        if (weaponSwitchSound != null)
        {
            AudioSource.PlayClipAtPoint(weaponSwitchSound, transform.position);
        }
        
        // Fire event
        OnWeaponChanged?.Invoke(currentWeapon);
        
        Debug.Log($"🗡️ Equipped: {currentWeapon.weaponName}");
    }
    
    void CreateWeaponObject()
    {
        if (currentWeapon == null || weaponHoldPoint == null) return;
        
        if (currentWeapon.weaponPrefab != null)
        {
            currentWeaponObject = Instantiate(currentWeapon.weaponPrefab, weaponHoldPoint);
        }
        else
        {
            // Create simple weapon object
            currentWeaponObject = new GameObject($"Weapon_{currentWeapon.weaponName}");
            currentWeaponObject.transform.SetParent(weaponHoldPoint);
            currentWeaponObject.transform.localPosition = Vector3.zero;
            
            // Add sprite renderer
            SpriteRenderer sr = currentWeaponObject.AddComponent<SpriteRenderer>();
            if (currentWeapon.weaponIcon != null)
            {
                sr.sprite = currentWeapon.weaponIcon;
            }
            sr.sortingOrder = 1;
        }
    }
    
    void UpdateCombatStats()
    {
        if (combatEntity == null || currentWeapon == null) return;
        
        // Update combat entity stats based on weapon
        combatEntity.baseDamage = currentWeapon.baseDamage;
        combatEntity.attackSpeed = currentWeapon.attackSpeed;
        combatEntity.criticalChance = currentWeapon.criticalChance;
        combatEntity.criticalMultiplier = currentWeapon.criticalMultiplier;
        combatEntity.attackRange = currentWeapon.range;
        
        if (currentWeapon.canBlock)
        {
            combatEntity.blockChance = currentWeapon.blockChance;
            combatEntity.blockDamageReduction = currentWeapon.blockReduction;
        }
    }
    
    public bool IsWeaponBroken(WeaponData weapon)
    {
        if (weapon == null || !weapon.isBreakable) return false;
        
        return weaponDurabilities.ContainsKey(weapon) && weaponDurabilities[weapon] <= 0;
    }
    
    public int GetWeaponDurability(WeaponData weapon)
    {
        if (weapon == null) return 0;
        return weaponDurabilities.ContainsKey(weapon) ? weaponDurabilities[weapon] : weapon.maxDurability;
    }
    
    public float GetWeaponDurabilityPercentage(WeaponData weapon)
    {
        if (weapon == null) return 0f;
        return (float)GetWeaponDurability(weapon) / weapon.maxDurability;
    }
    
    #endregion
    
    #region Combat Actions
    
    public void TryAttack()
    {
        if (!CanAttack()) return;
        
        Vector3 mousePos = GetMouseWorldPosition();
        
        // Find targets
        List<CombatEntity> targets = FindTargetsInRange(mousePos);
        
        if (targets.Count > 0)
        {
            PerformAttack(targets);
        }
        else
        {
            // Swing in air
            PerformSwing();
        }
    }
    
    bool CanAttack()
    {
        if (currentWeapon == null) return false;
        if (isAttacking) return false;
        if (IsWeaponBroken(currentWeapon)) return false;
        
        float cooldown = 1f / currentWeapon.attackSpeed;
        return Time.time - lastAttackTime >= cooldown;
    }
    
    void PerformAttack(List<CombatEntity> targets)
    {
        if (currentWeapon == null || targets.Count == 0) return;
        
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // Attack each target
        int targetsHit = 0;
        int maxTargets = currentWeapon.hasPiercing ? currentWeapon.pierceCount : 1;
        
        foreach (CombatEntity target in targets)
        {
            if (targetsHit >= maxTargets) break;
            
            if (target != null && !target.IsDead())
            {
                AttackTarget(target);
                targetsHit++;
            }
        }
        
        // Consume durability
        ConsumeDurability();
        
        // Reset attack state
        StartCoroutine(ResetAttackState());
    }
    
    void AttackTarget(CombatEntity target)
    {
        if (combatManager != null)
        {
            combatManager.PerformAttack(combatEntity, target);
        }
        
        // Play effects
        PlayWeaponEffects(target.transform.position);
    }
    
    void PerformSwing()
    {
        if (currentWeapon == null) return;
        
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // Play swing sound
        if (currentWeapon.swingSound != null)
        {
            AudioSource.PlayClipAtPoint(currentWeapon.swingSound, transform.position);
        }
        
        // Minor durability loss for missing
        if (currentWeapon.isBreakable)
        {
            int minorLoss = Mathf.Max(1, currentWeapon.durabilityLossPerHit / 2);
            ConsumeDurability(minorLoss);
        }
        
        StartCoroutine(ResetAttackState());
        
        Debug.Log($"🗡️ {currentWeapon.weaponName} swung but missed");
    }
    
    System.Collections.IEnumerator ResetAttackState()
    {
        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }
    
    void ConsumeDurability(int customLoss = -1)
    {
        if (currentWeapon == null || !currentWeapon.isBreakable) return;
        
        int durabilityLoss = customLoss >= 0 ? customLoss : currentWeapon.durabilityLossPerHit;
        
        int currentDurability = GetWeaponDurability(currentWeapon);
        int newDurability = Mathf.Max(0, currentDurability - durabilityLoss);
        
        weaponDurabilities[currentWeapon] = newDurability;
        
        // Fire event
        OnWeaponDurabilityChanged?.Invoke(currentWeapon, newDurability - currentDurability);
        
        // Check if weapon broke
        if (newDurability <= 0)
        {
            BreakWeapon(currentWeapon);
        }
        
        Debug.Log($"🔧 {currentWeapon.weaponName} durability: {newDurability}/{currentWeapon.maxDurability}");
    }
    
    void BreakWeapon(WeaponData weapon)
    {
        if (weapon == null) return;
        
        // Play break sound
        if (weaponBreakSound != null)
        {
            AudioSource.PlayClipAtPoint(weaponBreakSound, transform.position);
        }
        
        // Visual effect
        if (currentWeaponObject != null)
        {
            StartCoroutine(WeaponBreakEffect());
        }
        
        // Fire event
        OnWeaponBroken?.Invoke(weapon);
        
        // Try to equip another weapon
        TryEquipWorkingWeapon();
        
        Debug.Log($"💥 {weapon.weaponName} broke!");
    }
    
    System.Collections.IEnumerator WeaponBreakEffect()
    {
        SpriteRenderer sr = currentWeaponObject?.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Flash red and fade out
            for (int i = 0; i < 5; i++)
            {
                sr.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                sr.color = Color.white;
                yield return new WaitForSeconds(0.1f);
            }
            
            // Fade out
            float alpha = 1f;
            while (alpha > 0)
            {
                alpha -= Time.deltaTime * 2f;
                sr.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
        }
    }
    
    void TryEquipWorkingWeapon()
    {
        for (int i = 0; i < availableWeapons.Count; i++)
        {
            if (availableWeapons[i] != null && !IsWeaponBroken(availableWeapons[i]))
            {
                EquipWeapon(i);
                return;
            }
        }
        
        Debug.Log("❌ No working weapons available!");
    }
    
    #endregion
    
    #region Blocking System
    
    void StartBlocking()
    {
        if (currentWeapon == null || !currentWeapon.canBlock) return;
        
        if (combatEntity != null)
        {
            combatEntity.SetBlocking(true);
        }
        
        Debug.Log($"🛡️ Blocking with {currentWeapon.weaponName}");
    }
    
    void StopBlocking()
    {
        if (combatEntity != null)
        {
            combatEntity.SetBlocking(false);
        }
        
        Debug.Log($"🛡️ Stopped blocking");
    }
    
    #endregion
    
    #region Target Finding
    
    List<CombatEntity> FindTargetsInRange(Vector3 attackPosition)
    {
        List<CombatEntity> targets = new List<CombatEntity>();
        
        if (currentWeapon == null) return targets;
        
        // Get all colliders in range
        Collider2D[] colliders = Physics2D.OverlapCircleAll(attackPosition, currentWeapon.range * 0.5f, targetLayers);
        
        foreach (Collider2D col in colliders)
        {
            CombatEntity target = col.GetComponent<CombatEntity>();
            if (target != null && target != combatEntity && !target.IsDead())
            {
                // Check if target is in attack arc
                if (autoAim && IsTargetInAttackArc(target.transform.position))
                {
                    targets.Add(target);
                }
                else if (!autoAim)
                {
                    targets.Add(target);
                }
            }
        }
        
        // Sort by distance
        targets.Sort((a, b) => 
        {
            float distA = Vector3.Distance(transform.position, a.transform.position);
            float distB = Vector3.Distance(transform.position, b.transform.position);
            return distA.CompareTo(distB);
        });
        
        return targets;
    }
    
    bool IsTargetInAttackArc(Vector3 targetPosition)
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Vector3 attackDirection = (mousePos - transform.position).normalized;
        Vector3 targetDirection = (targetPosition - transform.position).normalized;
        
        float angle = Vector3.Angle(attackDirection, targetDirection);
        return angle <= autoAimAngle;
    }
    
    #endregion
    
    #region Effects & Audio
    
    void PlayWeaponEffects(Vector3 position)
    {
        if (currentWeapon == null) return;
        
        // Visual effects
        if (currentWeapon.hitEffect != null)
        {
            GameObject effect = Instantiate(currentWeapon.hitEffect, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Audio effects
        if (currentWeapon.hitSound != null)
        {
            AudioSource.PlayClipAtPoint(currentWeapon.hitSound, position);
        }
    }
    
    #endregion
    
    #region Utility Methods
    
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
    
    public string GetCurrentWeaponName()
    {
        return currentWeapon != null ? currentWeapon.weaponName : "None";
    }
    
    public int GetCurrentWeaponDamage()
    {
        return currentWeapon != null ? currentWeapon.baseDamage : 0;
    }
    
    public float GetCurrentWeaponSpeed()
    {
        return currentWeapon != null ? currentWeapon.attackSpeed : 0f;
    }
    
    #endregion
    
    #region Repair System
    
    public bool CanRepairWeapon(WeaponData weapon)
    {
        if (weapon == null || !weapon.isBreakable) return false;
        
        int currentDurability = GetWeaponDurability(weapon);
        return currentDurability < weapon.maxDurability;
    }
    
    public void RepairWeapon(WeaponData weapon, int repairAmount)
    {
        if (weapon == null || !CanRepairWeapon(weapon)) return;
        
        int currentDurability = GetWeaponDurability(weapon);
        int newDurability = Mathf.Min(weapon.maxDurability, currentDurability + repairAmount);
        
        weaponDurabilities[weapon] = newDurability;
        
        // Fire event
        OnWeaponDurabilityChanged?.Invoke(weapon, newDurability - currentDurability);
        
        Debug.Log($"🔧 Repaired {weapon.weaponName} by {repairAmount} points");
    }
    
    public void RepairWeaponToFull(WeaponData weapon)
    {
        if (weapon == null) return;
        
        RepairWeapon(weapon, weapon.maxDurability);
    }
    
    public void RepairAllWeapons()
    {
        foreach (WeaponData weapon in availableWeapons)
        {
            if (weapon != null && CanRepairWeapon(weapon))
            {
                RepairWeaponToFull(weapon);
            }
        }
        
        Debug.Log("🔧 All weapons repaired!");
    }
    
    #endregion
    
    #region UI Display
    
    void OnGUI()
    {
        if (!showWeaponUI) return;
        
        DrawWeaponSlots();
        DrawWeaponInfo();
        DrawAttackRange();
    }
    
    void DrawWeaponSlots()
    {
        float startX = Screen.width - 220f;
        float startY = 20f;
        float slotSize = 60f;
        
        // Background
        //GUI.Box(new Rect(startX - 10, startY - 10, 200, slotSize + 20), "Weapons");
        
        // Weapon slots
        for (int i = 0; i < Mathf.Min(3, availableWeapons.Count); i++)
        {
            Rect slotRect = new Rect(startX + i * (slotSize + 5), startY, slotSize, slotSize);
            
            WeaponData weapon = availableWeapons[i];
            
            // Highlight current weapon
            if (i == currentWeaponIndex)
            {
                GUI.backgroundColor = Color.yellow;
            }
            else if (weapon != null && IsWeaponBroken(weapon))
            {
                GUI.backgroundColor = Color.red;
            }
            else
            {
                GUI.backgroundColor = Color.white;
            }
            
            // Draw weapon slot
            if (weapon != null && weapon.weaponIcon != null)
            {
                GUI.DrawTexture(slotRect, weapon.weaponIcon.texture);
            }
            else
            {
                GUI.Box(slotRect, (i + 1).ToString());
            }
            
            // Durability bar
            if (weapon != null && weapon.isBreakable)
            {
                float durabilityPercent = GetWeaponDurabilityPercentage(weapon);
                Rect durabilityRect = new Rect(slotRect.x, slotRect.y + slotRect.height - 5, slotRect.width * durabilityPercent, 5);
                
                Color durabilityColor = Color.Lerp(Color.red, Color.green, durabilityPercent);
                GUI.color = durabilityColor;
                GUI.DrawTexture(durabilityRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }
        
        GUI.backgroundColor = Color.white;
    }
    
   // 🔧 THAY THẾ METHOD DrawWeaponInfo() TRONG WeaponSystem.cs

void DrawWeaponInfo()
{
    if (currentWeapon == null) return;
    
    // 🎯 VỊ TRÍ MỚI - TRÁNH CONFLICT VỚI UI KHÁC
    float startX = Screen.width - 250f;  // Xa hơn từ cạnh phải
    float startY = 100f;                  // Sát trên cùng
    float panelWidth = 230f;
    float panelHeight = 140f;
    
    // Background panel
    Rect panelRect = new Rect(startX, startY, panelWidth, panelHeight);
    GUI.color = new Color(0f, 0f, 0f, 0.8f);
    GUI.Box(panelRect, "");
    
    // Border
    GUI.color = new Color(0f, 0.8f, 1f, 0.8f); // Cyan border
    GUI.Box(new Rect(panelRect.x - 2, panelRect.y - 2, panelRect.width + 4, panelRect.height + 4), "");
    
    GUI.color = Color.white;
    
    // Content
    float contentX = startX + 10f;
    float contentY = startY + 10f;
    
    // Title
    GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
    {
        fontSize = 14,
        fontStyle = FontStyle.Bold,
        normal = { textColor = Color.cyan }
    };
    
    GUI.Label(new Rect(contentX, contentY, panelWidth - 20, 20), "⚔️ WEAPON STATUS", titleStyle);
    
    // Weapon info
    GUI.Label(new Rect(contentX, contentY + 25, panelWidth - 20, 20), $"Weapon: {currentWeapon.weaponName}");
    GUI.Label(new Rect(contentX, contentY + 45, panelWidth - 20, 20), $"Damage: {currentWeapon.baseDamage}");
    GUI.Label(new Rect(contentX, contentY + 65, panelWidth - 20, 20), $"Speed: {currentWeapon.attackSpeed:F1}");
    GUI.Label(new Rect(contentX, contentY + 85, panelWidth - 20, 20), $"Range: {currentWeapon.range:F1}m");
    
    // Durability
    if (currentWeapon.isBreakable)
    {
        int durability = GetWeaponDurability(currentWeapon);
        GUI.Label(new Rect(contentX, contentY + 105, panelWidth - 20, 20), $"Durability: {durability}/{currentWeapon.maxDurability}");
    }
    
    // Status
    if (IsWeaponBroken(currentWeapon))
    {
        GUI.color = Color.red;
        GUI.Label(new Rect(contentX, contentY + 125, panelWidth - 20, 20), "BROKEN!");
        GUI.color = Color.white;
    }
    else if (isAttacking)
    {
        GUI.color = Color.yellow;
        GUI.Label(new Rect(contentX, contentY + 125, panelWidth - 20, 20), "Attacking...");
        GUI.color = Color.white;
    }
    else
    {
        GUI.color = Color.green;
        GUI.Label(new Rect(contentX, contentY + 125, panelWidth - 20, 20), "Ready");
        GUI.color = Color.white;
    }
}
    
    void DrawAttackRange()
    {
        if (!showWeaponRange || currentWeapon == null) return;
        
        // This would be better implemented with Gizmos, but showing concept
        GUI.color = new Color(1f, 0f, 0f, 0.3f);
        GUI.Label(new Rect(10, Screen.height - 50, 200, 20), $"Attack Range: {currentWeapon.range:F1}m");
        GUI.color = Color.white;
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Debug Weapon Info")]
    public void DebugWeaponInfo()
    {
        Debug.Log($"🗡️ === WEAPON SYSTEM DEBUG ===");
        Debug.Log($"Current weapon: {GetCurrentWeaponName()}");
        Debug.Log($"Weapon count: {availableWeapons.Count}");
        
        if (currentWeapon != null)
        {
            Debug.Log($"Damage: {currentWeapon.baseDamage}");
            Debug.Log($"Speed: {currentWeapon.attackSpeed}");
            Debug.Log($"Range: {currentWeapon.range}");
            Debug.Log($"Durability: {GetWeaponDurability(currentWeapon)}/{currentWeapon.maxDurability}");
            Debug.Log($"Is broken: {IsWeaponBroken(currentWeapon)}");
        }
        
        Debug.Log($"Can attack: {CanAttack()}");
        Debug.Log($"Is attacking: {isAttacking}");
    }
    
    [ContextMenu("Break Current Weapon")]
    public void DebugBreakWeapon()
    {
        if (currentWeapon != null)
        {
            weaponDurabilities[currentWeapon] = 0;
            BreakWeapon(currentWeapon);
        }
    }
    
    [ContextMenu("Repair All Weapons")]
    public void DebugRepairAllWeapons()
    {
        RepairAllWeapons();
    }
    
    [ContextMenu("Spawn Test Weapons")]
    public void SpawnTestWeapons()
    {
        // Create test weapons if list is empty
        if (availableWeapons.Count == 0)
        {
            // Sword
            WeaponData sword = new WeaponData();
            sword.weaponName = "Iron Sword";
            sword.weaponType = WeaponType.Melee;
            sword.baseDamage = 15;
            sword.attackSpeed = 1.2f;
            sword.range = 1.5f;
            sword.maxDurability = 100;
            availableWeapons.Add(sword);
            
            // Axe
            WeaponData axe = new WeaponData();
            axe.weaponName = "Battle Axe";
            axe.weaponType = WeaponType.Melee;
            axe.baseDamage = 20;
            axe.attackSpeed = 0.8f;
            axe.range = 1.2f;
            axe.maxDurability = 80;
            availableWeapons.Add(axe);
            
            // Dagger
            WeaponData dagger = new WeaponData();
            dagger.weaponName = "Steel Dagger";
            dagger.weaponType = WeaponType.Melee;
            dagger.baseDamage = 8;
            dagger.attackSpeed = 2f;
            dagger.range = 1f;
            dagger.criticalChance = 0.15f;
            dagger.maxDurability = 60;
            availableWeapons.Add(dagger);
            
            InitializeWeaponDurabilities();
            EquipWeapon(0);
            
            Debug.Log("🗡️ Test weapons created!");
        }
    }
    
    #endregion
    
    #region Gizmos
    
    void OnDrawGizmosSelected()
    {
        if (currentWeapon == null) return;
        
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, currentWeapon.range);
        
        // Auto-aim arc
        if (autoAim && mainCamera != null)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            Vector3 attackDirection = (mousePos - transform.position).normalized;
            
            // Draw attack arc
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            float arcRadius = currentWeapon.range;
            
            Vector3 arcStart = Quaternion.AngleAxis(-autoAimAngle, Vector3.forward) * attackDirection * arcRadius;
            Vector3 arcEnd = Quaternion.AngleAxis(autoAimAngle, Vector3.forward) * attackDirection * arcRadius;
            
            Gizmos.DrawLine(transform.position, transform.position + arcStart);
            Gizmos.DrawLine(transform.position, transform.position + arcEnd);
        }
        
        // Weapon hold point
        if (weaponHoldPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(weaponHoldPoint.position, 0.1f);
        }
    }
    
    #endregion
}