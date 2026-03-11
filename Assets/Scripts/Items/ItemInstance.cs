using System;
using AsakuShop.Core;

namespace AsakuShop.Items
{
    /// <summary>
    /// Represents one physical instance of an item in the game world. Wraps a
    /// shared <see cref="ItemDefinition"/> with per-instance mutable state such
    /// as current grade, purchase price, and acquisition time.
    /// </summary>
    /// <remarks>
    /// Instances are always discrete — there is no stacking or quantity field.
    /// A storage container (backpack, shelf crate, etc.) holds a collection of
    /// <see cref="ItemInstance"/> objects, each independently graded and priced.
    /// </remarks>
    public class ItemInstance
    {
        // ── Identity ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Unique identifier assigned at construction via
        /// <see cref="Guid.NewGuid"/>. Used as the primary key for save/load
        /// identity — never changes after creation.
        /// </summary>
        public string InstanceId { get; }

        // ── Definition ────────────────────────────────────────────────────────────

        /// <summary>
        /// Reference to the shared <see cref="ItemDefinition"/> ScriptableObject
        /// that describes the static properties of this item type. Read-only
        /// after construction.
        /// </summary>
        public ItemDefinition Definition { get; }

        // ── Mutable state ─────────────────────────────────────────────────────────

        /// <summary>
        /// The current quality grade of this instance. Starts at
        /// <see cref="ItemDefinition.BaseGrade"/> and may be lowered by
        /// overnight decay or other game events.
        /// </summary>
        public ItemGrade CurrentGrade { get; set; }

        /// <summary>
        /// The price actually paid for this specific instance in yen. May differ
        /// from <see cref="ItemDefinition.BaseBuyPrice"/> due to market
        /// fluctuations, crate-rolling, or negotiation.
        /// </summary>
        public float PurchasePrice { get; set; }

        /// <summary>
        /// The in-game moment when this instance was created or purchased.
        /// Used for age calculations and spoilage tracking.
        /// </summary>
        public GameTime AcquiredTime { get; }

        // ── Constructors ──────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new instance using the definition's base grade and base buy
        /// price as starting values.
        /// </summary>
        /// <param name="definition">
        /// The <see cref="ItemDefinition"/> that describes this item type. Must
        /// not be <c>null</c>.
        /// </param>
        /// <param name="acquiredTime">
        /// The current in-game time at the moment of acquisition.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="definition"/> is <c>null</c>.
        /// </exception>
        public ItemInstance(ItemDefinition definition, GameTime acquiredTime)
        {
            Definition    = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentGrade  = definition.BaseGrade;
            PurchasePrice = definition.BaseBuyPrice;
            AcquiredTime  = acquiredTime;
            InstanceId    = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Creates a new instance with an explicitly supplied grade and purchase
        /// price. Use this constructor when the grade has been rolled (e.g. for
        /// a crate purchase) or when restoring from a save snapshot.
        /// </summary>
        /// <param name="definition">
        /// The <see cref="ItemDefinition"/> that describes this item type. Must
        /// not be <c>null</c>.
        /// </param>
        /// <param name="grade">The starting grade for this instance.</param>
        /// <param name="purchasePrice">The actual price paid for this instance.</param>
        /// <param name="acquiredTime">
        /// The current in-game time at the moment of acquisition.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="definition"/> is <c>null</c>.
        /// </exception>
        public ItemInstance(ItemDefinition definition, ItemGrade grade, float purchasePrice, GameTime acquiredTime)
        {
            Definition    = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentGrade  = grade;
            PurchasePrice = purchasePrice;
            AcquiredTime  = acquiredTime;
            InstanceId    = Guid.NewGuid().ToString();
        }

        // ── Internal restore constructor ──────────────────────────────────────────

        /// <summary>
        /// Restores an instance from a save snapshot, preserving the original
        /// <see cref="InstanceId"/>. For internal use by
        /// <see cref="ItemSaveData.Restore"/> only.
        /// </summary>
        internal ItemInstance(string instanceId, ItemDefinition definition, ItemGrade grade, float purchasePrice, GameTime acquiredTime)
        {
            InstanceId    = instanceId ?? Guid.NewGuid().ToString();
            Definition    = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentGrade  = grade;
            PurchasePrice = purchasePrice;
            AcquiredTime  = acquiredTime;
        }

        // ── Public methods ────────────────────────────────────────────────────────

        /// <summary>
        /// Applies one step of overnight quality decay, lowering
        /// <see cref="CurrentGrade"/> by one tier. Grade will not fall below
        /// <see cref="ItemGrade.F"/>. Called once per overnight pass for all
        /// perishable items.
        /// </summary>
        public void ApplyOvernightDecay()
        {
            CurrentGrade = CurrentGrade.Decay();
        }

        // ── Object overrides ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns a human-readable summary, e.g.
        /// <c>"[Salmon Onigiri | Grade: A | ¥180]"</c>.
        /// </summary>
        public override string ToString()
        {
            string name  = Definition != null ? Definition.DisplayName : "Unknown";
            string grade = CurrentGrade.ToDisplayString();
            return $"[{name} | Grade: {grade} | ¥{PurchasePrice}]";
        }
    }
}
