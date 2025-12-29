using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ThatGameJam.Test
{
    /// <summary>
    /// 快速退出脚本：
    /// 编辑器模式下按 Esc 键停止播放
    /// 实际游戏下按 Esc 键退出游戏
    /// </summary>
    public class QuickExit : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
#if UNITY_EDITOR
                // 在编辑器模式下，停止运行
                EditorApplication.isPlaying = false;
#else
                // 在发布后的游戏中，退出程序
                Application.Quit();
#endif
            }
        }
    }
}
