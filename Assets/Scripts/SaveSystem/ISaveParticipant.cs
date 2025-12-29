namespace ThatGameJam.SaveSystem
{
    public interface ISaveParticipant
    {
        string SaveKey { get; }
        int LoadOrder { get; }
        string CaptureToJson();
        void RestoreFromJson(string json);
    }
}
