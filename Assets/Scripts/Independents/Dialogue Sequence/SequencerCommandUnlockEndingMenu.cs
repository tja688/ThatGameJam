using PixelCrushers.DialogueSystem;
using ThatGameJam.Independents;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: UnlockEndingMenu()
    ///
    /// Persists the ending menu unlock and updates any active controllers.
    /// </summary>
    public class SequencerCommandUnlockEndingMenu : SequencerCommand
    {
        public void Awake()
        {
            EndingMenuUnlockState.Save(true);
            RefreshSceneControllers();

            if (DialogueDebug.logInfo)
            {
                Debug.Log("SequencerCommandUnlockEndingMenu: Ending menu unlocked.");
            }

            Stop();
        }

        private static void RefreshSceneControllers()
        {
            var controllers = Resources.FindObjectsOfTypeAll<EndingMenuUnlockController>();
            for (int i = 0; i < controllers.Length; i++)
            {
                var controller = controllers[i];
                if (controller == null)
                {
                    continue;
                }

                if (!controller.gameObject.scene.IsValid())
                {
                    continue;
                }

                controller.SetUnlocked(true, false);
            }
        }
    }
}
