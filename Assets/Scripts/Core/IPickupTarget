using UnityEngine;

namespace AsakuShop.Core
{
    /// Abstraction over PlayerHands used by interactable world objects (ItemPickup,
    /// StorageContainer, ShelfComponent) to request pickup without depending on AsakuShop.Player.
    /// Removes Cyclic Dependencies between
    public interface IPickupTarget
    {
        bool IsHoldingInteractable { get; }
        bool IsLookingAtSuitableShelfMountingPosition { get; }
        void TryPickupInteractable(GameObject interactableObject);
    }
}
