using UnityEngine;

public class BuildingDestructionManager : MonoBehaviour
{
    public void DestroyBuilding()
    {
        Debug.Log($"Building {gameObject.name} destroyed!");
        Destroy(gameObject);
    }
}