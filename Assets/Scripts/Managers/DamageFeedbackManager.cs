using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamageFeedbackManager : MonoBehaviour
{
    [Header("Damage Text Settings")]
    public GameObject damageTextPrefab; // Prefab cho damage text
    public Canvas worldCanvas; // Canvas để hiển thị damage text
    public Color damageColor = Color.red;
    public float textLifetime = 2f;
    public float textMoveSpeed = 2f;
    
    [Header("Screen Shake Settings")]
    public bool enableScreenShake = true;
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.2f;
    
    [Header("Building Flash Settings")]
    public bool enableBuildingFlash = true;
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;
    
    [Header("Audio Settings")]
    public AudioClip hitSoundEffect;
    public AudioSource audioSource;
    
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<Camera>();
            
        if (mainCamera != null)
            originalCameraPosition = mainCamera.transform.position;
            
        // Auto-create world canvas if not assigned
        if (worldCanvas == null)
        {
            CreateWorldCanvas();
        }
        
        // Auto-setup audio if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        Debug.Log("🎬 Damage Feedback Manager initialized!");
    }
    
    void CreateWorldCanvas()
    {
        GameObject canvasGO = new GameObject("DamageTextCanvas");
        worldCanvas = canvasGO.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = mainCamera;
        worldCanvas.sortingOrder = 100; // On top of everything
        
        // Add CanvasScaler for consistent sizing
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        
        Debug.Log("🎬 Created world canvas for damage text");
    }
    
    // MAIN METHOD: Call this when building takes damage
    public void ShowDamageEffects(Vector3 worldPosition, int damageAmount, string buildingName = "Building")
    {
        // 1. Show floating damage text
        ShowDamageText(worldPosition, damageAmount);
        
        // 2. Screen shake effect
        if (enableScreenShake)
            StartCoroutine(ScreenShake());
            
        // 3. Play hit sound
        PlayHitSound();
        
        // 4. Building flash effect (if building object provided)
        GameObject building = GetBuildingAtPosition(worldPosition);
        if (building != null && enableBuildingFlash)
            StartCoroutine(FlashBuilding(building));
            
        // 5. Console feedback
        Debug.Log($"💥 DAMAGE FEEDBACK: {buildingName} took {damageAmount} damage at {worldPosition}");
    }
    
    void ShowDamageText(Vector3 worldPosition, int damageAmount)
    {
        if (worldCanvas == null) return;
        
        // Create damage text GameObject
        GameObject damageTextObj = CreateDamageTextObject(damageAmount);
        if (damageTextObj == null) return;
        
        // Position in world space
        damageTextObj.transform.SetParent(worldCanvas.transform, false);
        damageTextObj.transform.position = worldPosition + Vector3.up * 0.5f; // Slightly above building
        
        // Start animation
        StartCoroutine(AnimateDamageText(damageTextObj));
        
        Debug.Log($"📝 Damage text '{damageAmount}' created at {worldPosition}");
    }
    
    GameObject CreateDamageTextObject(int damageAmount)
    {
        GameObject textObj;
        
        if (damageTextPrefab != null)
        {
            // Use provided prefab
            textObj = Instantiate(damageTextPrefab);
        }
        else
        {
            // Create simple text object
            textObj = new GameObject("DamageText");
            Text textComponent = textObj.AddComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 24;
            textComponent.color = damageColor;
            textComponent.text = "-" + damageAmount.ToString();
            textComponent.alignment = TextAnchor.MiddleCenter;
            
            // Add outline for better visibility
            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, 1);
        }
        
        // Ensure text component exists and set damage value
        Text text = textObj.GetComponent<Text>();
        if (text != null)
        {
            text.text = "-" + damageAmount.ToString();
            text.color = damageColor;
        }
        
        return textObj;
    }
    
    IEnumerator AnimateDamageText(GameObject textObj)
    {
        if (textObj == null) yield break;
        
        Vector3 startPos = textObj.transform.position;
        Vector3 endPos = startPos + Vector3.up * textMoveSpeed;
        
        float elapsed = 0f;
        
        while (elapsed < textLifetime && textObj != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / textLifetime;
            
            // Move text upward
            textObj.transform.position = Vector3.Lerp(startPos, endPos, progress);
            
            // Fade out
            Text textComponent = textObj.GetComponent<Text>();
            if (textComponent != null)
            {
                Color color = textComponent.color;
                color.a = 1f - progress;
                textComponent.color = color;
                
                // Optional: Scale effect
                float scale = 1f + (progress * 0.5f);
                textObj.transform.localScale = Vector3.one * scale;
            }
            
            yield return null;
        }
        
        // Clean up
        if (textObj != null)
            Destroy(textObj);
    }
    
    IEnumerator ScreenShake()
    {
        if (mainCamera == null) yield break;
        
        Vector3 originalPos = mainCamera.transform.position;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            
            // Random shake offset
            Vector3 randomOffset = Random.insideUnitCircle * shakeIntensity;
            mainCamera.transform.position = originalPos + randomOffset;
            
            yield return null;
        }
        
        // Return to original position
        mainCamera.transform.position = originalPos;
    }
    
    IEnumerator FlashBuilding(GameObject building)
    {
        SpriteRenderer renderer = building.GetComponent<SpriteRenderer>();
        if (renderer == null) yield break;
        
        Color originalColor = renderer.color;
        
        // Flash to damage color
        renderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        
        // Return to original color
        renderer.color = originalColor;
    }
    
    void PlayHitSound()
    {
        if (audioSource != null && hitSoundEffect != null)
        {
            audioSource.PlayOneShot(hitSoundEffect);
        }
    }
    
    GameObject GetBuildingAtPosition(Vector3 position)
    {
        // Try to find building at this position
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, 0.5f);
        foreach (Collider2D col in colliders)
        {
            BuildingComponent building = col.GetComponent<BuildingComponent>();
            if (building != null)
                return col.gameObject;
        }
        return null;
    }
    
    // PUBLIC METHODS for easy integration
    public void ShowBuildingHit(Vector3 position, int damage, string buildingName)
    {
        ShowDamageEffects(position, damage, buildingName);
    }
    
    public void ShowSimpleDamage(Vector3 position, int damage)
    {
        ShowDamageText(position, damage);
        if (enableScreenShake)
            StartCoroutine(ScreenShake());
        PlayHitSound();
    }
}

// EXTENSION: Add this to your existing BuildingComponent or DestructionTool
/*
// In BuildingComponent.cs - Add this method:
public void TakeDamage(int damage) 
{
    currentDurability -= damage;
    
    // NEW: Show visual feedback
    DamageFeedbackManager feedbackManager = FindFirstObjectByType<DamageFeedbackManager>();
    if (feedbackManager != null)
    {
        feedbackManager.ShowBuildingHit(transform.position, damage, buildingName);
    }
    
    if (currentDurability <= 0)
    {
        // Building destroyed logic
    }
}
*/