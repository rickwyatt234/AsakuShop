namespace AsakuShop.Core
{
    /// <summary>
    /// Implemented by any game system that wants to participate in the save/load
    /// cycle managed by <see cref="SaveManager"/>. Register via
    /// <see cref="SaveManager.Register"/> during <c>Awake</c> or <c>OnEnable</c>.
    /// </summary>
    public interface ISaveParticipant
    {
        /// <summary>
        /// A globally unique string key used to identify this system's data
        /// within the <see cref="SaveData.SystemData"/> dictionary.
        /// </summary>
        string SaveKey { get; }

        /// <summary>
        /// Called by <see cref="SaveManager"/> just before writing to disk.
        /// Return a serialisable object (plain C# class or struct with only
        /// primitive / string / array fields) representing this system's current
        /// state.
        /// </summary>
        /// <returns>A serialisable state snapshot.</returns>
        object CaptureState();

        /// <summary>
        /// Called by <see cref="SaveManager"/> after the save file has been
        /// read and deserialised. Apply the provided <paramref name="json"/> to
        /// restore this system to its saved state. Use
        /// <c>JsonUtility.FromJson&lt;TState&gt;(json)</c> (or equivalent) to
        /// deserialise back to your concrete state type.
        /// </summary>
        /// <param name="json">
        /// The JSON string previously produced by serialising the object returned
        /// from <see cref="CaptureState"/>. Will never be null or empty when
        /// called by <see cref="SaveManager"/>.
        /// </param>
        void RestoreState(string json);
    }
}
