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
        private readonly bool _wallDetected;
        private readonly float _wallSideSign;
        private readonly bool _wallHorizontal;
        private readonly string _wallColliderName;
        private readonly string _wallColliderTag;
        private readonly int _wallColliderLayer;
        private readonly float _fixedDeltaTime;
        private readonly PlatformerCharacterStats _stats;

        public TickFixedStepCommand(
            bool groundHit,
            bool ceilingHit,
            bool wallDetected,
            float wallSideSign,
            bool wallHorizontal,
            string wallColliderName,
            string wallColliderTag,
            int wallColliderLayer,
            float fixedDeltaTime,
            PlatformerCharacterStats stats)
        {
            _groundHit = groundHit;
            _ceilingHit = ceilingHit;
            _wallDetected = wallDetected;
            _wallSideSign = wallSideSign;
            _wallHorizontal = wallHorizontal;
            _wallColliderName = wallColliderName;
            _wallColliderTag = wallColliderTag;
            _wallColliderLayer = wallColliderLayer;
            _fixedDeltaTime = fixedDeltaTime;
            _stats = stats;
        }

        protected override void OnExecute()
        {
            var model = this.GetModel<IPlayerCharacter2DModel>();
            var velocity = model.Velocity.Value;
            var wasClimbing = model.IsClimbing.Value;

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

            if (model.RegrabLockoutTimer > 0f)
            {
                model.RegrabLockoutTimer = Mathf.Max(0f, model.RegrabLockoutTimer - _fixedDeltaTime);
            }

            var wantsClimb = model.FrameInput.GrabHeld
                             && model.RegrabLockoutTimer <= 0f;

            if (model.IsClimbing.Value)
            {
                if (_wallDetected)
                {
                    model.WallContactTimer = _stats.WallCoyoteTime;
                    model.ClimbIsHorizontal = _wallHorizontal;
                    if (_wallSideSign != 0f)
                    {
                        if (model.ClimbWallSide == 0f || Mathf.Sign(model.ClimbWallSide) == Mathf.Sign(_wallSideSign))
                        {
                            model.ClimbWallSide = _wallSideSign;
                        }
                    }
                }
                else
                {
                    model.WallContactTimer = Mathf.Max(0f, model.WallContactTimer - _fixedDeltaTime);
                }

                var lostWall = !_wallDetected && model.WallContactTimer <= 0f;
                if (!wantsClimb || lostWall)
                {
                    model.IsClimbing.Value = false;
                    model.WallContactTimer = 0f;
                    model.ClimbIsHorizontal = false;
                    if (!wantsClimb)
                    {
                        model.RegrabLockoutTimer = Mathf.Max(model.RegrabLockoutTimer, _stats.ClimbRegrabLockout);
                    }
                }
            }
            else
            {
                model.WallContactTimer = 0f;
                if (wantsClimb && _wallDetected)
                {
                    model.IsClimbing.Value = true;
                    model.WallContactTimer = _stats.WallCoyoteTime;
                    model.ClimbIsHorizontal = _wallHorizontal;
                    if (_wallSideSign != 0f)
                    {
                        model.ClimbWallSide = _wallSideSign;
                    }
                    velocity.x = 0f;
                }
            }

            var performedClimbJump = false;
            if (model.IsClimbing.Value)
            {
                if (model.FrameInput.JumpDown)
                {
                    if (model.Grounded.Value)
                    {
                        model.IsClimbing.Value = false;
                        model.WallContactTimer = 0f;
                    }
                    else
                    {
                        model.IsClimbing.Value = false;
                        model.WallContactTimer = 0f;
                        model.ClimbIsHorizontal = false;
                        model.RegrabLockoutTimer = Mathf.Max(model.RegrabLockoutTimer, _stats.ClimbRegrabLockout);

                        model.EndedJumpEarly = false;
                        model.TimeJumpWasPressed = 0f;
                        model.BufferedJumpUsable = false;
                        model.CoyoteUsable = false;
                        model.JumpToConsume = false;

                        velocity.y = _stats.ClimbJumpPower;

                        var detachVelocity = _stats.ClimbJumpDetachVelocity;
                        if (detachVelocity > 0f)
                        {
                            var sideSign = model.ClimbWallSide;
                            if (sideSign == 0f)
                            {
                                sideSign = _wallSideSign;
                            }

                            if (sideSign == 0f)
                            {
                                sideSign = 1f;
                            }

                            if (model.ClimbIsHorizontal)
                            {
                                velocity.y += -Mathf.Sign(sideSign) * detachVelocity;
                            }
                            else
                            {
                                velocity.x = -Mathf.Sign(sideSign) * detachVelocity;
                            }
                        }
                        else
                        {
                            velocity.x = 0f;
                        }

                        this.SendEvent<PlayerJumpedEvent>();
                        performedClimbJump = true;
                    }
                }

                if (model.IsClimbing.Value)
                {
                    var isHorizontal = model.ClimbIsHorizontal;
                    var move = model.FrameInput.Move;
                    var mainInput = isHorizontal ? move.x : move.y;
                    var secondaryInput = isHorizontal ? move.y : move.x;

                    var mainVelocity = 0f;
                    if (mainInput > 0f)
                    {
                        mainVelocity = _stats.ClimbUpSpeed;
                    }
                    else if (mainInput < 0f)
                    {
                        mainVelocity = -Mathf.Min(_stats.ClimbDownSpeed, _stats.MaxFallSpeed);
                    }
                    else
                    {
                        mainVelocity = model.Grounded.Value
                            ? 0f
                            : -Mathf.Min(_stats.ClimbIdleSlideSpeed, _stats.MaxFallSpeed);
                    }

                    var secondaryVelocity = 0f;
                    if (_stats != null && !_stats.ClimbLockSecondaryAxis)
                    {
                        var up = _stats.ClimbUpSpeed * _stats.ClimbSecondaryAxisMultiplier;
                        var down = _stats.ClimbDownSpeed * _stats.ClimbSecondaryAxisMultiplier;
                        if (secondaryInput > 0f)
                        {
                            secondaryVelocity = up;
                        }
                        else if (secondaryInput < 0f)
                        {
                            secondaryVelocity = -Mathf.Min(down, _stats.MaxFallSpeed);
                        }
                    }

                    if (_ceilingHit)
                    {
                        if (isHorizontal)
                        {
                            if (secondaryVelocity > 0f)
                            {
                                secondaryVelocity = 0f;
                            }
                        }
                        else if (mainVelocity > 0f)
                        {
                            mainVelocity = 0f;
                        }
                    }

                    if (isHorizontal)
                    {
                        velocity.x = mainVelocity;
                        velocity.y = _stats.ClimbLockSecondaryAxis ? 0f : secondaryVelocity;
                    }
                    else
                    {
                        velocity.y = mainVelocity;
                        velocity.x = _stats.ClimbLockSecondaryAxis ? 0f : secondaryVelocity;
                    }
                    model.Velocity.Value = velocity;
                }
            }

            var climbStateChanged = wasClimbing != model.IsClimbing.Value;
            if (model.IsClimbing.Value || performedClimbJump)
            {
                if (!model.IsClimbing.Value)
                {
                    model.Velocity.Value = velocity;
                }

                if (climbStateChanged)
                {
                    this.SendEvent(new PlayerClimbStateChangedEvent { IsClimbing = model.IsClimbing.Value });
                    if (_stats != null && _stats.EnableClimbDebugLogs)
                    {
                        var move = model.FrameInput.Move;
                        LogKit.I(
                            $"[PlayerCharacter2D] Climb {wasClimbing}->{model.IsClimbing.Value} " +
                            $"groundHit={_groundHit} ceilingHit={_ceilingHit} wallDetected={_wallDetected} wallSide={_wallSideSign:0.##} " +
                            $"wallHorizontal={model.ClimbIsHorizontal} grabHeld={model.FrameInput.GrabHeld} moveY={move.y:0.##} " +
                            $"wallTimer={model.WallContactTimer:0.###} regrabTimer={model.RegrabLockoutTimer:0.###} " +
                            $"wallCollider={_wallColliderName} tag={_wallColliderTag} layer={_wallColliderLayer}");
                    }
                }

                return;
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

            if (climbStateChanged)
            {
                this.SendEvent(new PlayerClimbStateChangedEvent { IsClimbing = model.IsClimbing.Value });
                if (_stats != null && _stats.EnableClimbDebugLogs)
                {
                    var move = model.FrameInput.Move;
                    LogKit.I(
                        $"[PlayerCharacter2D] Climb {wasClimbing}->{model.IsClimbing.Value} " +
                        $"groundHit={_groundHit} ceilingHit={_ceilingHit} wallDetected={_wallDetected} wallSide={_wallSideSign:0.##} " +
                        $"wallHorizontal={model.ClimbIsHorizontal} grabHeld={model.FrameInput.GrabHeld} moveY={move.y:0.##} " +
                        $"wallTimer={model.WallContactTimer:0.###} regrabTimer={model.RegrabLockoutTimer:0.###} " +
                        $"wallCollider={_wallColliderName} tag={_wallColliderTag} layer={_wallColliderLayer}");
                }
            }
        }
    }
}
