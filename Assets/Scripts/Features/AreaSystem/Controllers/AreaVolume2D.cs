using QFramework;
using UnityEngine;

namespace ThatGameJam.Features.AreaSystem.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class AreaVolume2D : MonoBehaviour
    {
        [SerializeField] private string areaId;
        [SerializeField] private int priority;

        private Collider2D _collider2D;

        public string AreaId => areaId;
        public int Priority => priority;

        public float AreaSize
        {
            get
            {
                if (_collider2D == null)
                {
                    _collider2D = GetComponent<Collider2D>();
                }

                if (_collider2D == null)
                {
                    return 0f;
                }

                var size = _collider2D.bounds.size;
                return Mathf.Abs(size.x * size.y);
            }
        }

        private void Awake()
        {
            _collider2D = GetComponent<Collider2D>();
            if (_collider2D != null && !_collider2D.isTrigger)
            {
                LogKit.W("AreaVolume2D expects Collider2D.isTrigger = true.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var sensor = other.GetComponentInParent<PlayerAreaSensor>();
            if (sensor == null)
            {
                return;
            }

            sensor.NotifyAreaEnter(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var sensor = other.GetComponentInParent<PlayerAreaSensor>();
            if (sensor == null)
            {
                return;
            }

            sensor.NotifyAreaExit(this);
        }
    }
}
