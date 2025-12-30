using QFramework;
using ThatGameJam.Independents.Audio;
using ThatGameJam.Features.KeroseneLamp.Models;
using ThatGameJam.Features.KeroseneLamp.Queries;
using UnityEngine;

namespace ThatGameJam.Features.BugAI.Controllers
{
    public class BugMovementBase : MonoBehaviour, IController
    {
        private enum BugState
        {
            Loiter,
            ChaseLight,
            ReturnHome
        }

        private enum FacingAxis
        {
            Right,
            Up,
            Left,
            Down
        }

        private enum LoiterFacingMode
        {
            /// <summary>旧行为：巢内只应用 Overlay，不叠加方向角（等价永远朝右）。</summary>
            OverlayOnly = 0,
            /// <summary>巢内固定朝向（比如朝上）。</summary>
            FixedDirection = 1,
            /// <summary>巢内也跟随移动方向。</summary>
            FaceMovement = 2
        }

        [Header("References")]
        [SerializeField, Tooltip("可选：用于移动的 Rigidbody2D（为空时将直接改 Transform 位置）。")]
        private Rigidbody2D body;
        [SerializeField, Tooltip("活动区域的 Collider2D（虫子不会移动到此范围外）。")]
        private Collider2D moveBounds;
        [SerializeField, Tooltip("巢穴范围的 Collider2D（用于巢内乱飞与回巢判定）。")]
        private Collider2D homeBounds;
        [SerializeField, Tooltip("巢穴参考点（为空则使用 Home Center 的数值）。")]
        private Transform homeAnchor;
        [SerializeField, Tooltip("是否使用 Rigidbody2D 进行移动。")]
        private bool useRigidbody = true;
        [SerializeField, Tooltip("是否根据移动方向旋转朝向。")]
        private bool faceMovement = true;
        [SerializeField, Tooltip("启动时用当前坐标覆盖 Home Center。")]
        private bool overrideHomeCenterOnAwake = true;
        [SerializeField, Tooltip("虫子正朝向轴（用于对齐移动方向）。")]
        private FacingAxis facingAxis = FacingAxis.Right;
        [SerializeField, Tooltip("正朝向叠加偏移角度（度）。")]
        private float facingAngleOffset = 0f;

        [Header("Facing (Visual)")]
        [SerializeField, Tooltip("巢内（Loiter）朝向模式。FixedDirection 可用于“回巢后默认朝上”。")]
        private LoiterFacingMode loiterFacingMode = LoiterFacingMode.FixedDirection;
        [SerializeField, Tooltip("当 LoiterFacingMode=FixedDirection 时，巢内固定朝向的世界轴。")]
        private FacingAxis loiterFixedFacingAxis = FacingAxis.Up;

        [SerializeField, Tooltip("是否对“最终旋转”做平滑过渡（只影响视觉朝向，不影响移动）。")]
        private bool smoothFacing = true;
        [SerializeField, Tooltip("视觉朝向平滑速度（度/秒）。<=0 则为硬切。")]
        private float facingSmoothSpeed = 720f;

        [Header("Home")]
        [SerializeField, Tooltip("巢穴中心点（homeBounds 为空时使用）。")]
        private Vector2 homeCenter;
        [SerializeField, Tooltip("进入巢穴判定半径（homeBounds 为空时使用）。")]
        private float homeEnterRadius = 0.3f;
        [SerializeField, Tooltip("离开巢穴判定半径（应大于进入半径，homeBounds 为空时使用）。")]
        private float homeExitRadius = 0.5f;

        [Header("Loiter")]
        [SerializeField, Tooltip("巢内乱飞移动速度。")]
        private float loiterSpeed = 1.2f;
        [SerializeField, Tooltip("巢内乱飞目标点刷新间隔。")]
        private float loiterTargetInterval = 1.2f;
        [SerializeField, Tooltip("接近目标点的判定距离。")]
        private float loiterTargetReachDistance = 0.15f;
        [SerializeField, Tooltip("巢内乱飞每次目标点的最大偏移半径。")]
        private float loiterJitterRadius = 0.4f;

