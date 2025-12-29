using ThatGameJam.Features.KeroseneLamp.Controllers;
using ThatGameJam.Features.KeroseneLamp.Models;
using UnityEngine;

namespace ThatGameJam.SaveSystem.Adapters
{
    public class KeroseneLampSaveAdapter : SaveParticipant<KeroseneLampSaveState>
    {
        [SerializeField] private string saveKey = "lamp.kerosene";
        [SerializeField] private KeroseneLampManager lampManager;

        public override string SaveKey => saveKey;

        private void Reset()
        {
            saveKey = "lamp.kerosene";
        }

        protected override KeroseneLampSaveState Capture()
        {
            ResolveReferences();
            return lampManager != null ? lampManager.CaptureSaveState() : new KeroseneLampSaveState();
        }

        protected override void Restore(KeroseneLampSaveState data)
        {
            if (data == null)
            {
                return;
            }

            ResolveReferences();
            if (lampManager != null)
            {
                lampManager.RestoreFromSave(data);
            }
        }

        private void ResolveReferences()
        {
            if (lampManager == null)
            {
                lampManager = FindObjectOfType<KeroseneLampManager>();
            }
        }
    }
}
