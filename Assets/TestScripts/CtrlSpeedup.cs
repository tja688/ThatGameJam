using UnityEngine;

namespace ThatGameJam.Test
{
    public class CtrlSpeedup : MonoBehaviour
    {
        [Min(1f)]
        [SerializeField] private float speedMultiplier = 4f;

        private float normalTimeScale;
        private float normalFixedDeltaTime;
        private bool isAccelerating;

        private void OnEnable()
        {
            CacheNormalTime();
        }

        private void OnDisable()
        {
            if (isAccelerating)
            {
                RestoreTime();
            }
        }

        private void Update()
        {
            bool wantsAcceleration = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (wantsAcceleration)
            {
                if (!isAccelerating)
                {
                    CacheNormalTime();
                    float multiplier = Mathf.Max(1f, speedMultiplier);
                    ApplyTimeScale(normalTimeScale * multiplier, normalFixedDeltaTime * multiplier);
                    isAccelerating = true;
                }
            }
            else
            {
                if (isAccelerating)
                {
                    RestoreTime();
                }
                else
                {
                    CacheNormalTime();
                }
            }
        }

        private void CacheNormalTime()
        {
            normalTimeScale = Time.timeScale;
            normalFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void RestoreTime()
        {
            ApplyTimeScale(normalTimeScale, normalFixedDeltaTime);
            isAccelerating = false;
        }

        private void ApplyTimeScale(float timeScale, float fixedDeltaTime)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = fixedDeltaTime;
        }
    }
}
