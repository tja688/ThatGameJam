using UnityEngine;

namespace ThatGameJam.UI
{
    public class UIPauseService : MonoBehaviour
    {
        private bool _isPaused;

        public void ApplyPauseState(bool shouldPause)
        {
            if (_isPaused == shouldPause)
            {
                return;
            }

            _isPaused = shouldPause;
            Time.timeScale = shouldPause ? 0f : 1f;
        }
    }
}