        [Header("Light Scan")]
        [SerializeField, Tooltip("扫描触发器（每次扫描仅在该触发器范围内）。")]
        private Collider2D scanTrigger;
        [SerializeField, Tooltip("扫描半径（scanTrigger 为空时使用）。")]
        private float attentionRadius = 4f;
        [SerializeField, Tooltip("扫描光源的时间间隔（秒）。")]
        private float scanInterval = 3f;
        [SerializeField, Tooltip("放手后重新扫描光源的冷却时间（秒）。")]
        private float releaseScanCooldown = 5f;

        [Header("Movement")]
        [SerializeField, Tooltip("移动方向转向速度（度/秒）。")]
        private float turnSpeed = 360f;
        [SerializeField, Tooltip("追光移动速度。")]
        private float chaseSpeed = 3f;
        [SerializeField, Tooltip("正常归巢速度。")]
        private float returnSpeed = 1.4f;

        [Header("Light Interaction")]
        [SerializeField, Tooltip("追光停留距离（距离光源中心多少距离时停下）。")]
        private float chaseStopDistance = 0.5f;
        [SerializeField, Tooltip("进入停留距离前开始减速的缓冲距离（距离 = stopDistance + slowingDistance 时开始减速）。")]
        private float chaseSlowDistance = 0.3f;

        private BugState _state = BugState.Loiter;
        private bool _playerGrabbed;
        private bool _isInHome;
        private float _scanTimer;
        private int _trackedLampId = -1;
        private Vector2 _trackedLampPosition;
        private float _releaseCooldownTimer;
        private float _loiterTargetTimer;
        private Vector2 _loiterTarget;
        private Vector2 _desiredDirection;
        private float _currentSpeed;
        private Vector2 _forwardDirection;
        private float _baseRotation;
        private bool _initialized;

        // NEW: 用于视觉朝向平滑
        private float _currentFacingRotation;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        public void NotifyPlayerGrabbed()
        {
            _playerGrabbed = true;
            ClearTrackedLight();
            _scanTimer = 0f;
        }

        public void NotifyPlayerReleased()
        {
            _playerGrabbed = false;
            _releaseCooldownTimer = Mathf.Max(_releaseCooldownTimer, releaseScanCooldown);
            _scanTimer = 0f;
        }

        public void ResetToHome()
        {
            ClearTrackedLight();
            _playerGrabbed = false;
            _releaseCooldownTimer = 0f;
            _scanTimer = 0f;
            SetPositionImmediate(ClampToBounds(GetHomeCenter()));
            TransitionTo(BugState.Loiter, true);

            // 保证重置后视觉朝向从当前角度开始平滑（避免瞬跳）
            _currentFacingRotation = GetCurrentZRotation();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
            }

            _baseRotation = transform.eulerAngles.z;
            _currentFacingRotation = _baseRotation;

            if (overrideHomeCenterOnAwake)
            {
                homeCenter = transform.position;
            }
        }

