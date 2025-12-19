using QFramework;
using UnityEngine;

namespace ThatGameJam
{
    public static class ProjectToolkitBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            // Force Root Architecture initialization early.
            _ = GameRootApp.Interface;
        }
    }
}
