using QFramework;
using ThatGameJam.Features.PlayerCharacter2D.Configs;
using ThatGameJam.Features.PlayerCharacter2D.Events;
using ThatGameJam.Features.PlayerCharacter2D.Models;
using UnityEngine;

namespace ThatGameJam.Features.PlayerCharacter2D.Commands
{
    public class TickFixedStepCommand : AbstractCommand
    {
        private readonly bool _groundHit;
        private readonly bool _ceilingHit;
        private readonly float _fixedDeltaTime;
        private readonly PlatformerCharacterStats _stats;

        public TickFixedStepCommand(bool groundHit, bool ceilingHit, float fixedDeltaTime, PlatformerCharacterStats stats)
        {
            _groundHit = groundHit;
            _ceilingHit = ceilingHit;
            _fixedDeltaTime = fixedDeltaTime;
            _stats = stats;
        }

        protected override void OnExecute()
        {
            var model = this.GetModel<IPlayerCharacter2DModel>();
            var velocity = model.Velocity.Value;

            // --- Collisions ---
            if (_ceilingHit) velocity.y = Mathf.Min(0, velocity.y);

            if (!model.Grounded.Value && _groundHit)
            {
                model.Grounded.Value = true;
                model.CoyoteUsable = true;
                model.BufferedJumpUsable = true;
                model.EndedJumpEarly = false;

                var impact = Mathf.Abs(velocity.y);
                this.SendEvent(new PlayerGroundedChangedEvent { Grounded = true, ImpactSpeed = impact });
            }
            else if (model.Grounded.Value && !_groundHit)
            {
                model.Grounded.Value = false;
                model.FrameLeftGrounded = model.Time;
                this.SendEvent(new PlayerGroundedChangedEvent { Grounded = false, ImpactSpeed = 0 });
            }

            // --- Jumping ---
            if (!model.EndedJumpEarly && !model.Grounded.Value && !model.FrameInput.JumpHeld && velocity.y > 0)
            {
                model.EndedJumpEarly = true;
            }

            bool hasBufferedJump = model.BufferedJumpUsable && model.Time < model.TimeJumpWasPressed + _stats.JumpBuffer;
            bool canUseCoyote = model.CoyoteUsable && !model.Grounded.Value && model.Time < model.FrameLeftGrounded + _stats.CoyoteTime;

            if (model.JumpToConsume || hasBufferedJump)
            {
                if (model.Grounded.Value || canUseCoyote)
                {
                    // Execute Jump
                    model.EndedJumpEarly = false;
                    model.TimeJumpWasPressed = 0;
                    model.BufferedJumpUsable = false;
                    model.CoyoteUsable = false;
                    velocity.y = _stats.JumpPower;
                    this.SendEvent<PlayerJumpedEvent>();
                }
                model.JumpToConsume = false;
            }

            // --- Horizontal ---
            if (model.FrameInput.Move.x == 0)
            {
                var deceleration = model.Grounded.Value ? _stats.GroundDeceleration : _stats.AirDeceleration;
                velocity.x = Mathf.MoveTowards(velocity.x, 0, deceleration * _fixedDeltaTime);
            }
            else
            {
                velocity.x = Mathf.MoveTowards(
                    velocity.x,
                    model.FrameInput.Move.x * _stats.MaxSpeed,
                    _stats.Acceleration * _fixedDeltaTime);
            }

            // --- Gravity ---
            if (model.Grounded.Value && velocity.y <= 0f)
            {
                velocity.y = _stats.GroundingForce;
            }
            else
            {
                var inAirGravity = _stats.FallAcceleration;
                if (model.EndedJumpEarly && velocity.y > 0)
                    inAirGravity *= _stats.JumpEndEarlyGravityModifier;

                velocity.y = Mathf.MoveTowards(
                    velocity.y,
                    -_stats.MaxFallSpeed,
                    inAirGravity * _fixedDeltaTime);
            }

            model.Velocity.Value = velocity;
        }
    }
}