        private void OnEnable()
        {
            if (!_initialized)
            {
                _initialized = true;
                TransitionTo(BugState.Loiter, true);
            }

            // 初始化移动前向（用于移动转向插值）
            _forwardDirection = transform.right;
            if (_forwardDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                _forwardDirection = Vector2.right;
            }

            // 初始化视觉朝向基准（避免启用瞬跳）
            _currentFacingRotation = GetCurrentZRotation();
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            UpdateHomeStatus();
            UpdateCooldowns(deltaTime);

            if (_playerGrabbed)
            {
                ClearTrackedLight();
                if (_isInHome)
                {
                    TransitionTo(BugState.Loiter);
                }
                else
                {
                    TransitionTo(BugState.ReturnHome);
                }
            }

            UpdateStateMachine(deltaTime);

            if (!useRigidbody || body == null)
            {
                ApplyMovement(deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (useRigidbody && body != null)
            {
                ApplyMovement(Time.fixedDeltaTime);
            }
        }

        private void LateUpdate()
        {
            ApplyFacing(Time.deltaTime);
        }

        private void UpdateStateMachine(float deltaTime)
        {
            switch (_state)
            {
                case BugState.Loiter:
                    UpdateLoiter(deltaTime);
                    break;
                case BugState.ChaseLight:
                    UpdateChaseLight();
                    break;
                case BugState.ReturnHome:
                    UpdateReturnHome();
                    break;
            }
        }

        private void UpdateLoiter(float deltaTime)
        {
            var position = GetPosition();
            _currentSpeed = Mathf.Max(0f, loiterSpeed);

            _loiterTargetTimer -= deltaTime;
            if (_loiterTargetTimer <= 0f || Vector2.Distance(position, _loiterTarget) <= loiterTargetReachDistance)
            {
                _loiterTarget = GetRandomLoiterTarget(position);
                _loiterTargetTimer = Mathf.Max(0.1f, loiterTargetInterval);
            }

            SetDesiredMovement(_loiterTarget - position);

            if (_playerGrabbed || _releaseCooldownTimer > 0f)
            {
                return;
            }

            if (scanInterval <= 0f)
            {
                TryAcquireLamp();
                return;
            }

            _scanTimer += deltaTime;
            if (_scanTimer >= scanInterval)
            {
                _scanTimer = 0f;
                TryAcquireLamp();
            }
        }

        private void TryAcquireLamp()
        {
            if (!TryFindClosestValidLamp(out var lampInfo))
            {
                return;
            }

            SetTrackedLamp(lampInfo);
            TransitionTo(BugState.ChaseLight, true);
        }

        private void UpdateChaseLight()
        {
            if (!TryGetTrackedLampInfo(out var info))
            {
                ClearTrackedLight();
                TransitionTo(BugState.ReturnHome);
                return;
            }

            if (!IsWithinActivityBounds(info.WorldPos))
            {
                ClearTrackedLight();
                TransitionTo(BugState.ReturnHome);
                return;
            }

            _trackedLampPosition = info.WorldPos;


            var toTarget = _trackedLampPosition - GetPosition();
            var distance = toTarget.magnitude;

            // 根据距离计算目标速度：进圈减速，入圈停住
            if (distance <= chaseStopDistance)
            {
                _currentSpeed = 0f;
            }
            else if (distance <= chaseStopDistance + chaseSlowDistance && chaseSlowDistance > 0f)
            {
                float t = (distance - chaseStopDistance) / chaseSlowDistance;
                _currentSpeed = chaseSpeed * t;
            }
            else
            {
                _currentSpeed = chaseSpeed;
            }

            SetDesiredMovement(toTarget);
        }

        private void UpdateReturnHome()
        {
            _currentSpeed = Mathf.Max(0f, returnSpeed);
            SetDesiredMovement(GetHomeCenter() - GetPosition());

            if (_isInHome)
            {
                TransitionTo(BugState.Loiter);
            }
        }

        private bool TryFindClosestValidLamp(out LampInfo lampInfo)
        {
            lampInfo = default;
            var lamps = this.SendQuery(new GetGameplayEnabledLampsQuery());
            if (lamps == null || lamps.Count == 0)
            {
                return false;
            }

            var origin = GetPosition();
            var bestDistance = float.MaxValue;
            var found = false;

            for (var i = 0; i < lamps.Count; i++)
            {
                var lamp = lamps[i];
                var lampPosition = lamp.WorldPos;

                if (!IsWithinScanArea(origin, lampPosition))
                {
                    continue;
                }

                if (!IsWithinActivityBounds(lampPosition))
                {
                    continue;
                }

                var distance = Vector2.Distance(origin, lampPosition);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    lampInfo = lamp;
                    found = true;
                }
            }

            return found;
        }

        private bool TryGetTrackedLampInfo(out LampInfo lampInfo)
        {
            lampInfo = default;
            if (_trackedLampId < 0)
            {
                return false;
            }

            var lamps = this.SendQuery(new GetGameplayEnabledLampsQuery());
            if (lamps == null || lamps.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < lamps.Count; i++)
            {
                var lamp = lamps[i];
                if (lamp.LampId != _trackedLampId)
                {
                    continue;
                }

                if (!IsWithinActivityBounds(lamp.WorldPos))
                {
                    return false;
                }

                lampInfo = lamp;
                return true;
            }

            return false;
        }

        private void SetTrackedLamp(LampInfo lampInfo)
        {
            _trackedLampId = lampInfo.LampId;
            _trackedLampPosition = lampInfo.WorldPos;
        }

        private void ClearTrackedLight()
        {
            _trackedLampId = -1;
        }

        private void TransitionTo(BugState next, bool force = false)
        {
            if (!force && _state == next)
            {
                return;
            }

            var previous = _state;
            _state = next;
            OnEnterState(next, previous);
        }

        private void OnEnterState(BugState next, BugState previous)
        {
            switch (next)
            {
                case BugState.Loiter:
                    _loiterTargetTimer = 0f;
                    _scanTimer = 0f;
                    if (previous == BugState.ChaseLight)
                    {
                        AudioService.Play("SFX-ENM-0002", new AudioContext
                        {
                            Position = transform.position,
                            HasPosition = true
                        });
                    }
                    break;
                case BugState.ChaseLight:
                    AudioService.Play("SFX-ENM-0001", new AudioContext
                    {
                        Position = transform.position,
                        HasPosition = true
                    });
                    break;
                case BugState.ReturnHome:
                    if (previous == BugState.ChaseLight)
                    {
                        AudioService.Play("SFX-ENM-0002", new AudioContext
                        {
                            Position = transform.position,
                            HasPosition = true
                        });
                    }
                    break;
            }
        }

        private void SetDesiredMovement(Vector2 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                _desiredDirection = _forwardDirection;
                return;
            }

            _desiredDirection = direction.normalized;
        }

