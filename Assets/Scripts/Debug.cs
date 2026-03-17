using UnityEngine;
using AsakuShop.Items;

namespace AsakuShop.Core
{
    public class Debug : MonoBehaviour
    {
        public Transform spawnPoint;
        public ItemRegistry itemRegistry;
        public GameClock gameClock;
        void Start()
        {
            itemRegistry = ItemRegistry.Instance;
            gameClock = GameBootstrapper.Clock;

            if (gameClock == null || itemRegistry == null)
            {
                UnityEngine.Debug.Log("GameClock or ItemRegistry not found. Make sure they are properly initialized in the scene.");
                return;
            }
            SpawnItem("item_sack_of_rice", spawnPoint.position);
            SpawnItem("item_sack_of_rice", spawnPoint.position);
            SpawnItem("item_sack_of_rice", spawnPoint.position);
            SpawnItem("item_sack_of_rice", spawnPoint.position);

        }

        public void SpawnItem(string itemID, Vector3 position)
        {
            ItemDefinition def = itemRegistry.Get(itemID);
            if (def != null && def.WorldPrefab != null)
            {
                GameTime currentTime = gameClock.CurrentTime;
                ItemInstance instance = new ItemInstance(def, currentTime);

                GameObject newObject = Instantiate(def.WorldPrefab, position, Quaternion.identity);
                newObject.name = $"{def.ItemId}_Instance";

                ItemPickup pickup = newObject.AddComponent<ItemPickup>();
                pickup.itemInstance = instance;

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
    
}
