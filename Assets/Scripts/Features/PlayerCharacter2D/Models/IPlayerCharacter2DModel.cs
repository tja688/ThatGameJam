using QFramework;
using UnityEngine;

namespace ThatGameJam.Features.PlayerCharacter2D.Models
{
    public interface IPlayerCharacter2DModel : IModel
    {
        BindableProperty<bool> Grounded { get; }
        BindableProperty<bool> IsClimbing { get; }
        BindableProperty<Vector2> Velocity { get; }

        PlatformerFrameInput FrameInput { get; set; }
        float Time { get; set; }

        float FrameLeftGrounded { get; set; }
        bool JumpToConsume { get; set; }
        bool BufferedJumpUsable { get; set; }
        bool EndedJumpEarly { get; set; }
        bool CoyoteUsable { get; set; }
        float TimeJumpWasPressed { get; set; }

        float WallContactTimer { get; set; }
        float RegrabLockoutTimer { get; set; }
        float ClimbWallSide { get; set; }
    }
}