        private void ApplyMovement(float deltaTime)
        {
            var position = GetPosition();

            // 即使移动速度为 0，也通过转向插值更新 _forwardDirection，从而支持原地转头（朝向 _desiredDirection）

            var desired = _desiredDirection;
            if (desired.sqrMagnitude <= Mathf.Epsilon)
            {
                desired = _forwardDirection;
            }

            _forwardDirection = RotateTowards(_forwardDirection, desired, Mathf.Max(0f, turnSpeed) * deltaTime);
            if (_forwardDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                _forwardDirection = desired.sqrMagnitude > Mathf.Epsilon ? desired : Vector2.right;
            }

            if (_currentSpeed <= 0f)
            {
                return;
            }

            var newPosition = position + _forwardDirection * _currentSpeed * deltaTime;
            if (_state == BugState.Loiter)
            {
                newPosition = ClampToHomeBounds(newPosition);
            }
            newPosition = ClampToBounds(newPosition);

            SetPosition(newPosition);
        }

        // CHANGED: 支持 Loiter 固定朝向 + 视觉平滑
        private void ApplyFacing(float deltaTime)
        {
            if (!faceMovement)
            {
                return;
            }

            var overlayRotation = GetFacingOverlayRotation();
            var targetRotation = overlayRotation;

            // 计算要叠加的“方向角”
            float addAngle = 0f;

            if (_state == BugState.Loiter)
            {
                switch (loiterFacingMode)
                {
                    case LoiterFacingMode.OverlayOnly:
                        addAngle = 0f; // 旧行为：等价朝右
                        break;

                    case LoiterFacingMode.FixedDirection:
                        addAngle = GetWorldAxisAngle(loiterFixedFacingAxis); // 默认 Up => 90
                        break;

                    case LoiterFacingMode.FaceMovement:
                        if (_forwardDirection.sqrMagnitude > Mathf.Epsilon)
                        {
                            addAngle = Mathf.Atan2(_forwardDirection.y, _forwardDirection.x) * Mathf.Rad2Deg;
                        }
                        else
                        {
                            addAngle = 0f;
                        }
                        break;
                }

                targetRotation = overlayRotation + addAngle;
            }
            else
            {
                if (_forwardDirection.sqrMagnitude <= Mathf.Epsilon)
                {
                    return;
                }

                addAngle = Mathf.Atan2(_forwardDirection.y, _forwardDirection.x) * Mathf.Rad2Deg;
                targetRotation = overlayRotation + addAngle;
            }

            // 平滑过渡（避免硬切）
            if (!smoothFacing || facingSmoothSpeed <= 0f || deltaTime <= 0f)
            {
                _currentFacingRotation = targetRotation;
                ApplyRotation(targetRotation);
                return;
            }

            _currentFacingRotation = Mathf.MoveTowardsAngle(_currentFacingRotation, targetRotation, facingSmoothSpeed * deltaTime);
            ApplyRotation(_currentFacingRotation);
        }

