using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ThatGameJam.Independents
{
    [DisallowMultipleComponent]
    public class DialogueImageDisplay : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Image targetImage;
        [SerializeField] private GameObject targetRoot;

        [Header("Sprites")]
        [SerializeField] private Sprite image1;
        [SerializeField] private Sprite image2;
        [SerializeField] private Sprite image3;

        [Header("Sequencer")]
        [SerializeField] private string submitMessage = "DialogueImageSubmit";

        private bool _isShowing;

        public string SubmitMessage => submitMessage;
        public bool IsShowing => _isShowing;

        private void Awake()
        {
            ResolveReferences();
            ClearVisual();
            SetRootActive(false);
        }

        private void OnDisable()
        {
            _isShowing = false;
        }

        public void ApplyKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                Hide();
                return;
            }

            int index = ParseIndex(key);
            if (index <= 0)
            {
                Debug.LogWarning($"DialogueImageDisplay: Invalid image key '{key}'. Use 1-3 or empty to clear.", this);
                return;
            }

            ShowByIndex(index);
        }

        public void ShowByIndex(int index)
        {
            ResolveReferences();

            Sprite sprite = GetSpriteByIndex(index);
            if (sprite == null)
            {
                Debug.LogWarning($"DialogueImageDisplay: Sprite for index {index} not assigned.", this);
                Hide();
                return;
            }

            SetImage(sprite);
        }

        public void Hide()
        {
            ResolveReferences();
            ClearVisual();
            SetRootActive(false);
            _isShowing = false;
        }

        private void Update()
        {
            if (!_isShowing)
            {
                return;
            }

            if (WasSubmitPressed() && !string.IsNullOrEmpty(submitMessage))
            {
                Sequencer.Message(submitMessage);
            }
        }

        private bool WasSubmitPressed()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }

            foreach (var gamepad in Gamepad.all)
            {
                if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetImage(Sprite sprite)
        {
            if (targetImage == null)
            {
                Debug.LogWarning("DialogueImageDisplay: Target Image not assigned.", this);
                return;
            }

            targetImage.sprite = sprite;
            targetImage.enabled = sprite != null;

            bool hasSprite = sprite != null;
            SetRootActive(hasSprite);
            _isShowing = hasSprite;
        }

        private Sprite GetSpriteByIndex(int index)
        {
            switch (index)
            {
                case 1:
                    return image1;
                case 2:
                    return image2;
                case 3:
                    return image3;
            }

            return null;
        }

        private int ParseIndex(string key)
        {
            for (int i = 0; i < key.Length; i++)
            {
                if (!char.IsDigit(key[i]))
                {
                    continue;
                }

                int digit = key[i] - '0';
                if (digit >= 1 && digit <= 3)
                {
                    return digit;
                }
            }

            return 0;
        }

        private void ResolveReferences()
        {
            if (targetImage == null)
            {
                if (targetRoot != null)
                {
                    targetImage = targetRoot.GetComponentInChildren<Image>(true);
                }

                if (targetImage == null)
                {
                    targetImage = GetComponentInChildren<Image>(true);
                }
            }

            if (targetRoot == null && targetImage != null)
            {
                targetRoot = targetImage.gameObject;
            }
        }

        private void ClearVisual()
        {
            if (targetImage == null)
            {
                return;
            }

            targetImage.sprite = null;
            targetImage.enabled = false;
        }

        private void SetRootActive(bool active)
        {
            if (targetRoot == null)
            {
                return;
            }

            if (targetRoot.activeSelf == active)
            {
                return;
            }

            targetRoot.SetActive(active);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif
    }
}
