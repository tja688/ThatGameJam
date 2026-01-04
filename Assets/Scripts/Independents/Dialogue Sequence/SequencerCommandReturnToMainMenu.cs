using PixelCrushers.DialogueSystem;
using ThatGameJam.UI.Services;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: ReturnToMainMenu()
    ///
    /// Calls IGameFlowService.ReturnToMainMenu() via UIServiceRegistry.
    /// </summary>
    public class SequencerCommandReturnToMainMenu : SequencerCommand
    {
        public void Awake()
        {
            var gameFlow = UIServiceRegistry.GameFlow;
            if (gameFlow == null)
            {
                if (DialogueDebug.logWarnings)
                {
                    Debug.LogWarning("SequencerCommandReturnToMainMenu: UIServiceRegistry.GameFlow not registered.");
                }

                Stop();
                return;
            }

            gameFlow.ReturnToMainMenu();
            Stop();
        }
    }
}