        private void UpdateHomeStatus()
        {
            var position = GetPosition();
            if (homeBounds != null)
            {
                _isInHome = homeBounds.OverlapPoint(position);
                return;
            }

            var distance = Vector2.Distance(position, GetHomeCenter());
            if (_isInHome)
            {
                if (distance > homeExitRadius)
                {
                    _isInHome = false;
                }
            }
            else if (distance <= homeEnterRadius)
            {
                _isInHome = true;
            }
        }

        private void UpdateCooldowns(float deltaTime)
        {
            if (_releaseCooldownTimer > 0f)
            {
                _releaseCooldownTimer = Mathf.Max(0f, _releaseCooldownTimer - deltaTime);
            }
        }

        private Vector2 GetPosition()
        {
            if (useRigidbody && body != null)
            {
                return body.position;
            }

            return transform.position;
        }

        private void SetPosition(Vector2 position)
        {
            if (useRigidbody && body != null)
            {
                body.MovePosition(position);
            }
            else
            {
                transform.position = position;
            }
        }

        private void SetPositionImmediate(Vector2 position)
        {
            if (useRigidbody && body != null)
            {
                body.position = position;
            }
            else
            {
                transform.position = position;
            }
        }

        private Vector2 ClampToBounds(Vector2 position)
        {
            if (moveBounds == null)
            {
                return position;
            }

            return moveBounds.ClosestPoint(position);
        }

        private Vector2 ClampToHomeBounds(Vector2 position)
        {
            if (homeBounds != null)
            {
                return homeBounds.ClosestPoint(position);
            }

            var center = GetHomeCenter();
            var maxRadius = Mathf.Max(0f, homeExitRadius);
            if (maxRadius <= Mathf.Epsilon)
            {
                return center;
            }

            var offset = position - center;
            var distance = offset.magnitude;
            if (distance > maxRadius)
            {
                return center + offset / distance * maxRadius;
            }

            return position;
        }

        private bool IsWithinActivityBounds(Vector2 position)
        {
            return moveBounds == null || moveBounds.OverlapPoint(position);
        }

        private bool IsWithinScanArea(Vector2 origin, Vector2 position)
        {
            if (scanTrigger != null)
            {
                return scanTrigger.OverlapPoint(position);
            }

            if (attentionRadius <= 0f)
            {
                return true;
            }

            return Vector2.Distance(origin, position) <= attentionRadius;
        }

        private Vector2 GetHomeCenter()
        {
            return homeAnchor != null ? (Vector2)homeAnchor.position : homeCenter;
        }

