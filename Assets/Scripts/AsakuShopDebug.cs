using UnityEngine;
using AsakuShop.Items;

namespace AsakuShop.Core
{
    public class AsakuShopDebug : MonoBehaviour
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
                Debug.Log("GameClock or ItemRegistry not found. Make sure they are properly initialized in the scene.");
                return;
            }
            ItemInstanceSpawner.SpawnItemInstance("item_sack_of_rice", spawnPoint.position, spawnPoint.rotation);
            ItemInstanceSpawner.SpawnItemInstance("item_ramune_bottle", spawnPoint.position, spawnPoint.rotation);
            ItemInstanceSpawner.SpawnItemInstance("item_single_cup_noodle", spawnPoint.position, spawnPoint.rotation);
            ItemInstanceSpawner.SpawnItemInstance("item_sack_of_rice", spawnPoint.position, spawnPoint.rotation);
            ItemInstanceSpawner.SpawnItemInstance("item_ramune_bottle", spawnPoint.position, spawnPoint.rotation);
            ItemInstanceSpawner.SpawnItemInstance("item_single_cup_noodle", spawnPoint.position, spawnPoint.rotation);
            ItemInstanceSpawner.SpawnItemInstance("item_sack_of_rice", spawnPoint.position, spawnPoint.rotation);
            ItemInstanceSpawner.SpawnItemInstance("item_ramune_bottle", spawnPoint.position, spawnPoint.rotation);
            ItemInstanceSpawner.SpawnItemInstance("item_single_cup_noodle", spawnPoint.position, spawnPoint.rotation);
            ItemInstanceSpawner.SpawnItemInstance("item_sack_of_rice", spawnPoint.position, spawnPoint.rotation);
            ItemInstanceSpawner.SpawnItemInstance("item_ramune_bottle", spawnPoint.position, spawnPoint.rotation);
            ItemInstanceSpawner.SpawnItemInstance("item_single_cup_noodle", spawnPoint.position, spawnPoint.rotation);


        }
    } 
}
