using ThatGameJam.UI.Services;
using UnityEngine;

namespace ThatGameJam.UI.Mock
{
    public class UIMockBootstrap : MonoBehaviour
    {
        public static UIMockBootstrap Instance { get; private set; }

        [Header("Mock Mode")]
        [SerializeField] private bool enableMockMode = true;
        [SerializeField] private bool autoInjectOnStart = true;

        private UIMockDataInjector _injector;

        private MockGameFlowService _gameFlow;
        private MockSaveService _saveService;
        private MockAudioSettingsService _audioSettings;
        private MockInputRebindService _inputRebind;
        private MockPlayerStatusProvider _playerStatus;
        private MockQuestLogProvider _questLog;
        private MockInventoryProvider _inventory;

        private void Awake()
        {
            if (!enableMockMode)
            {
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _gameFlow = new MockGameFlowService();
            _saveService = new MockSaveService();
            _audioSettings = new MockAudioSettingsService();
            _inputRebind = new MockInputRebindService();
            _playerStatus = new MockPlayerStatusProvider();
            _questLog = new MockQuestLogProvider();
            _inventory = new MockInventoryProvider();

            UIServiceRegistry.RegisterAll(
                _gameFlow,
                _saveService,
                _audioSettings,
                _inputRebind,
                _playerStatus,
                _questLog,
                _inventory);

            _injector = new UIMockDataInjector(
                _audioSettings,
                _saveService,
                _playerStatus,
                _questLog,
                _inventory);
        }

        private void Start()
        {
            if (enableMockMode && autoInjectOnStart)
            {
                InjectNow();
            }
        }

        public void InjectNow()
        {
            if (!enableMockMode)
            {
                return;
            }

            _injector?.InjectAll();
        }
    }
}