        private Vector2 GetRandomPointInHomeArea()
        {
            if (homeBounds != null)
            {
                var bounds = homeBounds.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var random = new Vector2(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y));
                    if (homeBounds.OverlapPoint(random))
                    {
                        return ClampToBounds(random);
                    }
                }

                return ClampToBounds(homeBounds.ClosestPoint(GetHomeCenter()));
            }

            var radius = Mathf.Max(0f, homeExitRadius);
            return ClampToBounds(GetHomeCenter() + Random.insideUnitCircle * radius);
        }

        private Vector2 GetRandomLoiterTarget(Vector2 origin)
        {
            var radius = Mathf.Max(0f, loiterJitterRadius);
            if (radius <= Mathf.Epsilon)
            {
                return GetRandomPointInHomeArea();
            }

            var target = origin + Random.insideUnitCircle * radius;
            target = ClampToHomeBounds(target);
            return ClampToBounds(target);
        }

        private void ApplyRotation(float rotation)
        {
            if (useRigidbody && body != null)
            {
                body.MoveRotation(rotation);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            }
        }

        private float GetFacingOverlayRotation()
        {
            return _baseRotation + GetFacingAxisOffset() + facingAngleOffset;
        }

        private float GetFacingAxisOffset()
        {
            switch (facingAxis)
            {
                case FacingAxis.Up:
                    return -90f;
                case FacingAxis.Left:
                    return 180f;
                case FacingAxis.Down:
                    return 90f;
                default:
                    return 0f;
            }
        }

        private static float GetWorldAxisAngle(FacingAxis axis)
        {
            switch (axis)
            {
                case FacingAxis.Up:
                    return 90f;
                case FacingAxis.Left:
                    return 180f;
                case FacingAxis.Down:
                    return -90f; // 等价 270
                default:
                    return 0f;   // Right
            }
        }

        private float GetCurrentZRotation()
        {
            if (useRigidbody && body != null)
            {
                return body.rotation;
            }

            return transform.eulerAngles.z;
        }

        private static Vector2 RotateTowards(Vector2 current, Vector2 desired, float maxDegrees)
        {
            if (current.sqrMagnitude <= Mathf.Epsilon)
            {
                return desired.sqrMagnitude <= Mathf.Epsilon ? Vector2.right : desired.normalized;
            }

            if (desired.sqrMagnitude <= Mathf.Epsilon)
            {
                return current.normalized;
            }

            var currentAngle = Mathf.Atan2(current.y, current.x) * Mathf.Rad2Deg;
            var desiredAngle = Mathf.Atan2(desired.y, desired.x) * Mathf.Rad2Deg;
            var newAngle = Mathf.MoveTowardsAngle(currentAngle, desiredAngle, maxDegrees);
            var radians = newAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private void OnValidate()
        {
            homeEnterRadius = Mathf.Max(0f, homeEnterRadius);
            homeExitRadius = Mathf.Max(homeEnterRadius, homeExitRadius);
            attentionRadius = Mathf.Max(0f, attentionRadius);
            scanInterval = Mathf.Max(0f, scanInterval);
            loiterSpeed = Mathf.Max(0f, loiterSpeed);
            chaseSpeed = Mathf.Max(0f, chaseSpeed);
            returnSpeed = Mathf.Max(0f, returnSpeed);
            loiterTargetInterval = Mathf.Max(0.05f, loiterTargetInterval);
            loiterTargetReachDistance = Mathf.Max(0.01f, loiterTargetReachDistance);
            loiterJitterRadius = Mathf.Max(0f, loiterJitterRadius);
            turnSpeed = Mathf.Max(0f, turnSpeed);
            releaseScanCooldown = Mathf.Max(0f, releaseScanCooldown);

            // Facing 参数也做下保护
            facingSmoothSpeed = Mathf.Max(0f, facingSmoothSpeed);

            chaseStopDistance = Mathf.Max(0f, chaseStopDistance);
            chaseSlowDistance = Mathf.Max(0f, chaseSlowDistance);
        }
    }
}
