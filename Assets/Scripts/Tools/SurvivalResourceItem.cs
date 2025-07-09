using UnityEngine;

public class SurvivalResourceItem : MonoBehaviour
{
    [Header("Resource Settings")]
    public string resourceType = "Wood";
    public int timesHarvested = 0;
    public int maxHarvests = 3;
    
    [Header("Resource Info")]
    public Sprite resourceIcon;
    public int baseAmount = 1;
    
    void Start()
    {
        // Initialize resource
        if (string.IsNullOrEmpty(resourceType))
        {
            resourceType = "Wood";
        }
    }
    
    // Method for tools to check resource type
    public bool IsResourceType(string category)
    {
        if (string.IsNullOrEmpty(resourceType)) return false;
        
        string lowerType = resourceType.ToLower();
        string lowerCategory = category.ToLower();
        
        if (lowerCategory == "wood")
        {
            return lowerType.Contains("tree") || lowerType.Contains("log") || lowerType.Contains("wood");
        }
        else if (lowerCategory == "stone")
        {
            return lowerType.Contains("stone") || lowerType.Contains("rock") || 
                   lowerType.Contains("ore") || lowerType.Contains("metal");
        }
        
        return false;
    }
    
    // Method to process harvesting
    public void OnHarvested()
    {
        timesHarvested++;
        
        // Visual feedback - change color based on damage
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float damage = (float)timesHarvested / maxHarvests;
            sr.color = Color.Lerp(Color.white, new Color(0.7f, 0.7f, 0.7f), damage);
        }
        
        // Destroy if fully harvested
        if (timesHarvested >= maxHarvests)
        {
            Destroy(gameObject);
        }
    }
}