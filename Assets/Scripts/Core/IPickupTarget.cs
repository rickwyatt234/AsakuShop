using UnityEngine;

namespace AsakuShop.Core
{

    /// Abstraction over PlayerHands used by interactable world objects (ItemPickup,

    public interface IPickupTarget
    {
        bool IsHoldingInteractable { get; }
        bool IsLookingAtSuitableShelfMountingPosition { get; }
        void TryPickupInteractable(GameObject interactableObject);
        IInteractable GetHeldInteractable();
        Transform GetTransform();
    }


}