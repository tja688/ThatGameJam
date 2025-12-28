using QFramework;
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
        [SerializeField, Tooltip("是否根据移动方向翻转朝向。")]
        private bool faceMovement = true;
        [SerializeField, Tooltip("启动时用当前坐标覆盖 Home Center。")]
        private bool overrideHomeCenterOnAwake = true;

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

        [Header("Light Scan")]
        [SerializeField, Tooltip("感光范围半径。")]
        private float attentionRadius = 4f;
        [SerializeField, Tooltip("扫描光源的时间间隔（秒）。")]
        private float scanInterval = 3f;
        [SerializeField, Tooltip("放手后重新扫描光源的冷却时间（秒）。")]
        private float releaseScanCooldown = 5f;

        [Header("Movement")]
        [SerializeField, Tooltip("转向速度（度/秒）。")]
        private float turnSpeed = 360f;
        [SerializeField, Tooltip("追光移动速度。")]
        private float chaseSpeed = 3f;
        [SerializeField, Tooltip("正常归巢速度。")]
        private float returnSpeed = 1.4f;

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
        private Vector3 _baseScale;
        private bool _initialized;

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
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            _baseScale = transform.localScale;

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

            _forwardDirection = transform.right;
            if (_forwardDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                _forwardDirection = Vector2.right;
            }
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
            ApplyFacing();
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
                _loiterTarget = GetRandomPointInHomeArea();
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
            _currentSpeed = Mathf.Max(0f, chaseSpeed);
            SetDesiredMovement(_trackedLampPosition - GetPosition());
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

                if (!IsWithinActivityBounds(lampPosition))
                {
                    continue;
                }

                var distance = Vector2.Distance(origin, lampPosition);
                if (distance > attentionRadius)
                {
                    continue;
                }

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

            _state = next;
            OnEnterState(next);
        }

        private void OnEnterState(BugState next)
        {
            switch (next)
            {
                case BugState.Loiter:
                    _loiterTargetTimer = 0f;
                    _scanTimer = 0f;
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
            if (_currentSpeed <= 0f)
            {
                return;
            }

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

            var newPosition = position + _forwardDirection * _currentSpeed * deltaTime;
            if (_state == BugState.Loiter)
            {
                newPosition = ClampToHomeBounds(newPosition);
            }
            newPosition = ClampToBounds(newPosition);

            SetPosition(newPosition);
        }

        private void ApplyFacing()
        {
            if (!faceMovement)
            {
                return;
            }

            if (_forwardDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var scale = _baseScale;
            if (_forwardDirection.x != 0f)
            {
                scale.x = Mathf.Sign(_forwardDirection.x) * Mathf.Abs(_baseScale.x);
                transform.localScale = scale;
            }
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
            turnSpeed = Mathf.Max(0f, turnSpeed);
            releaseScanCooldown = Mathf.Max(0f, releaseScanCooldown);
        }
    }
}
