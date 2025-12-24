using System;
using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.DeathRespawn.Controllers;
using UnityEngine;

// 下面两个 using 的 namespace 可能需要你按项目实际路径微调：
// - ConsumeLightCommand
// - ELightConsumeReason
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.LightVitality;
using ThatGameJam.Features.Shared;

namespace ThatGameJam.Features.Mechanisms.Controllers
{
    /// <summary>
    /// Ghost mechanism:
    /// - Moves inside a rectangular bounds without physics collision blocking.
    /// - Fixed half-cycle duration (e.g., 3s) from left to right, then right to left.
    /// - B: Generates waypoints per half-cycle and uses Catmull-Rom spline.
    /// - A: Adds subtle Perlin noise on top (non-repetitive).
    /// - C: Applies floaty following (SmoothDamp) so it feels "light".
    /// - Contact trigger drains player's light faster by sending ConsumeLightCommand continuously.
    /// </summary>
    [DisallowMultipleComponent]
    public class GhostMechanism2D : MechanismControllerBase
    {
        [Header("Bounds (Rect)")]
        [Tooltip("Optional: use a BoxCollider2D as the movement bounds provider (scene gizmo friendly).")]
        [SerializeField] private BoxCollider2D boundsCollider;

        [Tooltip("If boundsCollider is null, use this local rect as movement bounds (center + size in local space).")]
        [SerializeField] private Vector2 localBoundsCenter = Vector2.zero;

        [SerializeField] private Vector2 localBoundsSize = new Vector2(8f, 4f);

        [Tooltip("Keep the spline safely inside bounds (world units).")]
        [SerializeField] private float boundsPadding = 0.25f;

        [Header("Timing (Hard Constraint)")]
        [Tooltip("Seconds to go from left edge to right edge (half cycle).")]
        [SerializeField] private float halfCycleDuration = 3f;

        [Tooltip("Movement easing along the half cycle (S-curve gives floaty start/stop).")]
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Path (B: Waypoints + Spline)")]
        [Tooltip("Waypoints count per half cycle (4~6 recommended).")]
        [Range(3, 10)]
        [SerializeField] private int waypointsPerHalf = 5;

        [Tooltip("How much X jitter per waypoint (as fraction of bounds width).")]
        [Range(0f, 0.3f)]
        [SerializeField] private float waypointXJitter01 = 0.12f;

