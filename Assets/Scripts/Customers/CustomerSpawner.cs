using UnityEngine;
using System.Collections.Generic;
using AsakuShop.Items;

namespace AsakuShop.Customers
{
    public class CustomerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject customerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private List<ItemDefinition> availableItems = new();

        private float spawnTimer = 0f;
        private List<CustomerAgent> activeCustomers = new();

        private void Start()
        {
            if (customerPrefab == null)
            {
                Debug.LogError("Customer prefab not assigned!");
                return;
            }

            if (spawnPoint == null)
                spawnPoint = transform;

            spawnTimer = spawnInterval;
        }

        private void Update()
        {
            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0f)
            {
                SpawnCustomer();
                spawnTimer = spawnInterval;
            }

            // Clean up destroyed customers from list
            activeCustomers.RemoveAll(c => c == null);
        }

        private void SpawnCustomer()
        {
            GameObject customerGo = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
            CustomerAgent agent = customerGo.GetComponent<CustomerAgent>();

            if (agent == null)
            {
                agent = customerGo.AddComponent<CustomerAgent>();
            }

            // Generate shopping list and assign
            List<ItemInstance> shoppingList = ShoppingListGenerator.GenerateShoppingList(availableItems);
            agent.SetShoppingList(shoppingList);

            activeCustomers.Add(agent);
            Debug.Log($"[SPAWNER] Customer spawned. Active: {activeCustomers.Count}");
        }

        public void AddAvailableItem(ItemDefinition item)
        {
            if (!availableItems.Contains(item))
                availableItems.Add(item);
        }

        public int GetActiveCustomerCount() => activeCustomers.Count;
    }
}