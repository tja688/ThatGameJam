using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThatGameJam.UI.Services
{
    public static class MainMenuSceneLoader
    {
        public static void ReturnToMainMenu()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
                return;
            }

            if (!string.IsNullOrEmpty(scene.name))
            {
                SceneManager.LoadScene(scene.name, LoadSceneMode.Single);
                return;
            }

            Debug.LogWarning("MainMenuSceneLoader could not reload the active scene.");
        }
    }
}
