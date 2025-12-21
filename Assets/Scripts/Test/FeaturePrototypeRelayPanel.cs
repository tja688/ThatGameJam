using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Light
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.LightVitality.Models;

// Darkness
using ThatGameJam.Features.Darkness.Commands;
using ThatGameJam.Features.Darkness.Models;

// SafeZone
using ThatGameJam.Features.SafeZone.Commands;
using ThatGameJam.Features.SafeZone.Models;

// Shared events / enums
using ThatGameJam.Features.Shared;

namespace ThatGameJam.Features._Prototype.UI
{
    /// <summary>
    /// Temp demo panel to show features quickly:
    /// - Light vitality (current/max)
    /// - Darkness state + manual toggle
    /// - SafeZone count + manual +/- 
    /// - Event echo log for leader demo
    /// </summary>
    public sealed class FeaturePrototypeRelayPanel : MonoBehaviour, IController
    {
        [Header("UI Text (TMP)")]
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text lightText;
        [SerializeField] private TMP_Text darknessText;
        [SerializeField] private TMP_Text safeZoneText;
        [SerializeField] private TMP_Text logText;

        [Header("Tuning (Demo Only)")]
        [SerializeField] private float addLightAmount = 15f;
        [SerializeField] private float consumeLightAmount = 12f;

        [Header("Keyboard Shortcuts (optional)")]
        [SerializeField] private bool enableHotkeys = true;
        [SerializeField] private KeyCode keyAdd = KeyCode.Alpha1;
        [SerializeField] private KeyCode keyConsume = KeyCode.Alpha2;
        [SerializeField] private KeyCode keySetToMax = KeyCode.Alpha3;
        [SerializeField] private KeyCode keyToggleDarkness = KeyCode.Alpha4;
        [SerializeField] private KeyCode keySafePlus = KeyCode.Alpha5;
        [SerializeField] private KeyCode keySafeMinus = KeyCode.Alpha6;

        private readonly Queue<string> _logLines = new Queue<string>(32);
        private const int MaxLogLines = 10;

        // Local manual override state for demo buttons
        private bool _manualInDarkness;
        private int _manualSafeZoneCount;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            // Event echo
            this.RegisterEvent<LightChangedEvent>(OnLightChanged)
                .UnRegisterWhenDisabled(gameObject);

            this.RegisterEvent<LightDepletedEvent>(_ => AppendLog("LightDepletedEvent"))
                .UnRegisterWhenDisabled(gameObject);

            this.RegisterEvent<DarknessStateChangedEvent>(e =>
            {
                AppendLog($"DarknessStateChangedEvent: IsInDarkness={e.IsInDarkness}");
                RefreshSnapshot();
            }).UnRegisterWhenDisabled(gameObject);

            this.RegisterEvent<SafeZoneStateChangedEvent>(e =>
            {
                AppendLog($"SafeZoneStateChangedEvent: Count={e.SafeZoneCount}, IsSafe={e.IsSafe}");
                RefreshSnapshot();
            }).UnRegisterWhenDisabled(gameObject);

            // Init manual states from model snapshot (so buttons start consistent)
            var safeModel = this.GetModel<ISafeZoneModel>();
            _manualSafeZoneCount = safeModel.SafeZoneCount.Value;

            var darkModel = this.GetModel<IDarknessModel>();
            _manualInDarkness = darkModel.IsInDarkness.Value;

            RefreshSnapshot();
            AppendLog("Prototype panel enabled");
        }

        private void Start()
        {
            if (headerText != null)
            {
                headerText.text = "Feature Prototype Relay (Light / Darkness / SafeZone)";
            }

            RefreshSnapshot();
        }

        private void Update()
        {
            if (!enableHotkeys) return;

            if (Input.GetKeyDown(keyAdd)) UI_AddLight();
            if (Input.GetKeyDown(keyConsume)) UI_ConsumeLight_Debug();
            if (Input.GetKeyDown(keySetToMax)) UI_SetLightToMax();
            if (Input.GetKeyDown(keyToggleDarkness)) UI_ToggleDarkness();
            if (Input.GetKeyDown(keySafePlus)) UI_SafeZonePlus();
            if (Input.GetKeyDown(keySafeMinus)) UI_SafeZoneMinus();
        }

