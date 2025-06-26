using UnityEngine;
using System.Collections;

public class RoofComponent : MonoBehaviour
{
    [Header("Roof Settings")]
    public bool providesWeatherProtection = true;
    public float protectionRadius = 3f;
    public Color roofColor = new Color(0.6f, 0.3f, 0.1f); // Brown
    
    private SpriteRenderer spriteRenderer;
    private bool playerUnderRoof = false;
    
    void Start()
    {
        SetupRoof();
        
        if (providesWeatherProtection)
        {
            StartCoroutine(CheckWeatherProtection());
        }
    }
    
    void SetupRoof()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = roofColor;
            // Make roof appear above other buildings
            spriteRenderer.sortingOrder = 1;
        }
        
        Debug.Log($"Roof setup complete - Protection Radius: {protectionRadius}");
    }
    
    IEnumerator CheckWeatherProtection()
    {
        while (gameObject != null)
        {
            if (providesWeatherProtection)
            {
                CheckForPlayer();
            }
            yield return new WaitForSeconds(1f);
        }
    }
    
    void CheckForPlayer()
    {
        // Find player within protection radius
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool currentlyUnderRoof = distance <= protectionRadius;
            
            if (currentlyUnderRoof != playerUnderRoof)
            {
                playerUnderRoof = currentlyUnderRoof;
                
                if (playerUnderRoof)
                {
                    ApplyWeatherProtection(player);
                }
                else
                {
                    RemoveWeatherProtection(player);
                }
            }
        }
    }
    
    void ApplyWeatherProtection(GameObject player)
    {
        // Apply weather protection buff to player
        SurvivalManager survival = player.GetComponent<SurvivalManager>();
        if (survival != null)
        {
            Debug.Log("Player protected from weather by roof");
            // Could add actual weather protection logic here
            // survival.SetWeatherProtection(true);
        }
        
        // Visual feedback
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(roofColor, Color.green, 0.3f);
        }
    }
    
    void RemoveWeatherProtection(GameObject player)
    {
        SurvivalManager survival = player.GetComponent<SurvivalManager>();
        if (survival != null)
        {
            Debug.Log("Player no longer protected by roof");
            // survival.SetWeatherProtection(false);
        }
        
        // Reset visual
        if (spriteRenderer != null)
        {
            spriteRenderer.color = roofColor;
        }
    }
    
    // Method to upgrade roof protection
    public void UpgradeProtection(float newRadius)
    {
        protectionRadius = newRadius;
        Debug.Log($"Roof protection upgraded to radius: {protectionRadius}");
    }
    
    // Method to check if position is under roof protection
    public bool IsPositionProtected(Vector3 position)
    {
        float distance = Vector3.Distance(transform.position, position);
        return distance <= protectionRadius;
    }
    
    // Visual feedback in editor
    void OnDrawGizmosSelected()
    {
        // Draw protection radius
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, protectionRadius);
        
        // Draw roof outline
        Gizmos.color = roofColor;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
        
        // Show player protection status
        if (playerUnderRoof)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, protectionRadius);
        }
    }
    
    void OnDestroy()
    {
        // Clean up when roof is destroyed
        if (playerUnderRoof)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                RemoveWeatherProtection(player);
            }
        }
        
        StopAllCoroutines();
    }
}