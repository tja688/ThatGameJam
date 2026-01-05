using System;
using System.Linq;
using System.Reflection;
using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace ThatGameJam.Independents
{
    /// <summary>
    /// Diagnostic tool to check if custom Sequencer commands are properly registered.
    /// Attach this to any GameObject and it will log diagnostic info on Start.
    /// </summary>
    public class SequencerCommandDiagnostic : MonoBehaviour
    {
        private const string LOG_TAG = "[SeqCmd-Diag] ";

        [Header("Settings")]
        [Tooltip("List of custom sequencer command names to check (without 'SequencerCommand' prefix)")]
        [SerializeField] private string[] commandsToCheck = new string[]
        {
            "EnvelopeReadShow",
            "ActivateByName",
            "CanvasGroupFade",
            "DialogueImage",
            "GiveItem",
            "HideInteractionAndSprite",
            "PlayEndingBgm",
            "ReturnToMainMenu",
            "UnlockEndingMenu"
        };

        [Tooltip("Run diagnostic on Start")]
        [SerializeField] private bool runOnStart = true;

        private void Start()
        {
            if (runOnStart)
            {
                RunDiagnostic();
            }
        }

        [ContextMenu("Run Diagnostic")]
        public void RunDiagnostic()
        {
            Debug.Log($"{LOG_TAG}========== SEQUENCER COMMAND DIAGNOSTIC START ==========");
            Debug.Log($"{LOG_TAG}Time: {Time.time}, Frame: {Time.frameCount}");
            Debug.Log($"{LOG_TAG}Platform: {Application.platform}");
            Debug.Log($"{LOG_TAG}Unity Version: {Application.unityVersion}");
            Debug.Log($"{LOG_TAG}Is Editor: {Application.isEditor}");
            Debug.Log($"{LOG_TAG}Scripting Backend: {GetScriptingBackend()}");

            // Check all loaded assemblies
            Debug.Log($"{LOG_TAG}");
            Debug.Log($"{LOG_TAG}--- Loaded Assemblies ---");
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Debug.Log($"{LOG_TAG}Total assemblies loaded: {assemblies.Length}");

            // Find Assembly-CSharp
            var asmCSharp = assemblies.FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
            if (asmCSharp != null)
            {
                Debug.Log($"{LOG_TAG}Assembly-CSharp found: {asmCSharp.FullName}");
            }
            else
            {
                Debug.LogError($"{LOG_TAG}ERROR: Assembly-CSharp NOT FOUND!");
            }

            // Check for SequencerCommands namespace
            Debug.Log($"{LOG_TAG}");
            Debug.Log($"{LOG_TAG}--- Checking SequencerCommands Namespace ---");

            var sequencerCommandTypes = assemblies
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        Debug.LogWarning($"{LOG_TAG}Could not load types from {a.GetName().Name}: {ex.Message}");
                        return ex.Types.Where(t => t != null);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"{LOG_TAG}Could not load types from {a.GetName().Name}: {ex.Message}");
                        return Array.Empty<Type>();
                    }
                })
                .Where(t => t != null && t.Namespace == "PixelCrushers.DialogueSystem.SequencerCommands")
                .ToList();

            Debug.Log($"{LOG_TAG}Found {sequencerCommandTypes.Count} types in PixelCrushers.DialogueSystem.SequencerCommands namespace:");
            foreach (var type in sequencerCommandTypes.OrderBy(t => t.Name))
            {
                bool isCustom = type.Assembly.GetName().Name == "Assembly-CSharp";
                Debug.Log($"{LOG_TAG}  - {type.Name} (Assembly: {type.Assembly.GetName().Name}) {(isCustom ? "[CUSTOM]" : "")}");
            }

            // Check specific commands
            Debug.Log($"{LOG_TAG}");
            Debug.Log($"{LOG_TAG}--- Checking Specific Custom Commands ---");

            foreach (var cmdName in commandsToCheck)
            {
                string fullTypeName = $"PixelCrushers.DialogueSystem.SequencerCommands.SequencerCommand{cmdName}";
                
                Type cmdType = null;
                foreach (var asm in assemblies)
                {
                    cmdType = asm.GetType(fullTypeName);
                    if (cmdType != null) break;
                }

                if (cmdType != null)
                {
                    Debug.Log($"{LOG_TAG}✓ {cmdName}: FOUND in {cmdType.Assembly.GetName().Name}");
                    
                    // Check if it inherits from SequencerCommand
                    var baseType = cmdType.BaseType;
                    while (baseType != null)
                    {
                        if (baseType.Name == "SequencerCommand")
                        {
                            Debug.Log($"{LOG_TAG}    Base class: SequencerCommand ✓");
                            break;
                        }
                        baseType = baseType.BaseType;
                    }

                    // Check for Awake method
                    var awakeMethod = cmdType.GetMethod("Awake", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (awakeMethod != null)
                    {
                        Debug.Log($"{LOG_TAG}    Awake method: Found ✓");
                    }
                    else
                    {
                        Debug.LogWarning($"{LOG_TAG}    Awake method: NOT FOUND!");
                    }
                }
                else
                {
                    Debug.LogError($"{LOG_TAG}✗ {cmdName}: NOT FOUND! This command may have been stripped by IL2CPP!");
                }
            }

            // Check EnvelopeReadShow class
            Debug.Log($"{LOG_TAG}");
            Debug.Log($"{LOG_TAG}--- Checking EnvelopeReadShow Class ---");
            
            Type envelopeType = null;
            foreach (var asm in assemblies)
            {
                envelopeType = asm.GetType("ThatGameJam.Independents.EnvelopeReadShow");
                if (envelopeType != null) break;
            }

            if (envelopeType != null)
            {
                Debug.Log($"{LOG_TAG}✓ EnvelopeReadShow class found in {envelopeType.Assembly.GetName().Name}");
            }
            else
            {
                Debug.LogError($"{LOG_TAG}✗ EnvelopeReadShow class NOT FOUND! This class may have been stripped!");
            }

            // Check if Dialogue System is present
            Debug.Log($"{LOG_TAG}");
            Debug.Log($"{LOG_TAG}--- Checking Dialogue System ---");

            var dialogueManager = FindAnyObjectByType<DialogueSystemController>();
            if (dialogueManager != null)
            {
                Debug.Log($"{LOG_TAG}✓ DialogueSystemController found: {dialogueManager.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"{LOG_TAG}DialogueSystemController not found in scene");
            }

            Debug.Log($"{LOG_TAG}========== SEQUENCER COMMAND DIAGNOSTIC END ==========");
        }

        private string GetScriptingBackend()
        {
#if ENABLE_IL2CPP
            return "IL2CPP";
#elif ENABLE_MONO
            return "Mono";
#else
            return "Unknown";
#endif
        }

        [ContextMenu("Test EnvelopeReadShow Command")]
        public void TestEnvelopeReadShowCommand()
        {
            Debug.Log($"{LOG_TAG}Testing EnvelopeReadShow command via Sequencer...");
            
            // This simulates what happens when Dialogue System tries to run the command
            string commandName = "EnvelopeReadShow";
            string fullTypeName = $"PixelCrushers.DialogueSystem.SequencerCommands.SequencerCommand{commandName}";
            
            Debug.Log($"{LOG_TAG}Looking for type: {fullTypeName}");
            
            Type cmdType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                cmdType = asm.GetType(fullTypeName);
                if (cmdType != null)
                {
                    Debug.Log($"{LOG_TAG}Found type in assembly: {asm.GetName().Name}");
                    break;
                }
            }

            if (cmdType != null)
            {
                Debug.Log($"{LOG_TAG}Type found! Attempting to create instance...");
                try
                {
                    var instance = Activator.CreateInstance(cmdType);
                    Debug.Log($"{LOG_TAG}Instance created successfully: {instance.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{LOG_TAG}Failed to create instance: {ex.Message}");
                }
            }
            else
            {
                Debug.LogError($"{LOG_TAG}Type NOT FOUND! The command will not work.");
            }
        }
    }
}
