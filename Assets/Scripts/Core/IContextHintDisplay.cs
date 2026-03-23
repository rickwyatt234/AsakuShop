namespace AsakuShop.Core
{
    /// Interface for the context hint display UI, kept in Core so that Input-layer code
    /// can request context hints without importing AsakuShop.UI directly.
    public interface IContextHintDisplay
    {
        void SetContext(string hint);
        string GetContext(IInteractable interactable);
        void HideContext();
    }
}
