namespace ThatGameJam.Features.DoorGate.Models
{
    public struct DoorGateState
    {
        public string DoorId;
        public int RequiredFlowerCount;
        public int ActiveFlowerCount;
        public bool IsOpen;
        public bool AllowCloseOnDeactivate;
        public bool StartOpen;
    }
}
