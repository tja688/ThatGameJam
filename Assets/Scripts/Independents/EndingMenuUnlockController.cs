using ThatGameJam.SaveSystem;
using ThatGameJam.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ThatGameJam.Independents
{
    [DisallowMultipleComponent]
    public class EndingMenuUnlockController : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private GraphicRaycaster graphicRaycaster;

        [Header("Toggle Image")]
        [SerializeField] private GameObject toggleImage;

        [Header("Main Menu")]
        [SerializeField] private UIRouter uiRouter;
        [SerializeField] private bool autoFindRouter = true;

        [Header("Persistence")]
        [SerializeField] private bool loadOnEnable = true;

        private bool _unlocked;

        public bool IsUnlocked => _unlocked;

        private void Awake()
        {
            if (targetCanvas == null)
            {
                targetCanvas = GetComponent<Canvas>();
            }

            if (graphicRaycaster == null)
            {
                graphicRaycaster = GetComponent<GraphicRaycaster>();
            }

            if (toggleImage != null)
            {
                toggleImage.SetActive(false);
            }
        }

        private void OnEnable()
        {
            ResolveRouter();

            if (loadOnEnable)
            {
                _unlocked = EndingMenuUnlockState.Load();
            }

            ApplyVisibility();
        }

        private void Start()
        {
            ResolveRouter();
            ApplyVisibility();
        }

        private void OnDisable()
        {
            if (uiRouter != null)
            {
                uiRouter.MainMenuVisibilityChanged -= HandleMainMenuVisibilityChanged;
            }
        }

        public void ToggleImage()
        {
            if (!_unlocked)
            {
                return;
            }

            if (!IsMainMenuOnTop())
            {
                return;
            }

            if (toggleImage != null)
            {
                toggleImage.SetActive(!toggleImage.activeSelf);
            }
        }

        public void SetUnlocked(bool value, bool persist)
        {
            _unlocked = value;

            if (persist)
            {
                EndingMenuUnlockState.Save(value);
            }

            ApplyVisibility();
        }

        private void ResolveRouter()
        {
            if (uiRouter == null && autoFindRouter)
            {
                uiRouter = FindObjectOfType<UIRouter>();
            }

            if (uiRouter != null)
            {
                uiRouter.MainMenuVisibilityChanged -= HandleMainMenuVisibilityChanged;
                uiRouter.MainMenuVisibilityChanged += HandleMainMenuVisibilityChanged;
            }
        }

        private void HandleMainMenuVisibilityChanged(bool isVisible)
        {
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            bool visible = _unlocked && IsMainMenuOnTop();
            SetCanvasVisible(visible);

            if (!visible && toggleImage != null)
            {
                toggleImage.SetActive(false);
            }
        }

        private bool IsMainMenuOnTop()
        {
            return uiRouter != null && uiRouter.IsMainMenuOnTop;
        }

        private void SetCanvasVisible(bool visible)
        {
            if (targetCanvas != null)
            {
                targetCanvas.enabled = visible;
            }

            if (graphicRaycaster != null)
            {
                graphicRaycaster.enabled = visible;
            }

            if (targetCanvas == null && graphicRaycaster == null)
            {
                gameObject.SetActive(visible);
            }
        }
    }

    public static class EndingMenuUnlockState
    {
        public const string SaveKey = "feature.endingmenu.unlocked";

        public static bool Load()
        {
            if (!ES3.KeyExists(SaveKey, SaveKeys.Settings))
            {
                return false;
            }

            return ES3.Load<bool>(SaveKey, SaveKeys.Settings);
        }

        public static void Save(bool unlocked)
        {
            ES3.Save(SaveKey, unlocked, SaveKeys.Settings);
        }

        public static void Clear()
        {
            if (ES3.KeyExists(SaveKey, SaveKeys.Settings))
            {
                ES3.DeleteKey(SaveKey, SaveKeys.Settings);
            }
        }
    }
}
