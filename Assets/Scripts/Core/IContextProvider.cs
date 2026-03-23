namespace AsakuShop.Core
{
    // Implemented by any world object that wants to supply context-sensitive
    // interaction hints to the UI layer (e.g. ContextHintDisplay).
    // Lives in Core so that UI can depend on it without referencing Storage or Player directly.
    public interface IContextProvider
    {
        // Hint shown when the player looks at this object while empty-handed.
        string GetHoverHint();

        // Hint shown when the player looks at this object while already holding something.
        // The IPickupTarget parameter lets the implementer inspect what is being held
        // (e.g. "Press [E] to stock" only if the player is holding an item, not a shelf).
        string GetHeldHint(IPickupTarget holder);
    }
}
