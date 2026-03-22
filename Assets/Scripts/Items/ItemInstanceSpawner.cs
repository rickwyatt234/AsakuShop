using UnityEngine;
using AsakuShop.Items;

public static class ItemInstanceSpawner
{
    // Spawns an ItemInstance in the world based on the provided ItemDefinition.
    // The spawned GameObject will have an ItemPickup component for player interaction.
    public static GameObject SpawnItemInstance(ItemDefinition definition, Vector3 position, Quaternion rotation)
    {
        if (definition == null || definition.WorldPrefab == null)
        {
            Debug.LogError("Invalid ItemDefinition or missing WorldPrefab.");
            return null;
        }

        // Instantiate the world prefab
        GameObject itemObject = Object.Instantiate(definition.WorldPrefab, position, rotation);

        // Create a new ItemInstance and assign the definition
        ItemInstance itemInstance = new ItemInstance(definition);

        // Add an ItemPickup component for interaction
        ItemPickup pickup = itemObject.AddComponent<ItemPickup>();
        pickup.Initialize(itemInstance);

        return itemObject;
    }

    public static GameObject SpawnItemInstance(ItemDefinition definition, ItemGrade grade, Vector3 position, Quaternion rotation)
    {
        if (definition == null || definition.WorldPrefab == null)
        {
            Debug.LogError("Invalid ItemDefinition or missing WorldPrefab.");
            return null;
        }

        // Instantiate the world prefab
        GameObject itemObject = Object.Instantiate(definition.WorldPrefab, position, rotation);

        // Create a new ItemInstance and assign the definition
        ItemInstance itemInstance = new ItemInstance(definition, grade);

        // Add an ItemPickup component for interaction
        ItemPickup pickup = itemObject.AddComponent<ItemPickup>();
        pickup.Initialize(itemInstance);

        return itemObject;
    }

    public static GameObject SpawnItemInstance(ItemDefinition definition, ItemGrade grade, float currentPrice, Vector3 position, Quaternion rotation)
    {
        if (definition == null || definition.WorldPrefab == null)
        {
            Debug.LogError("Invalid ItemDefinition or missing WorldPrefab.");
            return null;
        }

        // Instantiate the world prefab
        GameObject itemObject = Object.Instantiate(definition.WorldPrefab, position, rotation);

        // Create a new ItemInstance and assign the definition
        ItemInstance itemInstance = new ItemInstance(definition, grade, currentPrice);

        // Add an ItemPickup component for interaction
        ItemPickup pickup = itemObject.AddComponent<ItemPickup>();
        pickup.Initialize(itemInstance);

        return itemObject;
    }
}
