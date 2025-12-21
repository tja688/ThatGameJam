using QFramework;
using UnityEngine;

public static class ProjectToolkitBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BeforeSceneLoad()
    {
        _ = GameRootApp.Interface;
    }
}

