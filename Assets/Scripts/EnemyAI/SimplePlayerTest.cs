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
        
        // Press E to test enemy status
        if (Input.GetKeyDown(KeyCode.E))
        {
            ShowEnemyStatus();
        }
        
        // Press Q to show enemy health
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ShowEnemyHealth();
        }
    }
    
    void DamageNearestEnemy()
    {
        // Look for EnemyHealth component
        EnemyHealth enemy = FindObjectOfType<EnemyHealth>();
        if (enemy != null && enemy.IsAlive())
        {
            enemy.TakeDamage(damageAmount, gameObject);
            Debug.Log($"Player damaged enemy for {damageAmount}!");
        }
        else
        {
            Debug.Log("No alive enemy found!");
        }
    }
    
    void ShowEnemyStatus()
    {
        EnemyAI enemy = FindObjectOfType<EnemyAI>();
        if (enemy != null)
        {
            enemy.ShowEnemyStatus();
        }
        else
        {
            Debug.Log("No enemy AI found!");
        }
    }
    
    void ShowEnemyHealth()
    {
        EnemyHealth enemy = FindObjectOfType<EnemyHealth>();
        if (enemy != null)
        {
            enemy.ShowHealthStatus();
        }
        else
        {
            Debug.Log("No enemy health found!");
        }
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 220, 70), "Test Controls");
        GUI.Label(new Rect(20, 25, 200, 20), "SPACE - Damage Enemy");
        GUI.Label(new Rect(20, 40, 200, 20), "E - Show Enemy Status");
        GUI.Label(new Rect(20, 55, 200, 20), "Q - Show Enemy Health");
    }
}