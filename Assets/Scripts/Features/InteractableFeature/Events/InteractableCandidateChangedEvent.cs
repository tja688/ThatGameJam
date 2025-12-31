using ThatGameJam.Features.BackpackFeature.Models;
using ThatGameJam.Features.InteractableFeature.Controllers;

namespace ThatGameJam.Features.InteractableFeature.Events
{
    public struct InteractableCandidateChangedEvent
    {
        public bool HasCandidate;
        public InteractableType Type;
        public string DisplayName;
        public ItemDefinition ItemDefinition;
    }
}
