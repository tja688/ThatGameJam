using UnityEngine;

namespace ThatGameJam.Independents.Audio
{
    [RequireComponent(typeof(Collider2D))]
    public class TowerTopBgmTrigger2D : MonoBehaviour
    {
        [SerializeField] private BackgroundMusicManager manager;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool triggerOnce = true;

        private bool _triggered;

        private void Reset()
        {
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null)
            {
                collider2D.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(playerTag) && !other.CompareTag(playerTag))
            {
                return;
            }

            var target = manager != null ? manager : BackgroundMusicManager.Instance;
            if (target == null)
            {
                target = FindObjectOfType<BackgroundMusicManager>();
            }

            if (target == null)
            {
                Debug.LogWarning("TowerTopBgmTrigger2D: BackgroundMusicManager not found.", this);
                return;
            }

            target.PlayTowerTopMusic();
            if (triggerOnce)
            {
                _triggered = true;
            }
        }
    }
}
