using UnityEngine;

namespace ThatGameJam.SaveSystem
{
    public abstract class SaveParticipant<T> : MonoBehaviour, ISaveParticipant
    {
        [SerializeField] private int loadOrder;

        public abstract string SaveKey { get; }
        public int LoadOrder => loadOrder;

        protected virtual void OnEnable()
        {
            SaveRegistry.Register(this);
        }

        protected virtual void OnDisable()
        {
            SaveRegistry.Unregister(this);
        }

        public string CaptureToJson()
        {
            var data = Capture();
            if (data == null)
            {
                return string.Empty;
            }

            return JsonUtility.ToJson(data);
        }

        public void RestoreFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            var data = JsonUtility.FromJson<T>(json);
            Restore(data);
        }

        protected abstract T Capture();
        protected abstract void Restore(T data);
    }
}
