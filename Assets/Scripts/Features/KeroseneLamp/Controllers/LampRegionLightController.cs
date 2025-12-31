using QFramework;
using ThatGameJam.Features.AreaSystem.Events;
using ThatGameJam.Features.AreaSystem.Queries;
using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Controllers
{
    public class LampRegionLightController : MonoBehaviour, IController
    {
        [SerializeField] private string areaId;
        [SerializeField] private string fallbackAreaId = "Unknown";
        [SerializeField] private bool usePreplacedAreaId = true;

        [Header("Visual Targets")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Behaviour[] visualBehaviours;
        [SerializeField] private Renderer[] visualRenderers;

        private bool _visualEnabled = true;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;
        public bool IsVisualEnabled => _visualEnabled;
        public string AreaId => areaId;

        private void Awake()
        {
            if (usePreplacedAreaId && string.IsNullOrEmpty(areaId))
            {
                var preplaced = GetComponent<KeroseneLampPreplaced>();
                if (preplaced != null)
                {
                    areaId = preplaced.AreaId;
                }
            }
        }

        private void OnEnable()
        {
            this.RegisterEvent<AreaChangedEvent>(OnAreaChanged)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void Start()
        {
            var currentAreaId = this.SendQuery(new GetCurrentAreaIdQuery());
            Refresh(currentAreaId);
        }

        public void SetAreaId(string newAreaId)
        {
            areaId = newAreaId;
            if (isActiveAndEnabled)
            {
                var currentAreaId = this.SendQuery(new GetCurrentAreaIdQuery());
                Refresh(currentAreaId);
            }
        }

        private void OnAreaChanged(AreaChangedEvent e)
        {
            Refresh(e.CurrentAreaId);
        }

        private void Refresh(string currentAreaId)
        {
            var resolvedAreaId = string.IsNullOrEmpty(areaId) ? fallbackAreaId : areaId;
            var shouldEnable = string.IsNullOrEmpty(currentAreaId) || resolvedAreaId == currentAreaId;
            SetVisualEnabled(shouldEnable);
        }

        private void SetVisualEnabled(bool enabled)
        {
            if (_visualEnabled == enabled)
            {
                return;
            }

            _visualEnabled = enabled;

            if (visualRoot != null)
            {
                visualRoot.SetActive(enabled);
            }

            if (visualBehaviours != null)
            {
                for (var i = 0; i < visualBehaviours.Length; i++)
                {
                    if (visualBehaviours[i] != null)
                    {
                        visualBehaviours[i].enabled = enabled;
                    }
                }
            }

            if (visualRenderers != null)
            {
                for (var i = 0; i < visualRenderers.Length; i++)
                {
                    if (visualRenderers[i] != null)
                    {
                        visualRenderers[i].enabled = enabled;
                    }
                }
            }
        }
    }
}
