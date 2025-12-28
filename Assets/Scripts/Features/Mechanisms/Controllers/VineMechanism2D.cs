using DG.Tweening;
using QFramework;
using ThatGameJam.Features.KeroseneLamp.Queries;
using UnityEngine;

namespace ThatGameJam.Features.Mechanisms.Controllers
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class VineMechanism2D : MechanismControllerBase
    {
        [Header("Light Detection")]
        [SerializeField] private float lightAffectRadius = 3f;
        [SerializeField] private float lightRequiredSeconds = 0f;
        [SerializeField] private float shrinkRequiredSeconds = 20f; 

        [Header("Optimization")]
        [Tooltip("扫描光照的间隔时间（秒）")]
        [SerializeField] private float scanInterval = 3f; 

        [Header("Growth Animation")]
        [SerializeField] private float targetGrowthLength = 5f;
        [SerializeField] private float growthDuration = 1.0f;
        [SerializeField] private Vector2 growthDirection = Vector2.up;

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private BoxCollider2D growthCollider;
        [SerializeField] private bool enableColliderDuringGrowth = true;

        [Header("Status (Read Only)")]
        [SerializeField] private float growthValue; 
        [SerializeField] private bool hasLightCached; // 上一次扫描的结果

        private float _lightTimer;
        private float _scanTimer;
        private bool _isActivated;
        private Tween _growthTween;

        // 基础物理与显示参数
        private Vector2 _initialSpriteSize;
        private float _initialGrowthLength;
        private Vector2 _colliderBaseAnchor;
        private float _colliderThickness;
        private GrowthAxis _axis;
        private float _axisSign = 1f;

        private enum GrowthAxis { X, Y }

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (growthCollider == null) growthCollider = GetComponent<BoxCollider2D>();

            CacheAxis();

            if (spriteRenderer != null)
            {
                _initialSpriteSize = spriteRenderer.size;
                _initialGrowthLength = (_axis == GrowthAxis.X) ? _initialSpriteSize.x : _initialSpriteSize.y;
            }

            if (growthCollider != null) CacheColliderBase();

            // 随机化初始扫描计时器，防止大量植物在同一帧启动扫描
            _scanTimer = Random.Range(0f, scanInterval);
            
            ResetGrowthImmediate();
        }

        private void Update()
        {
            // 1. 性能优化：每隔 scanInterval 时间才扫描一次环境
            _scanTimer += Time.deltaTime;
            if (_scanTimer >= scanInterval)
            {
                _scanTimer = 0f;
                hasLightCached = IsLighted(); // 执行昂贵的查询和距离计算
            }

            // 2. 逻辑更新：根据缓存的光照结果更新生长/回缩进度
            UpdateLightTimer(hasLightCached, Time.deltaTime);

            // 3. 状态判定
            bool shouldActivate;
            if (hasLightCached)
            {
                shouldActivate = _lightTimer >= lightRequiredSeconds;
            }
            else
            {
                shouldActivate = _lightTimer > 0;
            }

            if (shouldActivate != _isActivated)
            {
                _isActivated = shouldActivate;
                PlayGrowthAnimation(_isActivated ? 1f : 0f);
            }
        }

        private void UpdateLightTimer(bool hasLight, float deltaTime)
        {
            if (hasLight)
            {
                _lightTimer = Mathf.Min(lightRequiredSeconds, _lightTimer + deltaTime);
            }
            else
            {
                // 计算回缩速率
                float shrinkRate = (lightRequiredSeconds <= 0) ? 1f : (lightRequiredSeconds / Mathf.Max(0.1f, shrinkRequiredSeconds));
                
                if (lightRequiredSeconds <= 0)
                {
                    // 针对即时生长的植物（RequiredSeconds=0），使用 0-1 的归一化计时处理回缩
                    _lightTimer = Mathf.Max(0f, _lightTimer - (deltaTime / shrinkRequiredSeconds));
                }
                else
                {
                    _lightTimer = Mathf.Max(0f, _lightTimer - (deltaTime * shrinkRate));
                }
            }
        }

        // 执行一次性的场景灯光扫描
        private bool IsLighted()
        {
            if (lightAffectRadius <= 0f) return false;

            var lamps = this.SendQuery(new GetGameplayEnabledLampsQuery());
            var pos = (Vector2)transform.position;
            var radiusSqr = lightAffectRadius * lightAffectRadius; // 使用平方比较进一步优化

            foreach (var lamp in lamps)
            {
                if (!lamp.VisualEnabled)
                {
                    continue;
                }

                var lampRealPos = (Vector2)lamp.WorldPos;

                // 使用 sqrMagnitude 代替 Distance (省去开方运算)
                if ((lampRealPos - pos).sqrMagnitude <= radiusSqr) return true;
            }
            return false;
        }

        private void PlayGrowthAnimation(float target)
        {
            _growthTween?.Kill();
            _growthTween = DOTween.To(() => growthValue, x =>
            {
                growthValue = x;
                ApplyGrowth(growthValue);
            }, target, growthDuration)
            .SetEase(Ease.OutQuint);
        }

        private void ApplyGrowth(float normalized)
        {
            var currentLength = Mathf.Lerp(_initialGrowthLength, targetGrowthLength, normalized);
            SetGrowthInternal(currentLength);

            if (growthCollider != null)
            {
                if (enableColliderDuringGrowth)
                    growthCollider.enabled = normalized > 0.01f;
                else
                    growthCollider.enabled = normalized >= 0.99f;
            }
        }

        private void SetGrowthInternal(float length)
        {
            if (spriteRenderer != null)
            {
                var size = spriteRenderer.size;
                if (_axis == GrowthAxis.X) size.x = length;
                else size.y = length;
                spriteRenderer.size = size;
            }

            if (growthCollider != null)
            {
                Vector2 size;
                Vector2 offset;

                if (_axis == GrowthAxis.X)
                {
                    size = new Vector2(length, _colliderThickness);
                    offset = new Vector2(_colliderBaseAnchor.x + _axisSign * length * 0.5f, _colliderBaseAnchor.y);
                }
                else
                {
                    size = new Vector2(_colliderThickness, length);
                    offset = new Vector2(_colliderBaseAnchor.x, _colliderBaseAnchor.y + _axisSign * length * 0.5f);
                }

                growthCollider.size = size;
                growthCollider.offset = offset;
            }
        }

        protected override void OnHardReset() => ResetGrowthImmediate();

        private void ResetGrowthImmediate()
        {
            _lightTimer = 0f;
            _isActivated = false;
            growthValue = 0f;
            _growthTween?.Kill();
            ApplyGrowth(0f);
        }

        private void CacheAxis()
        {
            var direction = (growthDirection == Vector2.zero) ? Vector2.up : growthDirection;
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                _axis = GrowthAxis.X;
                _axisSign = Mathf.Sign(direction.x);
            }
            else
            {
                _axis = GrowthAxis.Y;
                _axisSign = Mathf.Sign(direction.y);
            }
        }

        private void CacheColliderBase()
        {
            var size = growthCollider.size;
            var offset = growthCollider.offset;

            if (_axis == GrowthAxis.X)
            {
                _colliderThickness = size.y;
                _colliderBaseAnchor = new Vector2(offset.x - _axisSign * size.x * 0.5f, offset.y);
            }
            else
            {
                _colliderThickness = size.x;
                _colliderBaseAnchor = new Vector2(offset.x, offset.y - _axisSign * size.y * 0.5f);
            }
        }

        private void OnDestroy() => _growthTween?.Kill();

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, lightAffectRadius);
        }
    }
}
