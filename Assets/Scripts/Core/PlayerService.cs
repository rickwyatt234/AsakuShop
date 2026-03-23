namespace AsakuShop.Core
{
    /// Lightweight service locator for the player's pickup target.
    /// PlayerHands registers itself here in Awake(). Storage and Items classes
    /// read PickupTarget from here instead of importing AsakuShop.Player.
    public static class PlayerService
    {
        public static IPickupTarget PickupTarget { get; set; }
    }
}
