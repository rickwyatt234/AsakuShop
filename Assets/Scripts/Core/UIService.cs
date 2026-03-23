namespace AsakuShop.Core
{
    /// Lightweight service locator for UI components that lower-layer code needs to call.
    /// ContextHintDisplay registers itself here in Awake() so that Input-layer code can
    /// call context hint methods without importing AsakuShop.UI.
    public static class UIService
    {
        public static IContextHintDisplay ContextHint { get; set; }
    }
}
