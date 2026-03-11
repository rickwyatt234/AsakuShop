namespace AsakuShop.Core
{
    public interface ISaveParticipant
    {
        string SaveKey { get; }
        object CaptureState();
        void RestoreState(string json);
    }
}