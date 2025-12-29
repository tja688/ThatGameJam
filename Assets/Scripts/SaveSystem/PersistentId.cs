using System;
using UnityEngine;

namespace ThatGameJam.SaveSystem
{
    [DisallowMultipleComponent]
    public class PersistentId : MonoBehaviour
    {
        [SerializeField] private string id;

        public string Id => id;

        public void Regenerate()
        {
            id = Guid.NewGuid().ToString("N");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
