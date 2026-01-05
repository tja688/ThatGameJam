using System.Collections;
using ThatGameJam.UI;
using UnityEngine;

namespace ThatGameJam.Independents.Audio
{
    public class BackgroundMusicManager : MonoBehaviour
    {
        public static BackgroundMusicManager Instance { get; private set; }

        [Header("Audio Event Ids")]
        [SerializeField] private string mainMenuEventId = "BGM-MENU-0001";
        [SerializeField] private string gameplayEventId = "BGM-GAME-0001";
        [SerializeField] private string towerTopEventId = "BGM-TOWER-0001";
        [SerializeField] private string ending1EventId = "BGM-END-0001";
        [SerializeField] private string ending2EventId = "BGM-END-0002";

        [Header("Gameplay Detection")]
        [SerializeField] private UIRouter uiRouter;
        [SerializeField] private bool findUiRouterOnEnable = true;
        [SerializeField] private bool requireNoOpenPanels = true;
        [SerializeField] private bool requireTimeScaleRunning = true;
        [SerializeField] private float gameplayFallbackInterval = 10f;

        private string _currentEventId;
        private BgmState _currentState = BgmState.None;
        private Coroutine _fallbackRoutine;

        public bool HasActiveMusic => !string.IsNullOrWhiteSpace(_currentEventId);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            if (findUiRouterOnEnable && uiRouter == null)
            {
                uiRouter = FindObjectOfType<UIRouter>();
            }

            if (uiRouter != null)
            {
                uiRouter.MainMenuVisibilityChanged -= HandleMainMenuVisibilityChanged;
                uiRouter.MainMenuVisibilityChanged += HandleMainMenuVisibilityChanged;
                HandleMainMenuVisibilityChanged(uiRouter.IsMainMenuOnTop);
            }

            StartFallbackRoutine();
        }

        private void OnDisable()
        {
            if (uiRouter != null)
            {
                uiRouter.MainMenuVisibilityChanged -= HandleMainMenuVisibilityChanged;
            }

            StopFallbackRoutine();
        }

        public void PlayMainMenuMusic()
        {
            SwitchMusic(BgmState.MainMenu, mainMenuEventId, true);
        }

        public void PlayGameplayMusic()
        {
            SwitchMusic(BgmState.Gameplay, gameplayEventId, false);
        }

        public void PlayTowerTopMusic()
        {
            SwitchMusic(BgmState.TowerTop, towerTopEventId, false);
        }

        public void PlayEndingMusic(int endingIndex)
        {
            if (endingIndex == 1)
            {
                SwitchMusic(BgmState.Ending1, ending1EventId, false);
            }
            else if (endingIndex == 2)
            {
                SwitchMusic(BgmState.Ending2, ending2EventId, false);
            }
            else
            {
                Debug.LogWarning($"BackgroundMusicManager: Invalid ending index {endingIndex}. Use 1 or 2.", this);
            }
        }

        public void StopAllMusic()
        {
            StopCurrent();
            _currentState = BgmState.None;
        }

        private void HandleMainMenuVisibilityChanged(bool isVisible)
        {
            if (isVisible)
            {
                PlayMainMenuMusic();
                return;
            }

            if (_currentState == BgmState.MainMenu)
            {
                StopCurrent();
            }

            TryStartGameplayFallback();
        }

        private void StartFallbackRoutine()
        {
            StopFallbackRoutine();
            if (gameplayFallbackInterval <= 0f)
            {
                return;
            }

            _fallbackRoutine = StartCoroutine(GameplayFallbackLoop());
        }

        private void StopFallbackRoutine()
        {
            if (_fallbackRoutine == null)
            {
                return;
            }

            StopCoroutine(_fallbackRoutine);
            _fallbackRoutine = null;
        }

        private IEnumerator GameplayFallbackLoop()
        {
            float interval = Mathf.Max(1f, gameplayFallbackInterval);
            var wait = new WaitForSecondsRealtime(interval);
            while (true)
            {
                yield return wait;
                TryStartGameplayFallback();
            }
        }

        private void TryStartGameplayFallback()
        {
            if (!IsGameplayActive())
            {
                return;
            }

            if (HasActiveMusic)
            {
                return;
            }

            PlayGameplayMusic();
        }

        private bool IsGameplayActive()
        {
            if (requireTimeScaleRunning && Time.timeScale <= 0f)
            {
                return false;
            }

            if (uiRouter != null)
            {
                if (uiRouter.IsMainMenuOnTop)
                {
                    return false;
                }

                if (requireNoOpenPanels && uiRouter.HasOpenPanels)
                {
                    return false;
                }
            }

            return true;
        }

        private void SwitchMusic(BgmState state, string eventId, bool stopIfEmpty)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                if (stopIfEmpty)
                {
                    StopCurrent();
                    _currentState = BgmState.None;
                }

                return;
            }

            if (_currentState == state && _currentEventId == eventId)
            {
                return;
            }

            StopCurrent();

            AudioService.Play(eventId, new AudioContext
            {
                Owner = transform
            });

            _currentEventId = eventId;
            _currentState = state;
        }

        private void StopCurrent()
        {
            if (string.IsNullOrWhiteSpace(_currentEventId))
            {
                return;
            }

            AudioService.Stop(_currentEventId, new AudioContext
            {
                Owner = transform
            });

            _currentEventId = null;
        }

        private enum BgmState
        {
            None,
            MainMenu,
            Gameplay,
            TowerTop,
            Ending1,
            Ending2
        }
    }
}
