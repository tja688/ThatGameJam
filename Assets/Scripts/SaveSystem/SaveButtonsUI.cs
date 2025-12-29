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
        }

        public void Load()
        {
            if (saveManager != null)
            {
                saveManager.Load();
            }
        }

        public void Delete()
        {
            if (saveManager != null)
            {
                saveManager.Delete();
            }
        }
    }
}
