using UnityEngine;
using System.Collections.Generic;

public class ResourceSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject resourcePrefab; // ResourceItem prefab
    public int initialSpawnCount = 10;
    public float spawnRadius = 20f;
    public float minSpawnDistance = 2f;
    
    [Header("Spawn Area")]
    public Vector2 spawnAreaMin = new Vector2(-25f, -25f);
    public Vector2 spawnAreaMax = new Vector2(25f, 25f);
    
    [Header("Resource Types")]
    public List<ResourceSpawnData> spawnData = new List<ResourceSpawnData>();
    
    private List<Vector2> spawnedPositions = new List<Vector2>();
    
    void Start()
    {
        // Initialize default spawn data if empty
        if (spawnData.Count == 0)
        {
            InitializeDefaultSpawnData();
        }
        
        // Spawn initial resources
        SpawnInitialResources();
    }
    
    void InitializeDefaultSpawnData()
    {
        spawnData.Add(new ResourceSpawnData(ResourceType.Food, 30f));
        spawnData.Add(new ResourceSpawnData(ResourceType.Water, 25f));
        spawnData.Add(new ResourceSpawnData(ResourceType.Medicine, 10f));
        spawnData.Add(new ResourceSpawnData(ResourceType.Wood, 20f));
        spawnData.Add(new ResourceSpawnData(ResourceType.Stone, 10f));
        spawnData.Add(new ResourceSpawnData(ResourceType.Metal, 5f));
        
        Debug.Log("ResourceSpawner: Default spawn data initialized");
    }
    
    void SpawnInitialResources()
    {
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnRandomResource();
        }
        
        Debug.Log($"ResourceSpawner: Spawned {initialSpawnCount} initial resources");
    }
    
    void SpawnRandomResource()
    {
        // Get random resource type based on spawn weights
        ResourceType resourceType = GetRandomResourceType();
        
        // Get random spawn position
        Vector2 spawnPos = GetRandomSpawnPosition();
        
        // Spawn the resource
        SpawnResourceAt(resourceType, spawnPos);
    }
    
    ResourceType GetRandomResourceType()
    {
        float totalWeight = 0f;
        foreach (var data in spawnData)
        {
            totalWeight += data.spawnWeight;
        }
        
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var data in spawnData)
        {
            currentWeight += data.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return data.resourceType;
            }
        }
        
        return ResourceType.Food; // Fallback
    }
    
    Vector2 GetRandomSpawnPosition()
    {
        Vector2 spawnPos;
        int attempts = 0;
        int maxAttempts = 50;
        
        do
        {
            spawnPos = new Vector2(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y)
            );
            attempts++;
        }
        while (!IsValidSpawnPosition(spawnPos) && attempts < maxAttempts);
        
        return spawnPos;
    }
    
    bool IsValidSpawnPosition(Vector2 position)
    {
        // Check minimum distance from other spawned resources
        foreach (Vector2 spawnedPos in spawnedPositions)
        {
            if (Vector2.Distance(position, spawnedPos) < minSpawnDistance)
            {
                return false;
            }
        }
        
        // Check distance from player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float playerDistance = Vector2.Distance(position, player.transform.position);
            if (playerDistance < 3f) // Don't spawn too close to player
            {
                return false;
            }
        }
        
        return true;
    }
    
    void SpawnResourceAt(ResourceType resourceType, Vector2 position)
    {
        if (resourcePrefab == null)
        {
            Debug.LogError("ResourceSpawner: Resource prefab not assigned!");
            return;
        }
        
        // Instantiate resource
        GameObject resource = Instantiate(resourcePrefab, position, Quaternion.identity);
        
        // Configure resource
        ResourceItem resourceItem = resource.GetComponent<ResourceItem>();
        if (resourceItem != null)
        {
            resourceItem.resourceType = resourceType;
            resourceItem.quantity = Random.Range(1, 4); // Random quantity 1-3
        }
        
        // Add to spawned positions
        spawnedPositions.Add(position);
    }
    
    // Public method to spawn specific resource
    public void SpawnResource(ResourceType resourceType, Vector2 position)
    {
        SpawnResourceAt(resourceType, position);
    }
    
    // Respawn resources periodically (optional)
    public void RespawnResources()
    {
        SpawnRandomResource();
    }
    
    // Gizmos for spawn area visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = new Vector3((spawnAreaMin.x + spawnAreaMax.x) / 2, (spawnAreaMin.y + spawnAreaMax.y) / 2, 0);
        Vector3 size = new Vector3(spawnAreaMax.x - spawnAreaMin.x, spawnAreaMax.y - spawnAreaMin.y, 0);
        Gizmos.DrawWireCube(center, size);
    }
}

[System.Serializable]
public class ResourceSpawnData
{
    public ResourceType resourceType;
    public float spawnWeight;
    
    public ResourceSpawnData(ResourceType type, float weight)
    {
        resourceType = type;
        spawnWeight = weight;
    }
}