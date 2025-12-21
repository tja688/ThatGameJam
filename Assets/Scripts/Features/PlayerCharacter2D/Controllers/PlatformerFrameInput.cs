using UnityEngine;

namespace ThatGameJam.Features.PlayerCharacter2D.Controllers
{
    public struct PlatformerFrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
    }

    public interface IPlatformerFrameInputSource
    {
        PlatformerFrameInput ReadInput();
    }
}

