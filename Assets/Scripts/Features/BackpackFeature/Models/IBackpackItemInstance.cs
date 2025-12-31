using UnityEngine;

namespace ThatGameJam.Features.BackpackFeature.Models
{
    public interface IBackpackItemInstance
    {
        ItemDefinition Definition { get; }
        int InstanceId { get; }
        void OnAddedToBackpack();
        void OnSetHeld(Transform holdPoint);
        void OnDropped(Vector3 worldPosition);
        void OnRemovedFromBackpack();
    }
}
