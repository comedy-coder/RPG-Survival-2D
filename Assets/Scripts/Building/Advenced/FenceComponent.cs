using UnityEngine;

public class FenceComponent : MonoBehaviour
{
    [Header("Fence Settings")]
    public bool blocksEnemies = true;
    public bool allowsPlayerPassage = true;
    public float fenceHeight = 1.5f;
    
    private BoxCollider2D fenceCollider;
    
    void Start()
    {
        SetupFence();
    }
    
    void SetupFence()
    {
        // Get or add collider
        fenceCollider = GetComponent<BoxCollider2D>();
        if (fenceCollider == null)
        {
            fenceCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Configure fence collision
        fenceCollider.size = new Vector2(1f, fenceHeight);
        fenceCollider.isTrigger = false; // Solid collision
        
        // Set fence appearance
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(0.4f, 0.2f, 0.1f); // Dark brown
            // Scale sprite to match fence height
            transform.localScale = new Vector3(1f, fenceHeight, 1f);
        }
        
        Debug.Log($"Fence setup complete - Height: {fenceHeight}, Blocks Enemies: {blocksEnemies}");
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;
        
        if (other.CompareTag("Enemy") && blocksEnemies)
        {
            Debug.Log("Fence blocked enemy movement");
            // Could add particle effect or sound here
        }
        else if (other.CompareTag("Player") && !allowsPlayerPassage)
        {
            Debug.Log("Fence blocked player movement");
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Alternative collision detection if using trigger
        if (other.CompareTag("Enemy") && blocksEnemies)
        {
            Debug.Log("Enemy approaching fence");
        }
    }
    
    // Method to toggle fence settings at runtime
    public void ToggleEnemyBlocking()
    {
        blocksEnemies = !blocksEnemies;
        Debug.Log($"Fence enemy blocking: {blocksEnemies}");
    }
    
    public void TogglePlayerPassage()
    {
        allowsPlayerPassage = !allowsPlayerPassage;
        Debug.Log($"Fence allows player passage: {allowsPlayerPassage}");
    }
    
    // Visual feedback in editor
    void OnDrawGizmosSelected()
    {
        // Draw fence collision area
        Gizmos.color = blocksEnemies ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, fenceHeight, 0f));
        
        // Draw blocking indicator
        if (blocksEnemies)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.left * 0.5f, transform.position + Vector3.right * 0.5f);
        }
    }
}