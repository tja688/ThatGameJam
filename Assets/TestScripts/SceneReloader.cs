using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThatGameJam.Test
{
    /// <summary>
    /// 用于快速测试的场景重载脚本。
    /// 可以将其挂载到场景中的任何物体上（例如一个 GlobalManager 或 UI 画布），
    /// 然后在 UI Button 的 OnClick 事件中关联并调用 RestartScene 方法。
    /// </summary>
    public class SceneReloader : MonoBehaviour
    {
        [Header("设置")]
        [Tooltip("是否启用 'R' 键快速重启")]
        public bool enableShortcut = true;

        /// <summary>
        /// 重新加载当前活动场景。
        /// </summary>
        public void RestartScene()
        {
            // 获取当前活跃场景的名称并重新加载
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        }

        private void Update()
        {
            // 如果启用了快捷键且按下了 R 键，则重启场景
            if (enableShortcut && Input.GetKeyDown(KeyCode.R))
            {
                RestartScene();
            }
        }
    }
}
