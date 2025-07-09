using UnityEngine;

public class SimplePlayerTest : MonoBehaviour
{
    [Header("Test Settings")]
    public float damageAmount = 25f;
    
    void Update()
    {
        // Press SPACE to damage nearest enemy
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DamageNearestEnemy();
        }
    }
    
    void DamageNearestEnemy()
    {
        // ✅ FIXED: Look for AdvancedHealthBar instead
        AdvancedHealthBar enemy = FindObjectOfType<AdvancedHealthBar>();
        if (enemy != null && enemy.IsAlive())
        {
            enemy.TakeDamage(damageAmount);
            Debug.Log($"Player damaged enemy for {damageAmount}!");
        }
        else
        {
            Debug.Log("No alive enemy found!");
        }
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 150, 30), "Test Controls");
        GUI.Label(new Rect(20, 25, 130, 20), "SPACE - Damage Enemy");
    }
}