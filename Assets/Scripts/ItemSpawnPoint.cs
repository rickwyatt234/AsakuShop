using AsakuShop.Core;
using AsakuShop.Items;
using UnityEngine;

public class ItemSpawnPoint : MonoBehaviour
{
    public ItemDefinition itemDefinition;
    [HideInInspector] public ItemRegistry itemRegistry;
    [HideInInspector] public GameClock gameClock;

    private void Awake()
    {
            itemRegistry = ItemRegistry.Instance;
            gameClock = GameBootstrapper.Clock;
    }
    private void Start()
    {
        SpawnItem(itemDefinition.ItemId, transform.position);
    }


    public void SpawnItem(string itemID, Vector3 position)
    {
        ItemDefinition def = itemRegistry.Get(itemID);
        if (def != null && def.WorldPrefab != null)
        {
            GameTime currentTime = gameClock.CurrentTime;
            ItemInstance instance = new ItemInstance(def, currentTime);

            GameObject newObject = Instantiate(def.WorldPrefab, position, Quaternion.identity);
            newObject.name = $"{def.DisplayName}";
            newObject.layer = LayerMask.NameToLayer("Item");

            ItemPickup pickup = newObject.AddComponent<ItemPickup>();
            pickup.itemInstance = instance;

            Rigidbody rb = newObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;

            MeshCollider collider = newObject.AddComponent<MeshCollider>();
            collider.convex = true;
        }
        else
        {
            UnityEngine.Debug.LogWarning($"ItemDefinition '{itemID}' not found or has no WorldPrefab.");
            return;
        }
    }
}