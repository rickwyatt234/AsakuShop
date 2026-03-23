using UnityEngine;

namespace AsakuShop.Core
{
    // Implemented by world objects that can be picked up and held by the player
    // (e.g. StorageContainer, ShelfComponent). Provides the data PlayerHands needs
    // without PlayerHands importing AsakuShop.Storage directly.
    public interface IHoldable
    {
        string DisplayName { get; }
        Vector3 HeldOffset { get; }
        Quaternion HeldRotation { get; }

        //The Unity GameObject this holdable lives on.
        GameObject GameObject { get; }
    }

    // Extended interface for shelf-type holdables that can be wall-mounted.
    // PlayerHands uses this to access shelf-specific positioning without knowing ShelfComponent.
    public interface IShelfHoldable : IHoldable
    {
        Vector3 RotationOffset { get; }
        float MountOffsetDistance { get; }
    }
}
