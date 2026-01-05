using ThatGameJam.UI.Services.Interfaces;

namespace ThatGameJam.UI.Services
{
    public static class UIServiceRegistry
    {
        public static IGameFlowService GameFlow { get; private set; }
        public static ISaveService SaveService { get; private set; }
        public static IAudioSettingsService AudioSettings { get; private set; }
        public static IInputRebindService InputRebind { get; private set; }
        public static IPlayerStatusProvider PlayerStatus { get; private set; }
        public static IQuestLogProvider QuestLog { get; private set; }
        public static IInventoryProvider Inventory { get; private set; }
        public static IOperationInstructionsProvider OperationInstructions { get; private set; }

        public static void RegisterAll(
            IGameFlowService gameFlow,
            ISaveService saveService,
            IAudioSettingsService audioSettings,
            IInputRebindService inputRebind,
            IPlayerStatusProvider playerStatus,
            IQuestLogProvider questLog,
            IInventoryProvider inventory,
            IOperationInstructionsProvider operationInstructions = null)
        {
            GameFlow = gameFlow;
            SaveService = saveService;
            AudioSettings = audioSettings;
            InputRebind = inputRebind;
            PlayerStatus = playerStatus;
            QuestLog = questLog;
            Inventory = inventory;
            OperationInstructions = operationInstructions;
        }

        public static void SetGameFlow(IGameFlowService service) => GameFlow = service;
        public static void SetSaveService(ISaveService service) => SaveService = service;
        public static void SetAudioSettings(IAudioSettingsService service) => AudioSettings = service;
        public static void SetInputRebind(IInputRebindService service) => InputRebind = service;
        public static void SetPlayerStatus(IPlayerStatusProvider service) => PlayerStatus = service;
        public static void SetQuestLog(IQuestLogProvider service) => QuestLog = service;
        public static void SetInventory(IInventoryProvider service) => Inventory = service;
        public static void SetOperationInstructions(IOperationInstructionsProvider service) => OperationInstructions = service;

        public static void ClearAll()
        {
            GameFlow = null;
            SaveService = null;
            AudioSettings = null;
            InputRebind = null;
            PlayerStatus = null;
            QuestLog = null;
            Inventory = null;
            OperationInstructions = null;
        }
    }
}
