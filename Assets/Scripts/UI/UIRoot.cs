using UnityEngine;
using UnityEngine.UIElements;

namespace ThatGameJam.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class UIRoot : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private UIRouter router;
        [SerializeField] private UIPauseService pauseService;

        [Header("Styles")]
        [SerializeField] private StyleSheet themeStyle;
        [SerializeField] private StyleSheet componentsStyle;
        [SerializeField] private StyleSheet menusStyle;

        [Header("UXML Assets")]
        [SerializeField] private UIPanelAssets panelAssets;

        [Header("Startup")]
        [SerializeField] private bool showMainMenuOnStart = true;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (router == null)
            {
                router = GetComponent<UIRouter>();
            }

            if (pauseService == null)
            {
                pauseService = GetComponent<UIPauseService>();
            }

            if (panelAssets == null)
            {
                panelAssets = new UIPanelAssets();
                Debug.LogWarning("UIRoot panel assets are not assigned. UI will render empty until set.");
            }

            if (router != null)
            {
                router.Initialize(uiDocument, panelAssets, new[] { themeStyle, componentsStyle, menusStyle }, pauseService);
            }
        }

        private void Start()
        {
            if (showMainMenuOnStart && router != null)
            {
                router.OpenMainMenu();
            }
        }

        public void OpenPlayerPanel()
        {
            router?.OpenPlayerPanel();
        }

        public void OpenSettingsFromGame()
        {
            router?.OpenSettings(SettingsOpenedFrom.InGame);
        }
    }
}
