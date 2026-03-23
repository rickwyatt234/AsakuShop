using UnityEngine;

namespace AsakuShop.Core
{
    /// Implemented by world objects that can be picked up and held by the player
    /// (e.g. StorageContainer, ShelfComponent). Provides the data PlayerHands needs
    /// without PlayerHands importing AsakuShop.Storage directly.
    public interface IHoldable
    {
        string DisplayName { get; }
        Vector3 HeldOffset { get; }
        Quaternion HeldRotation { get; }
    }
}
