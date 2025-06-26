using UnityEngine;
using System.Collections;

public class DecorativeBuildingComponent : MonoBehaviour
{
    [System.Serializable]
    public enum DecorationType
    {
        Torch,
        Campfire,
        Chest,
        Table,
        Chair
    }
    
    [Header("Decoration Settings")]
    public DecorationType decorationType = DecorationType.Torch;
    public bool providesLight = false;
    public float lightRadius = 2f;
    public bool providesComfort = false;
    public float comfortBonus = 5f;
    
    private GameObject lightEffect;
    
    void Start()
    {
        SetupDecoration();
        
        if (providesLight)
        {
            CreateSimpleLightEffect();
        }
        
        if (providesComfort)
        {
            StartCoroutine(ComfortBonus());
        }
    }
    
    void SetupDecoration()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            switch (decorationType)
            {
                case DecorationType.Torch:
                    sr.color = new Color(1f, 0.5f, 0f); // Orange
                    providesLight = true;
                    break;
                case DecorationType.Campfire:
                    sr.color = new Color(1f, 0.3f, 0f); // Red-orange
                    providesLight = true;
                    providesComfort = true;
                    break;
                case DecorationType.Chest:
                    sr.color = new Color(0.5f, 0.3f, 0.1f); // Brown
                    break;
                case DecorationType.Table:
                    sr.color = new Color(0.6f, 0.4f, 0.2f); // Light brown
                    providesComfort = true;
                    break;
                case DecorationType.Chair:
                    sr.color = new Color(0.4f, 0.2f, 0.1f); // Dark brown
                    providesComfort = true;
                    break;
            }
        }
    }
    
    void CreateSimpleLightEffect()
    {
        // Create simple light effect
        lightEffect = new GameObject("LightEffect");
        lightEffect.transform.SetParent(transform);
        lightEffect.transform.localPosition = Vector3.zero;
        
        SpriteRenderer lightSR = lightEffect.AddComponent<SpriteRenderer>();
        
        // Use Unity's default sprite
        lightSR.sprite = Resources.Load<Sprite>("UI/Skin/Knob");
        if (lightSR.sprite == null)
        {
            // Create simple texture if default sprite not found
            Texture2D lightTexture = new Texture2D(1, 1);
            lightTexture.SetPixel(0, 0, new Color(1f, 1f, 0.5f, 0.3f));
            lightTexture.Apply();
            lightSR.sprite = Sprite.Create(lightTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
        
        lightSR.color = new Color(1f, 1f, 0.5f, 0.2f); // Soft yellow light
        lightSR.sortingOrder = -2; // Behind everything
        
        // Scale light based on radius
        lightEffect.transform.localScale = Vector3.one * lightRadius;
        
        // Start light animation
        StartCoroutine(AnimateLight(lightSR));
    }
    
    IEnumerator AnimateLight(SpriteRenderer lightRenderer)
    {
        while (lightRenderer != null && gameObject != null)
        {
            // Simple flickering effect
            float alpha = Random.Range(0.15f, 0.25f);
            Color currentColor = lightRenderer.color;
            currentColor.a = alpha;
            lightRenderer.color = currentColor;
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    IEnumerator ComfortBonus()
    {
        while (gameObject != null)
        {
            // Find nearby player
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= lightRadius)
                {
                    // Apply comfort bonus
                    SurvivalManager survival = player.GetComponent<SurvivalManager>();
                    if (survival != null)
                    {
                        Debug.Log($"Player receiving comfort bonus from {decorationType}");
                    }
                }
            }
            yield return new WaitForSeconds(2f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (providesLight || providesComfort)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, lightRadius);
        }
    }
    
    void OnDestroy()
    {
        if (lightEffect != null)
        {
            DestroyImmediate(lightEffect);
        }
        StopAllCoroutines();
    }
}