namespace ThatGameJam.Features.DoorGate.Events
{
    public struct DoorStateChangedEvent
    {
        public string DoorId;
        public bool IsOpen;
    }
}
