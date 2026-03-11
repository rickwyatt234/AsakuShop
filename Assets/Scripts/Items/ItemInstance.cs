using System;
using AsakuShop.Core;

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

        // The price actually paid for this specific instance in yen. May differ
        // from ItemDefinition.BaseBuyPrice due to market
        // fluctuations, crate-rolling, or negotiation.
        public float PurchasePrice { get; set; }

        // The in-game moment when this instance was created or purchased.
        // Used for age calculations and spoilage tracking.
        public GameTime AcquiredTime { get; }

        // Creates a new instance using the definition's base grade and base buy price as starting values.
        public ItemInstance(ItemDefinition definition, GameTime acquiredTime)
        {
            Definition    = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentGrade  = definition.BaseGrade;
            PurchasePrice = definition.BaseBuyPrice;
            AcquiredTime  = acquiredTime;
            InstanceId    = Guid.NewGuid().ToString();
        }

        // Creates a new instance with an explicitly supplied grade and purchase price.
        // Use this constructor when the grade has been rolled (e.g. for a crate purchase)
        // or when restoring from a save snapshot.
        public ItemInstance(ItemDefinition definition, ItemGrade grade, float purchasePrice, GameTime acquiredTime)
        {
            Definition    = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentGrade  = grade;
            PurchasePrice = purchasePrice;
            AcquiredTime  = acquiredTime;
            InstanceId    = Guid.NewGuid().ToString();
        }

        // Restores an instance from a save snapshot, preserving the original InstanceId. 
        // For internal use by ItemSaveData.Restore only.
        internal ItemInstance(string instanceId, ItemDefinition definition, ItemGrade grade, float purchasePrice, GameTime acquiredTime)
        {
            InstanceId    = instanceId ?? Guid.NewGuid().ToString();
            Definition    = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentGrade  = grade;
            PurchasePrice = purchasePrice;
            AcquiredTime  = acquiredTime;
        }

        // Applies one step of overnight quality decay, lowering CurrentGrade by one tier. 
        // Grade will not fall below ItemGrade.F. Called once per overnight pass for all perishable items.
        public void ApplyOvernightDecay()
        {
            CurrentGrade = CurrentGrade.Decay();
        }

        // Returns a human-readable summary, e.g. [Salmon Onigiri | Grade: A | ¥180]"
        public override string ToString()
        {
            string name  = Definition != null ? Definition.DisplayName : "Unknown";
            string grade = CurrentGrade.ToDisplayString();
            return $"[{name} | Grade: {grade} | ¥{PurchasePrice}]";
        }
    }
}