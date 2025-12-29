using UnityEngine;

namespace ThatGameJam.SaveSystem
{
    public static class SaveLog
    {
        public static bool Enabled { get; set; }

        public static void Info(string message)
        {
            if (!Enabled)
            {
                return;
            }

            Debug.Log($"[SaveSystem] {message}");
        }

        public static void Warn(string message)
        {
            if (!Enabled)
            {
                return;
            }

            Debug.LogWarning($"[SaveSystem] {message}");
        }
    }
}
