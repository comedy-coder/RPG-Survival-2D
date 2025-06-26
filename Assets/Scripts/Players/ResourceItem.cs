using UnityEngine;

[System.Serializable]
public enum ResourceType
{
    Food,           // Restores hunger
    Water,          // Restores thirst  
    Medicine,       // Restores health
    Wood,           // Crafting material
    Stone,          // Crafting material
    Metal           // Crafting material
}

public class ResourceItem : MonoBehaviour
{
    [Header("Resource Settings")]
    public ResourceType resourceType;
    public string itemName;
    public int quantity = 1;
    public float restoreAmount = 20f; // For consumables
    
    [Header("Visual Settings")]
    public Color itemColor = Color.white;
    public float bobSpeed = 2f;
    public float bobHeight = 0.5f;
    
    [Header("Pickup Settings")]
    public float pickupRange = 2f;
    public bool autoPickup = true;
    
    private Vector3 startPosition;
    private SpriteRenderer spriteRenderer;
    private bool isPickedUp = false;
    
    void Start()
    {
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Set color based on resource type
        SetResourceColor();
        
        // Setup item name if not set
        if (string.IsNullOrEmpty(itemName))
        {
            itemName = resourceType.ToString();
        }
        
        Debug.Log($"Resource spawned: {itemName} x{quantity}");
    }
    
    void Update()
    {
        if (isPickedUp) return;
        
        // Bobbing animation
        BobAnimation();
        
        // Check for player in range
        if (autoPickup)
        {
            CheckPlayerInRange();
        }
    }
    
    void BobAnimation()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
    
    void CheckPlayerInRange()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance <= pickupRange)
            {
                TryPickup(player);
            }
        }
    }
    
    void SetResourceColor()
    {
        if (spriteRenderer == null) return;
        
        switch (resourceType)
        {
            case ResourceType.Food:
                itemColor = Color.green;
                break;
            case ResourceType.Water:
                itemColor = Color.blue;
                break;
            case ResourceType.Medicine:
                itemColor = Color.red;
                break;
            case ResourceType.Wood:
                itemColor = new Color(0.6f, 0.3f, 0.1f); // Brown
                break;
            case ResourceType.Stone:
                itemColor = Color.gray;
                break;
            case ResourceType.Metal:
                itemColor = Color.white;
                break;
        }
        
        spriteRenderer.color = itemColor;
    }
    
    public bool TryPickup(GameObject player)
    {
        if (isPickedUp) return false;
        
        // For now, directly consume items (no inventory system yet)
        ConsumeDirect(player);
        return true;
    }
    
    void ConsumeDirect(GameObject player)
    {
        SurvivalManager survival = FindObjectOfType<SurvivalManager>();
        if (survival != null)
        {
            switch (resourceType)
            {
                case ResourceType.Food:
                    survival.RestoreHunger(restoreAmount);
                    break;
                case ResourceType.Water:
                    survival.RestoreThirst(restoreAmount);
                    break;
                case ResourceType.Medicine:
                    survival.RestoreHealth(restoreAmount);
                    break;
            }
        }
        
        OnPickup(player);
    }
    
    void OnPickup(GameObject player)
    {
        isPickedUp = true;
        Debug.Log($"Player picked up: {itemName} x{quantity}");
        
        // Pickup effect (optional)
        // TODO: Add particle effect, sound effect
        
        // Destroy the item
        Destroy(gameObject);
    }
    
    // Gizmos for pickup range visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}