using QFramework;
using ThatGameJam.Independents.Audio;
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
        [SerializeField, Tooltip("是否要求玩家处于攀爬状态才算抓住。")]
        private bool requirePlayerClimbing = true;

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
            EndGrab();

            _playerInside = false;
        }

        private void Update()
        {
            if (!_playerInside)
            {
                EndGrab();
                return;
            }

            if (requirePlayerClimbing && !GetIsClimbing())
            {
                EndGrab();
                return;
            }

            if (!requireGrabHeld)
            {
                if (!autoGrabOnTouch)
                {
                    EndGrab();

                    return;
                }

                StartGrab();

                return;
            }

            var wantsGrab = GetGrabHeld();
            if (wantsGrab && !_isGrabbed)
            {
                StartGrab();
            }
            else if (!wantsGrab && _isGrabbed)
            {
                EndGrab();
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
            EndGrab();
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

        private bool GetIsClimbing()
        {
            var model = this.GetModel<IPlayerCharacter2DModel>();
            if (model == null)
            {
                return false;
            }

            return model.IsClimbing.Value;
        }

        private void StartGrab()
        {
            if (_isGrabbed)
            {
                return;
            }

            movement?.NotifyPlayerGrabbed();
            _isGrabbed = true;
            AudioService.Play("SFX-ENM-0003", new AudioContext
            {
                Position = transform.position,
                HasPosition = true
            });
        }

        private void EndGrab()
        {
            if (!_isGrabbed)
            {
                return;
            }

            movement?.NotifyPlayerReleased();
            _isGrabbed = false;
            AudioService.Stop("SFX-ENM-0003", new AudioContext
            {
                Position = transform.position,
                HasPosition = true
            });
        }
    }
}
