using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.BackpackFeature.Models;
using ThatGameJam.Features.InteractableFeature.Events;
using UnityEngine;

namespace ThatGameJam.Features.InteractableFeature.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class PlayerInteractSensor : MonoBehaviour, IController, ICanSendEvent
    {
        private readonly Dictionary<Interactable, int> _enterOrder = new Dictionary<Interactable, int>();
        private int _nextOrder;
        private Interactable _currentCandidate;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        public Interactable GetCurrentCandidate()
        {
            return _currentCandidate;
        }

        private void Awake()
        {
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null && !collider2D.isTrigger)
            {
                LogKit.W("PlayerInteractSensor expects Collider2D.isTrigger = true.");
            }
        }

        private void OnEnable()
        {
            UpdateCandidate();
        }

        private void OnDisable()
        {
            _enterOrder.Clear();
            UpdateCandidate();
        }

        private void Update()
        {
            if (_enterOrder.Count > 0)
            {
                UpdateCandidate();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsOwnedByPlayer(other))
            {
                return;
            }

            var interactable = other.GetComponentInParent<Interactable>();
            if (interactable == null || _enterOrder.ContainsKey(interactable))
            {
                return;
            }

            _enterOrder[interactable] = _nextOrder++;
            UpdateCandidate();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsOwnedByPlayer(other))
            {
                return;
            }

            var interactable = other.GetComponentInParent<Interactable>();
            if (interactable == null)
            {
                return;
            }

            if (_enterOrder.Remove(interactable))
            {
                UpdateCandidate();
            }
        }

        private void UpdateCandidate()
        {
            if (_enterOrder.Count == 0)
            {
                SetCandidate(null);
                return;
            }

            var best = default(Interactable);
            var bestPriority = int.MinValue;
            var bestDistance = float.MaxValue;
            var bestOrder = int.MaxValue;

            var toRemove = new List<Interactable>();
            foreach (var pair in _enterOrder)
            {
                var candidate = pair.Key;
                if (candidate == null || !candidate.isActiveAndEnabled)
                {
                    toRemove.Add(candidate);
                    continue;
                }

                if (candidate.transform.IsChildOf(transform))
                {
                    toRemove.Add(candidate);
                    continue;
                }

                var priority = candidate.Priority;
                var distance = (candidate.transform.position - transform.position).sqrMagnitude;
                var order = pair.Value;

                if (priority > bestPriority
                    || (priority == bestPriority && distance < bestDistance)
                    || (priority == bestPriority && Mathf.Approximately(distance, bestDistance) && order < bestOrder))
                {
                    best = candidate;
                    bestPriority = priority;
                    bestDistance = distance;
                    bestOrder = order;
                }
            }

            if (toRemove.Count > 0)
            {
                for (var i = 0; i < toRemove.Count; i++)
                {
                    _enterOrder.Remove(toRemove[i]);
                }
            }

            toRemove.Clear();

            SetCandidate(best);
        }

        private bool IsOwnedByPlayer(Collider2D collider2D)
        {
            return collider2D != null && collider2D.transform.IsChildOf(transform);
        }

        private void SetCandidate(Interactable candidate)
        {
            if (_currentCandidate == candidate)
            {
                return;
            }

            _currentCandidate = candidate;

            var item = candidate != null ? candidate.PickupItem : null;
            var type = candidate != null ? candidate.Type : default;
            var name = candidate != null ? candidate.DisplayName : string.Empty;
            if (string.IsNullOrEmpty(name) && item != null)
            {
                name = item.DisplayName;
            }

            this.SendEvent(new InteractableCandidateChangedEvent
            {
                HasCandidate = candidate != null,
                Type = type,
                DisplayName = name,
                ItemDefinition = item
            });
        }
    }
}
