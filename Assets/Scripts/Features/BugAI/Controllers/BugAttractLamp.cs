using QFramework;
using ThatGameJam.Features.KeroseneLamp.Queries;
using UnityEngine;

namespace ThatGameJam.Features.BugAI.Controllers
{
    public class BugAttractLamp : MonoBehaviour, IController
    {
        [SerializeField] private BugMovementBase movement;
        [SerializeField] private float attractRadius = 4f;
        [SerializeField] private int priority = 10;

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
            if (movement == null || attractRadius <= 0f)
            {
                return;
            }

            if (TryGetClosestLamp(out var lampPos))
            {
                movement.SetTarget(this, lampPos, priority);
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
                if (dist > attractRadius)
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
