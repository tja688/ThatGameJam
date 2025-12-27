namespace ThatGameJam.Features.BellFlower.Events
{
    public struct FlowerActivatedEvent
    {
        public string DoorId;
        public string FlowerId;
        public bool IsActive;
    }
}
