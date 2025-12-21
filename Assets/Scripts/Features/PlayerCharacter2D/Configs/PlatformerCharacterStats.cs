using UnityEngine;

namespace ThatGameJam.Features.PlayerCharacter2D.Configs
{
    [CreateAssetMenu(
        menuName = "ThatGameJam/Player Character 2D/Platformer Character Stats",
        fileName = "PlatformerCharacterStats")]
    public class PlatformerCharacterStats : ScriptableObject
    {
        [Header("Layers")]
        [Tooltip("Set this to the layer your player is on")]
        public LayerMask PlayerLayer;

        [Header("Input")]
        [Tooltip("Snaps input to integers for consistent keyboard/gamepad behavior.")]
        public bool SnapInput = true;

        [Range(0.01f, 0.99f)]
        public float VerticalDeadZoneThreshold = 0.3f;

        [Range(0.01f, 0.99f)]
        public float HorizontalDeadZoneThreshold = 0.1f;

        [Header("Movement")]
        [Tooltip("The top horizontal movement speed")]
        public float MaxSpeed = 14f;

        [Tooltip("The player's capacity to gain horizontal speed")]
        public float Acceleration = 120f;

        [Tooltip("The pace at which the player comes to a stop")]
        public float GroundDeceleration = 60f;

        [Tooltip("Deceleration in air only after stopping input mid-air")]
        public float AirDeceleration = 30f;

        [Tooltip("A constant downward force applied while grounded. Helps on slopes")]
        [Range(0f, -10f)]
        public float GroundingForce = -1.5f;

        [Tooltip("The detection distance for grounding and roof detection")]
        [Range(0f, 0.5f)]
        public float GrounderDistance = 0.05f;

        [Header("Jump")]
        [Tooltip("The immediate velocity applied when jumping")]
        public float JumpPower = 36f;

        [Tooltip("The maximum vertical movement speed")]
        public float MaxFallSpeed = 40f;

        [Tooltip("The player's capacity to gain fall speed (in-air gravity)")]
        public float FallAcceleration = 110f;

        [Tooltip("Gravity multiplier added when jump is released early")]
        public float JumpEndEarlyGravityModifier = 3f;

        [Tooltip("The time before coyote jump becomes unusable.")]
        public float CoyoteTime = 0.15f;

        [Tooltip("The amount of time we buffer a jump.")]
        public float JumpBuffer = 0.2f;
    }
}

