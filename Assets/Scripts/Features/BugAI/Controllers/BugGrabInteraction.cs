using QFramework;
using ThatGameJam.Features.PlayerCharacter2D.Models;
using UnityEngine;

namespace ThatGameJam.Features.BugAI.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class BugGrabInteraction : MonoBehaviour, IController
    {
        [SerializeField, Tooltip("要通知的虫子状态机（为空则在父级寻找）。")]
        private BugMovementBase movement;
        [SerializeField, Tooltip("玩家物体的 Tag（为空则不做 Tag 校验）。")]
        private string playerTag = "Player";
        [SerializeField, Tooltip("是否要求玩家按住抓取键才触发抓取。")]
        private bool requireGrabHeld = true;
        [SerializeField, Tooltip("进入触发器就自动抓取（会导致触碰即回巢，默认关闭）。")]
        private bool autoGrabOnTouch = false;

        private bool _playerInside;
        private bool _isGrabbed;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            if (movement == null)
            {
                movement = GetComponentInParent<BugMovementBase>();
            }

            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null && !collider2D.isTrigger)
            {
                LogKit.W("BugGrabInteraction expects Collider2D.isTrigger = true.");
            }
        }

        private void OnDisable()
        {
            if (_isGrabbed)
            {
                movement?.NotifyPlayerReleased();
                _isGrabbed = false;
            }

            _playerInside = false;
        }

        private void Update()
        {
            if (!_playerInside)
            {
                return;
            }

            if (!requireGrabHeld)
            {
                if (!autoGrabOnTouch)
                {
                    if (_isGrabbed)
                    {
                        movement?.NotifyPlayerReleased();
                        _isGrabbed = false;
                    }

                    return;
                }

                if (!_isGrabbed)
                {
                    movement?.NotifyPlayerGrabbed();
                    _isGrabbed = true;
                }

                return;
            }

            var wantsGrab = GetGrabHeld();
            if (wantsGrab && !_isGrabbed)
            {
                movement?.NotifyPlayerGrabbed();
                _isGrabbed = true;
            }
            else if (!wantsGrab && _isGrabbed)
            {
                movement?.NotifyPlayerReleased();
                _isGrabbed = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            _playerInside = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            _playerInside = false;
            if (_isGrabbed)
            {
                movement?.NotifyPlayerReleased();
                _isGrabbed = false;
            }
        }

        private bool IsPlayer(Collider2D other)
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                return true;
            }

            return other.CompareTag(playerTag);
        }

        private bool GetGrabHeld()
        {
            var model = this.GetModel<IPlayerCharacter2DModel>();
            if (model == null)
            {
                return false;
            }

            return model.FrameInput.GrabHeld;
        }
    }
}
