using UnityEngine;

[CreateAssetMenu(fileName = "New Survival Tool", menuName = "RPG Survival/Survival Tool Data", order = 1)]
public class SurvivalToolData : ScriptableObject
{
    [Header("Basic Info")]
    public string toolName = "Basic Tool";
    [TextArea(2, 4)]
    public string toolDescription = "A basic survival tool";
    public Sprite toolIcon;
    
    [Header("Tool Type")]
    [Tooltip("Tool types: Hammer, Axe, Pickaxe")]
    public SurvivalToolType toolType = SurvivalToolType.Hammer;
    
    [Header("Tool Stats")]
    public int damagePerHit = 25;
    public float attackRange = 2f;
    public float cooldownTime = 0.5f;
    public float maxDurability = 100f;
    public float durabilityLossPerUse = 1f;
    
    [Header("Material Effectiveness")]
    [Range(0f, 2f)]
    public float woodEffectiveness = 1f;
    [Range(0f, 2f)]
    public float stoneEffectiveness = 0.7f;
    [Range(0f, 2f)]
    public float metalEffectiveness = 0.3f;
    
    [Header("Resource Gathering")]
    public int resourcesPerHit = 2;
    public float gatherTime = 1f;
    public string primaryResourceType = "Wood";
    [Range(0f, 1f)]
    public float bonusYieldChance = 0.15f;
    [Range(1f, 3f)]
    public float bonusYieldMultiplier = 1.5f;
    
    [Header("Special Abilities")]
    public bool canFindRareResources = false;
    [Range(0f, 1f)]
    public float rareResourceChance = 0.05f;
    public bool canRepairBuildings = false;
    public int repairCostWood = 1;
    
    [Header("Audio")]
    public AudioClip useSound;
    public AudioClip hitSound;
    public AudioClip gatherSound;
    public AudioClip repairSound;
    public AudioClip criticalHitSound;
    
    [Header("Visual Effects")]
    public GameObject hitEffect;
    public GameObject criticalHitEffect;
    public GameObject gatherEffect;
    public GameObject repairEffect;
    public Color toolUIColor = Color.white;
    
    [Header("Build Mode Protection (Hammer Only)")]
    public bool respectBuildMode = true;
    public float buildModeProtectionDelay = 1f;
}

public enum SurvivalToolType
{
    Hammer,     // Building destruction & repair
    Axe,        // Wood gathering
    Pickaxe,    // Stone/Metal mining
    Sword,      // Combat (future)
    Bow,        // Ranged combat (future)
    Shovel,     // Digging (future)
    Fishing     // Fishing (future)
}