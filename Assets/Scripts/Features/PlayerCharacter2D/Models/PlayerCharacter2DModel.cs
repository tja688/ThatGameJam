using QFramework;
using UnityEngine;

namespace ThatGameJam.Features.PlayerCharacter2D.Models
{
    public class PlayerCharacter2DModel : AbstractModel, IPlayerCharacter2DModel
    {
        public BindableProperty<bool> Grounded { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> IsClimbing { get; } = new BindableProperty<bool>(false);
        public BindableProperty<Vector2> Velocity { get; } = new BindableProperty<Vector2>(Vector2.zero);

        public PlatformerFrameInput FrameInput { get; set; }
        public float Time { get; set; }

        public float FrameLeftGrounded { get; set; } = float.MinValue;
        public bool JumpToConsume { get; set; }
        public bool BufferedJumpUsable { get; set; }
        public bool EndedJumpEarly { get; set; }
        public bool CoyoteUsable { get; set; }
        public float TimeJumpWasPressed { get; set; }

        public float WallContactTimer { get; set; }
        public float RegrabLockoutTimer { get; set; }
        public float ClimbWallSide { get; set; }

        protected override void OnInit()
        {
            // Initial states are already set via property initializers or defaults.
        }
    }
}
