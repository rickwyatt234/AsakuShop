
namespace AsakuShop.Core
{
    /// Lightweight service locator for the player's pickup target and input manager.
    /// PlayerHands registers itself here in Awake(). Storage, Items, and Store classes
    /// read from here instead of importing AsakuShop.Player or AsakuShop.Input directly.
    public static class PlayerService
    {
        public static IPickupTarget PickupTarget { get; set; }

        /// The active input manager. Registered by PlayerHands on Awake.
        /// Store components (e.g. CheckoutCounter) use this to lock/unlock player movement
        /// without taking a hard dependency on AsakuShop.Input.
        public static IInputManager InputManager { get; set; }
    }
}
