using UnityEngine;
using System;

namespace AsakuShop.Items
{
    public class FurnitureInstance : MonoBehaviour
    {
        // Represents one physical instance of a piece of furniture in the game world. Wraps a
        // shared FurnitureDefinition. Instances are always discrete ie there is no 
        // stacking or quantity field.

        public string InstanceId { get; }
        public FurnitureDefinition Definition { get; }
        public int CurrentPrice { get; set; }

        // Creates a new instance based on the provided FurnitureDefinition.
         public FurnitureInstance(FurnitureDefinition definition)
        {
            InstanceId = Guid.NewGuid().ToString();
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentPrice = definition.WorldPrefab.GetComponent<FurniturePickup>() != null ? definition.WorldPrefab.GetComponent<FurniturePickup>().FurnitureInstance.CurrentPrice : 0;
        }

        internal FurnitureInstance(string instanceId, FurnitureDefinition definition, int currentPrice)
        {
            InstanceId    = instanceId ?? Guid.NewGuid().ToString();
            Definition    = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentPrice = currentPrice;
        }

        public override string ToString()
        {
            string name = Definition != null ? Definition.DisplayName : "Unknown Furniture";
            return $"[{name} | Price: ¥{CurrentPrice}]";
        }

        
    }
}
