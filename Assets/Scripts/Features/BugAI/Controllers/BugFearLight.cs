using QFramework;
using ThatGameJam.Features.KeroseneLamp.Queries;
using UnityEngine;

namespace ThatGameJam.Features.BugAI.Controllers
{
    public class BugFearLight : MonoBehaviour, IController
    {
        [SerializeField] private BugMovementBase movement;
        [SerializeField] private float fearRadius = 3f;
        [SerializeField] private float fleeDistance = 2f;
        [SerializeField] private int priority = 50;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            if (movement == null)
            {
                movement = GetComponent<BugMovementBase>();
            }
        }

        private void Update()
        {
            if (movement == null || fearRadius <= 0f)
            {
                return;
            }

            if (TryGetClosestLamp(out var lampPos))
            {
                var direction = ((Vector2)transform.position - lampPos).normalized;
                var target = (Vector2)transform.position + direction * fleeDistance;
                movement.SetTarget(this, target, priority);
            }
            else
            {
                movement.ClearTarget(this);
            }
        }

        private bool TryGetClosestLamp(out Vector2 position)
        {
            position = default;
            var lamps = this.SendQuery(new GetGameplayEnabledLampsQuery());
            if (lamps == null || lamps.Count == 0)
            {
                return false;
            }

            var origin = (Vector2)transform.position;
            var bestDistance = float.MaxValue;
            var found = false;

            for (var i = 0; i < lamps.Count; i++)
            {
                var lampPos = (Vector2)lamps[i].WorldPos;
                var dist = Vector2.Distance(origin, lampPos);
                if (dist > fearRadius)
                {
                    continue;
                }

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    position = lampPos;
                    found = true;
                }
            }

            return found;
        }
    }
}
