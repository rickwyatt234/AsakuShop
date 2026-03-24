using UnityEngine;

namespace AsakuShop.Core
{

    /// Abstraction over PlayerHands used by interactable world objects (ItemPickup,
    /// StorageContainer, ShelfComponent) to request pickup without depending on AsakuShop.Player.
    /// Removes cyclic dependencies between AsakuShop.Player, AsakuShop.Items, AsakuShop.Storage, AsakuShop.UI, and AsakuShop.Input.
    public interface IPickupTarget
    {
        bool IsHoldingInteractable { get; }
        bool IsLookingAtSuitableShelfMountingPosition { get; }
        void TryPickupInteractable(GameObject interactableObject);
        IInteractable GetHeldInteractable();
    }


}