using UnityEngine;

public class FloorComponent : MonoBehaviour
{
    [System.Serializable]
    public enum FloorType
    {
        Wood,
        Stone,
        Metal
    }
    
    [Header("Floor Settings")]
    public bool providesSpeedBoost = true;
    public float speedMultiplier = 1.2f;
    public FloorType floorType = FloorType.Wood;
    
    private BoxCollider2D floorCollider;
    private bool playerOnFloor = false;
    
    void Start()
    {
        SetupFloor();
    }
    
    void SetupFloor()
    {
        // Setup floor collision detection
        floorCollider = GetComponent<BoxCollider2D>();
        if (floorCollider == null)
        {
            floorCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        floorCollider.isTrigger = true; // Player can walk through
        floorCollider.size = Vector2.one; // Standard floor size
        
        // Set color based on floor type
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            switch (floorType)
            {
                case FloorType.Wood:
                    sr.color = new Color(0.6f, 0.4f, 0.2f);
                    break;
                case FloorType.Stone:
                    sr.color = new Color(0.5f, 0.5f, 0.5f);
                    break;
                case FloorType.Metal:
                    sr.color = new Color(0.7f, 0.7f, 0.8f);
                    break;
            }
            
            // Floor should be below other buildings
            sr.sortingOrder = -1;
        }
        
        Debug.Log($"Floor setup complete - Type: {floorType}, Speed Boost: {providesSpeedBoost}");
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && providesSpeedBoost)
        {
            playerOnFloor = true;
            ApplySpeedBoost(other.gameObject, true);
            Debug.Log($"Player stepped on {floorType} floor - speed boost applied");
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && providesSpeedBoost)
        {
            playerOnFloor = false;
            ApplySpeedBoost(other.gameObject, false);
            Debug.Log("Player left floor - speed boost removed");
        }
    }
    
    void ApplySpeedBoost(GameObject player, bool apply)
    {
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // This would require modifying PlayerController to support speed modifiers
            // For now, just log the effect
            if (apply)
            {
                Debug.Log($"Speed boost applied: {speedMultiplier}x");
                // playerController.SetSpeedMultiplier(speedMultiplier);
            }
            else
            {
                Debug.Log("Speed boost removed");
                // playerController.SetSpeedMultiplier(1f);
            }
        }
    }
    
    // Method to change floor type at runtime
    public void ChangeFloorType(FloorType newType)
    {
        floorType = newType;
        SetupFloor(); // Re-setup with new type
    }
    
    // Get floor material for crafting/upgrading
    public string GetFloorMaterial()
    {
        switch (floorType)
        {
            case FloorType.Wood: return "Wood";
            case FloorType.Stone: return "Stone";
            case FloorType.Metal: return "Metal";
            default: return "Wood";
        }
    }
    
    // Visual feedback in editor
    void OnDrawGizmosSelected()
    {
        // Draw floor area
        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.3f);
        Gizmos.DrawCube(transform.position, Vector3.one);
        
        // Draw speed boost indicator
        if (providesSpeedBoost)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 1.1f);
        }
        
        // Show player detection
        if (playerOnFloor)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.1f, 0.2f);
        }
    }
}