using UnityEngine;
using AsakuShop.Store;

public class StoreSetup : MonoBehaviour
{
    [SerializeField] private int maxShoppers = 10;
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 15f;

    private void Start()
    {
        if (StoreManager.Instance != null)
        {
            StoreManager.Instance.LoadConfigurationFromResources(
                prefabsPath: "Store/Prefabs",
                spawnPointTag: "SpawnPoint",
                wanderPointTag: "WanderPoint",
                boundsObjectName: "StoreBounds"
            );
            
            // Update spawn intervals if needed
            // (Can add public setters to StoreManager if you want to configure these too)
        }
    }
}