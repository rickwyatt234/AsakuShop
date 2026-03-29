using System;
using log4net.Util;

namespace AsakuShop.Items
{
    // Represents one physical instance of an item in the game world. Wraps a
    // shared ItemDefinition with per-instance mutable state such
    // as current grade, purchase price, and acquisition time.
    // Instances are always discrete ie there is no stacking or quantity field.
    // A storage container (backpack, shelf crate, etc.) holds a collection of
    // ItemInstance objects, each independently graded and priced.
    public class ItemInstance
    {
        // Unique identifier assigned at construction via
        // Guid.NewGuid(). Used as the primary key for save/load
        // identity — never changes after creation.
        public string InstanceId { get; }

        // Reference to the shared ItemDefinition ScriptableObject
        // that describes the static properties of this item type. Read-only after construction.
        public ItemDefinition Definition { get; }

        // The current quality grade of this instance. Starts at
        // ItemDefinition.BaseGrade and may be lowered by
        // overnight decay or other game events.
        public ItemGrade CurrentGrade { get; set; }

        // The price paid to acquire this item instance. Starts at
        // ItemDefinition.BasePrice and may be modified by game events.
        public float CurrentPrice { get; set; }

        public bool IsOnAShelf { get; set; } = false;

        public bool IsInOptimalStorageConditions
        {
            get
            {
                // For now, just return true for all items since I haven't implemented storage conditions yet.
                // In the future, this will check the current storage environment against the item's PreferredStorageType.
                return true;
            }
        }

        // Creates a new instance with default grade and price based on the provided ItemDefinition.
        // If a player-set price override exists in ItemPriceRegistry for this item type, that
        // price is used instead of ItemDefinition.BasePrice.
        public ItemInstance(ItemDefinition definition)
        {
            Definition    = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentGrade  = definition.BaseGrade;
            CurrentPrice  = ItemPriceRegistry.GetEffectivePrice(definition);
            InstanceId    = Guid.NewGuid().ToString();
        }

        // Creates a new instance with an explicitly supplied grade. 
        // Crafting outputs and buying crates of items will use this constructor to set the 
        // grade based on recipe results or crate purchase rolls.
        // If a player-set price override exists in ItemPriceRegistry for this item type, that
        // price is used instead of ItemDefinition.BasePrice.
        public ItemInstance(ItemDefinition definition, ItemGrade grade)
        {
            Definition    = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentGrade  = grade;
            CurrentPrice  = ItemPriceRegistry.GetEffectivePrice(definition);
            InstanceId    = Guid.NewGuid().ToString();
        }

        // Creates a new instance with explicitly supplied grade and price.
        // Used for instantiating items in markets or other special cases
        public ItemInstance(ItemDefinition definition, ItemGrade grade, float price)
        {
            Definition    = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentGrade  = grade;
            CurrentPrice = price;
            InstanceId    = Guid.NewGuid().ToString();
        }


        // Restores an instance from a save snapshot, preserving the original InstanceId. 
        // For internal use by ItemSaveData.Restore only.
        internal ItemInstance(string instanceId, ItemDefinition definition, ItemGrade grade, float currentPrice)
        {
            InstanceId    = instanceId ?? Guid.NewGuid().ToString();
            Definition    = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentGrade  = grade;
            CurrentPrice = currentPrice;
        }

        // Applies one step of overnight quality decay, lowering CurrentGrade by one tier. 
        // Grade will not fall below ItemGrade.F. Called once per overnight pass for all perishable items.
        public void ApplyOvernightDecay()
        {
            if (Definition.IsPerishable && IsInOptimalStorageConditions)
            {
                //DO LOGIC LATER
                CurrentGrade = CurrentGrade.Decay();
            }
            else if (Definition.IsPerishable && !IsInOptimalStorageConditions)
            {
                CurrentGrade = CurrentGrade.Decay();
            }
        }

        // Returns a human-readable summary, e.g. [Salmon Onigiri | Grade: A | Price: ¥180]"
        public override string ToString()
        {
            string name  = Definition != null ? Definition.DisplayName : "Unknown";
            string grade = CurrentGrade.ToDisplayString();
            return $"[{name} | Grade: {grade} | Price: ¥{CurrentPrice}]";
        }

        public string GradeToString()
        {
            return CurrentGrade.ToDisplayString();
        }

        
    }
}
