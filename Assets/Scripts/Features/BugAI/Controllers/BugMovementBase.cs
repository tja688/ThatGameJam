using System.Collections.Generic;
using UnityEngine;

namespace ThatGameJam.Features.BugAI.Controllers
{
    public class BugMovementBase : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private bool useRigidbody = true;
        [SerializeField] private bool faceMovement = true;

        private readonly Dictionary<object, TargetRequest> _targets = new Dictionary<object, TargetRequest>();
        private Vector3 _homePosition;

        private struct TargetRequest
        {
            public Vector3 Position;
            public int Priority;
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            _homePosition = transform.position;
        }

        private void Update()
        {
            if (_targets.Count == 0)
            {
                if (!useRigidbody)
                {
                    return;
                }

                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                }

                return;
            }

            var best = GetBestTarget();
            var direction = (best.Position - transform.position);
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var velocity = ((Vector2)direction).normalized * Mathf.Max(0f, moveSpeed);
            if (useRigidbody && body != null)
            {
                body.linearVelocity = velocity;
            }
            else
            {
                transform.position += (Vector3)(velocity * Time.deltaTime);
            }

            if (faceMovement)
            {
                var scale = transform.localScale;
                if (velocity.x != 0f)
                {
                    scale.x = Mathf.Sign(velocity.x) * Mathf.Abs(scale.x);
                    transform.localScale = scale;
                }
            }
        }

        public void SetTarget(object source, Vector3 position, int priority)
        {
            if (source == null)
            {
                return;
            }

            _targets[source] = new TargetRequest
            {
                Position = position,
                Priority = priority
            };
        }

        public void ClearTarget(object source)
        {
            if (source == null)
            {
                return;
            }

            _targets.Remove(source);
        }

        public void ResetToHome()
        {
            _targets.Clear();
            transform.position = _homePosition;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        private TargetRequest GetBestTarget()
        {
            var bestPriority = int.MinValue;
            TargetRequest best = default;
            foreach (var entry in _targets.Values)
            {
                if (entry.Priority >= bestPriority)
                {
                    bestPriority = entry.Priority;
                    best = entry;
                }
            }

            return best;
        }
    }
}
