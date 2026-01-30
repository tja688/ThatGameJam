namespace ThatGameJam.Features.Shared
{
    public struct LightConsumedEvent
    {
        public float Amount;
        public ELightConsumeReason Reason;
        public object Requester;
    }
}
