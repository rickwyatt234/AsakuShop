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

    // --- Additions for mountable holdables below ---

    public enum MountMode
    {
        Wall,
        Floor,
        Either
    }

    [System.Flags]
    public enum MountSurfaceMask
    {
        None = 0,
        Ground = 1 << 0,
        Wall = 1 << 1,
        Any = Ground | Wall
    }

    public interface IMountableHoldable : IHoldable
    {
        MountMode MountMode { get; }
        MountSurfaceMask AllowedSurfaces { get; }
        bool AlignToSurfaceNormal { get; }
        bool AllowManualYawRotation { get; }
        void NotifyMounted();
        void NotifyPickedUp();
    }
}