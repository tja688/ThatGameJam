using System.Collections.Generic;
using QFramework;

namespace ThatGameJam.Features.DoorGate.Models
{
    public class DoorGateModel : AbstractModel, IDoorGateModel
    {
        private readonly Dictionary<string, DoorGateState> _doors = new Dictionary<string, DoorGateState>();

        internal bool TryGetDoor(string doorId, out DoorGateState state) => _doors.TryGetValue(doorId, out state);

        internal void RegisterDoor(DoorGateState state)
        {
            if (string.IsNullOrEmpty(state.DoorId))
            {
                return;
            }

            _doors[state.DoorId] = state;
        }

        internal void UnregisterDoor(string doorId)
        {
            if (string.IsNullOrEmpty(doorId))
            {
                return;
            }

            _doors.Remove(doorId);
        }

        internal void UpdateDoor(DoorGateState state)
        {
            if (string.IsNullOrEmpty(state.DoorId))
            {
                return;
            }

            _doors[state.DoorId] = state;
        }

        internal List<DoorGateState> GetAllDoors()
        {
            var list = new List<DoorGateState>(_doors.Count);
            foreach (var state in _doors.Values)
            {
                list.Add(state);
            }

            return list;
        }

        protected override void OnInit()
        {
            _doors.Clear();
        }
    }
}
