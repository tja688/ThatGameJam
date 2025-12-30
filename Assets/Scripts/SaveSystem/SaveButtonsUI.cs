using ThatGameJam.Independents.Audio;
using UnityEngine;

namespace ThatGameJam.SaveSystem
{
    public class SaveButtonsUI : MonoBehaviour
    {
        [SerializeField] private SaveManager saveManager;

        private void Awake()
        {
            if (saveManager == null)
            {
                saveManager = FindObjectOfType<SaveManager>();
            }
        }

        public void Save()
        {
            if (saveManager != null)
            {
                saveManager.Save();
            }

            AudioService.Play("SFX-UI-0001", new AudioContext
            {
                IsUI = true
            });
        }

        public void Load()
        {
            if (saveManager != null)
            {
                saveManager.Load();
            }

            AudioService.Play("SFX-UI-0002", new AudioContext
            {
                IsUI = true
            });
        }

        public void Delete()
        {
            if (saveManager != null)
            {
                saveManager.Delete();
            }

            AudioService.Play("SFX-UI-0003", new AudioContext
            {
                IsUI = true
            });
        }
    }
}
