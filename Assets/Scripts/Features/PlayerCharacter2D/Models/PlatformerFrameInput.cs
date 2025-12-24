using UnityEngine;

namespace ThatGameJam.Features.PlayerCharacter2D.Models
{
    public struct PlatformerFrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public bool GrabHeld;
        public Vector2 Move;
    }

    public interface IPlatformerFrameInputSource
    {
        PlatformerFrameInput ReadInput();
    }
}
