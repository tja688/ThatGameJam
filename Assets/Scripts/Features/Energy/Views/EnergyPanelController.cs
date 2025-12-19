using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ThatGameJam
{
    public class EnergyPanelController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        [Header("UI References")]
        public Text EnergyText;
        public Button BtnConsume;
        public Button BtnRecover;
        public Button BtnReset;

        private void Start()
        {
            var model = this.GetModel<IEnergyModel>();

            // Initial Update
            UpdateView(model.CurrentEnergy.Value);

            // Bind Event
            model.CurrentEnergy.Register(UpdateView)
                .UnRegisterWhenGameObjectDestroyed(gameObject);

            // Button Listeners
            BtnConsume.onClick.AddListener(() =>
            {
                this.SendCommand(new ConsumeEnergyCommand(10));
            });

            BtnRecover.onClick.AddListener(() =>
            {
                this.SendCommand(new RecoverEnergyCommand(10));
            });

            BtnReset.onClick.AddListener(() =>
            {
                this.SendCommand(new ResetEnergyCommand());
            });
        }

        private void UpdateView(int energy)
        {
            var model = this.GetModel<IEnergyModel>();
            if (EnergyText != null)
            {
                EnergyText.text = $"{energy} / {model.MaxEnergy}";
            }
        }
    }
}