        // ---------------------------
        // UI Button Hooks (public)
        // ---------------------------

        public void UI_AddLight()
        {
            this.SendCommand(new AddLightCommand(addLightAmount));
            AppendLog($"SendCommand(AddLightCommand, +{addLightAmount})");
            // LightChangedEvent will refresh
        }

        public void UI_ConsumeLight_Debug()
        {
            // Use Debug reason for demo. (Your branch already fixed reason pipeline.)
            this.SendCommand(new ConsumeLightCommand(consumeLightAmount, ELightConsumeReason.Debug));
            AppendLog($"SendCommand(ConsumeLightCommand, -{consumeLightAmount}, reason=Debug)");
            // LightChangedEvent will refresh
        }

        public void UI_SetLightToMax()
        {
            var model = this.GetModel<ILightVitalityModel>();
            this.SendCommand(new SetLightCommand(model.MaxLight.Value));
            AppendLog("SendCommand(SetLightCommand, value=Max)");
        }

        public void UI_ToggleDarkness()
        {
            _manualInDarkness = !_manualInDarkness;
            this.SendCommand(new SetInDarknessCommand(_manualInDarkness));
            AppendLog($"SendCommand(SetInDarknessCommand, {_manualInDarkness})");
        }

        public void UI_SafeZonePlus()
        {
            _manualSafeZoneCount = Mathf.Clamp(_manualSafeZoneCount + 1, 0, 999);
            this.SendCommand(new SetSafeZoneCountCommand(_manualSafeZoneCount));
            AppendLog($"SendCommand(SetSafeZoneCountCommand, count={_manualSafeZoneCount})");
        }

        public void UI_SafeZoneMinus()
        {
            _manualSafeZoneCount = Mathf.Clamp(_manualSafeZoneCount - 1, 0, 999);
            this.SendCommand(new SetSafeZoneCountCommand(_manualSafeZoneCount));
            AppendLog($"SendCommand(SetSafeZoneCountCommand, count={_manualSafeZoneCount})");
        }

        public void UI_Refresh()
        {
            RefreshSnapshot();
            AppendLog("Manual refresh");
        }

        // ---------------------------
        // Event Handlers
        // ---------------------------

        private void OnLightChanged(LightChangedEvent e)
        {
            AppendLog($"LightChangedEvent: {e.Current:0.##}/{e.Max:0.##}");
            RefreshSnapshot();
        }

        // ---------------------------
        // Helpers
        // ---------------------------

        private void RefreshSnapshot()
        {
            // Light snapshot
            var lightModel = this.GetModel<ILightVitalityModel>();
            var current = lightModel.CurrentLight.Value;
            var max = lightModel.MaxLight.Value;

            if (lightText != null)
            {
                lightText.text = $"Light: {current:0.##} / {max:0.##}";
            }

            // Darkness snapshot
            var darkModel = this.GetModel<IDarknessModel>();
            var inDark = darkModel.IsInDarkness.Value;

            if (darknessText != null)
            {
                darknessText.text = $"Darkness: {(inDark ? "IN" : "OUT")}";
            }

            // SafeZone snapshot
            var safeModel = this.GetModel<ISafeZoneModel>();
            var count = safeModel.SafeZoneCount.Value;
            var isSafe = safeModel.IsSafe.Value;

            if (safeZoneText != null)
            {
                safeZoneText.text = $"SafeZone: count={count}  state={(isSafe ? "SAFE" : "UNSAFE")}";
            }

            // Keep manual values aligned if external sensors also change them
            _manualInDarkness = inDark;
            _manualSafeZoneCount = count;
        }

        private void AppendLog(string line)
        {
            if (logText == null) return;

            if (_logLines.Count >= MaxLogLines)
            {
                _logLines.Dequeue();
            }

            _logLines.Enqueue(line);

            // Rebuild
            var combined = string.Empty;
            foreach (var l in _logLines)
            {
                combined += l + "\n";
            }

            logText.text = combined;
        }
    }
}