        [Tooltip("How much Y range for waypoints (as fraction of bounds height).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float waypointYRange01 = 0.25f;

        [Tooltip("Limit adjacent waypoint delta Y (as fraction of bounds height).")]
        [Range(0f, 0.8f)]
        [SerializeField] private float waypointMaxDeltaY01 = 0.25f;

        [Header("Detail (A: Subtle Noise)")]
        [Tooltip("Extra Y noise amplitude (as fraction of bounds height). Keep small to avoid obvious wave.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float noiseAmpY01 = 0.06f;

        [Tooltip("Extra X micro noise amplitude (as fraction of bounds width).")]
        [Range(0f, 0.1f)]
        [SerializeField] private float noiseAmpX01 = 0.01f;

        [Tooltip("Noise frequency across the half cycle.")]
        [SerializeField] private float noiseFreq = 1.6f;

        [Header("Floaty Follow (C)")]
        [Tooltip("Smoothing time for following the target position. Smaller = snappier, larger = floatier.")]
        [Range(0.01f, 1.0f)]
        [SerializeField] private float followSmoothTime = 0.18f;

        [Tooltip("Clamp max follow speed (world units/sec).")]
        [SerializeField] private float followMaxSpeed = 999f;

        [Header("Light Drain On Contact")]
        [Tooltip("Trigger collider used to detect player contact. If null, will try GetComponent<Collider2D>(). Must be isTrigger.")]
        [SerializeField] private Collider2D contactTrigger;

        [Tooltip("Extra light consumed per second while player overlaps the ghost trigger.")]
        [SerializeField] private float extraConsumePerSecond = 8f;

        [Tooltip("Consume reason sent to LightVitality.")]
        [SerializeField] private ELightConsumeReason consumeReason = ELightConsumeReason.Script;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private bool drawPathPreview = true;
        [Range(8, 128)]
        [SerializeField] private int previewSamples = 32;

        private readonly List<Vector2> _waypoints = new();
        private bool _movingRight = true;
        private float _halfElapsed;

        private Vector2 _currentPos;
        private Vector2 _followVelocity;

        private int _playerOverlapCount;
        private float _seedA;
        private float _seedB;

        private float _z;

        private void Awake()
        {
            if (contactTrigger == null)
            {
                contactTrigger = GetComponent<Collider2D>();
            }

            if (contactTrigger != null && !contactTrigger.isTrigger)
            {
                LogKit.W("GhostMechanism2D expects contactTrigger.isTrigger = true.");
            }

            if (halfCycleDuration <= 0f)
            {
                halfCycleDuration = 0.01f;
            }

            _z = transform.position.z;
            _currentPos = transform.position;

            // Seed once, will also re-seed every half cycle.
            _seedA = UnityEngine.Random.Range(0f, 1000f);
            _seedB = UnityEngine.Random.Range(0f, 1000f);

            // Start at left edge heading right by default.
            BeginHalfCycle(forceResetToEdge: true);
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f) return;

            StepMovement(dt);
            StepDrain(dt);
        }

        private void StepMovement(float dt)
        {
            _halfElapsed += dt;
            var t = Mathf.Clamp01(_halfElapsed / halfCycleDuration);
            var eased = moveCurve != null ? moveCurve.Evaluate(t) : t;

            var bounds = GetWorldBounds();
            if (!bounds.IsValid)
            {
                return;
            }

            var target = EvaluateSpline(_waypoints, eased);

            // A: subtle noise (non-repeating-ish): depends on Time and per-half seeds.
            var width = bounds.Width;
            var height = bounds.Height;

            var timeTerm = Time.time * 0.37f;
            var nX = Mathf.PerlinNoise(_seedA + timeTerm, _seedB + eased * noiseFreq) - 0.5f;
            var nY = Mathf.PerlinNoise(_seedB + timeTerm, _seedA + eased * noiseFreq) - 0.5f;

            target.x += nX * (width * noiseAmpX01);
            target.y += nY * (height * noiseAmpY01);

            // Keep inside bounds (final safety clamp).
            target = ClampToBounds(target, bounds);

            // C: floaty follow (critically damped). Good enough for "light" feeling.
            _currentPos = Vector2.SmoothDamp(
                _currentPos,
                target,
                ref _followVelocity,
                followSmoothTime,
                followMaxSpeed,
                dt);

            // Clamp again after follow to avoid overshoot leaving bounds.
            _currentPos = ClampToBounds(_currentPos, bounds);

            transform.position = new Vector3(_currentPos.x, _currentPos.y, _z);

            if (_halfElapsed >= halfCycleDuration)
            {
                _movingRight = !_movingRight;
                BeginHalfCycle(forceResetToEdge: false);
            }
        }

        private void StepDrain(float dt)
        {
            if (_playerOverlapCount <= 0)
            {
                return;
            }

            if (extraConsumePerSecond <= 0f)
            {
                return;
            }

            var amount = extraConsumePerSecond * dt;
            // Per LightVitality note, other features integrate by sending ConsumeLightCommand. :contentReference[oaicite:2]{index=2}
            this.SendCommand(new ConsumeLightCommand(amount, consumeReason));
        }

        private void BeginHalfCycle(bool forceResetToEdge)
        {
            _halfElapsed = 0f;

            // Re-seed per half cycle to avoid obvious repetition.
            _seedA = UnityEngine.Random.Range(0f, 1000f);
            _seedB = UnityEngine.Random.Range(0f, 1000f);

            var bounds = GetWorldBounds();
            if (!bounds.IsValid)
            {
                _waypoints.Clear();
                return;
            }

            var leftX = bounds.Min.x;
            var rightX = bounds.Max.x;

            // Decide start/end edge by direction.
            var startX = _movingRight ? leftX : rightX;
            var endX = _movingRight ? rightX : leftX;

            var centerY = (bounds.Min.y + bounds.Max.y) * 0.5f;

            // Optionally snap to edge at first start (so it "patrols" cleanly).
            if (forceResetToEdge)
            {
                _currentPos = new Vector2(startX, centerY);
                _followVelocity = Vector2.zero;
                transform.position = new Vector3(_currentPos.x, _currentPos.y, _z);
            }

            GenerateWaypoints(bounds, startX, endX);
        }

        private void GenerateWaypoints(WorldBounds b, float startX, float endX)
        {
            _waypoints.Clear();

            var width = b.Width;
            var height = b.Height;

            var xJitter = width * waypointXJitter01;
            var yRange = height * waypointYRange01;
            var maxDeltaY = height * waypointMaxDeltaY01;

            var count = Mathf.Max(3, waypointsPerHalf);

            // Build monotonic X list (start -> end) with jitter, then sort by direction.
            var xs = new float[count];
            for (var i = 0; i < count; i++)
            {
                var u = count == 1 ? 0f : (float)i / (count - 1);
                var x = Mathf.Lerp(startX, endX, u);

                // Do not jitter endpoints, only middle points.
                if (i != 0 && i != count - 1)
                {
                    x += UnityEngine.Random.Range(-xJitter, xJitter);
                }

                xs[i] = x;
            }

            // Ensure monotonic.
            if (_movingRight)
            {
                Array.Sort(xs);
            }
            else
            {
                Array.Sort(xs);
                Array.Reverse(xs);
            }

            // Y with "inertia": constrain delta between adjacent points.
            var y = Mathf.Clamp(_currentPos.y, b.Min.y, b.Max.y);
            for (var i = 0; i < count; i++)
            {
                var targetY = UnityEngine.Random.Range(-yRange, yRange) + b.Center.y;

                if (i == 0)
                {
                    // Start near current Y (looks like it's continuing its float).
                    targetY = y;
                }
                else
                {
                    var dy = Mathf.Clamp(targetY - y, -maxDeltaY, maxDeltaY);
                    targetY = y + dy;
                }

                targetY = Mathf.Clamp(targetY, b.Min.y, b.Max.y);

                y = targetY;
                _waypoints.Add(new Vector2(xs[i], y));
            }

            // Final clamp all points (paranoia).
            for (var i = 0; i < _waypoints.Count; i++)
            {
                _waypoints[i] = ClampToBounds(_waypoints[i], b);
            }
        }

        private Vector2 EvaluateSpline(List<Vector2> pts, float t01)
        {
            if (pts == null || pts.Count == 0)
            {
                return _currentPos;
            }

            if (pts.Count == 1)
            {
                return pts[0];
            }

            // Catmull-Rom over segments between pts[i] and pts[i+1]
            var segmentCount = pts.Count - 1;
            var f = Mathf.Clamp01(t01) * segmentCount;
            var seg = Mathf.Clamp(Mathf.FloorToInt(f), 0, segmentCount - 1);
            var u = f - seg;

            var p0 = pts[Mathf.Clamp(seg - 1, 0, pts.Count - 1)];
            var p1 = pts[seg];
            var p2 = pts[seg + 1];
            var p3 = pts[Mathf.Clamp(seg + 2, 0, pts.Count - 1)];

            return CatmullRom(p0, p1, p2, p3, u);
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            // Standard Catmull-Rom (centripetal variant not needed here; keep it simple).
            var t2 = t * t;
            var t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private Vector2 ClampToBounds(Vector2 p, WorldBounds b)
        {
            return new Vector2(
                Mathf.Clamp(p.x, b.Min.x, b.Max.x),
                Mathf.Clamp(p.y, b.Min.y, b.Max.y)
            );
        }

        private WorldBounds GetWorldBounds()
        {
            if (boundsCollider != null)
            {
                var bb = boundsCollider.bounds;
                var min = (Vector2)bb.min + Vector2.one * boundsPadding;
                var max = (Vector2)bb.max - Vector2.one * boundsPadding;
                return WorldBounds.FromMinMax(min, max);
            }

            // Local rect -> world rect (assume no rotation; if rotated, gizmo still ok but bounds become approximate).
            var size = new Vector2(Mathf.Abs(localBoundsSize.x), Mathf.Abs(localBoundsSize.y));
            var centerLocal = localBoundsCenter;

            var centerWorld = (Vector2)transform.TransformPoint(centerLocal);
            var half = size * 0.5f;

            var minW = centerWorld - half + Vector2.one * boundsPadding;
            var maxW = centerWorld + half - Vector2.one * boundsPadding;
            return WorldBounds.FromMinMax(minW, maxW);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
            {
                return;
            }

            _playerOverlapCount++;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
            {
                return;
            }

            _playerOverlapCount = Mathf.Max(0, _playerOverlapCount - 1);
        }

        private bool IsPlayerCollider(Collider2D other)
        {
            // Follow SpikeHazard2D convention: if collider belongs to something with DeathController in parent, treat as player. :contentReference[oaicite:3]{index=3}
            return other != null && other.GetComponentInParent<DeathController>() != null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            var b = GetWorldBounds();
            if (!b.IsValid) return;

            Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireCube(b.Center, new Vector3(b.Width, b.Height, 0f));

            if (!drawPathPreview) return;
            if (_waypoints == null || _waypoints.Count < 2) return;

            Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
            var prev = (Vector3)EvaluateSpline(_waypoints, 0f);
            for (var i = 1; i <= previewSamples; i++)
            {
                var t = (float)i / previewSamples;
                var p = (Vector3)EvaluateSpline(_waypoints, t);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }

            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.75f);
            foreach (var wp in _waypoints)
            {
                Gizmos.DrawSphere(wp, 0.06f);
            }
        }

        [Serializable]
        private struct WorldBounds
        {
            public Vector2 Min;
            public Vector2 Max;

            public Vector2 Center => (Min + Max) * 0.5f;
            public float Width => Mathf.Max(0f, Max.x - Min.x);
            public float Height => Mathf.Max(0f, Max.y - Min.y);
            public bool IsValid => Width > 0.0001f && Height > 0.0001f;

            public static WorldBounds FromMinMax(Vector2 min, Vector2 max)
            {
                // Ensure min <= max
                var mn = new Vector2(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y));
                var mx = new Vector2(Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
                return new WorldBounds { Min = mn, Max = mx };
            }
        }
    }
}
