using System;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityCodeIntel.Editor
{
    [InitializeOnLoad]
    public class UnityCompilationWatcher
    {
        public static bool IsCompiling { get; private set; }
        public static DateTime LastCompileStartTime { get; private set; }
        public static DateTime LastCompileEndTime { get; private set; }
        public static bool LastCompileSuccess { get; private set; }

        static UnityCompilationWatcher()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            
            // If we are here, domain reload just finished (or editor started)
            // We can assume compilation was successful if we just reloaded? 
            // Not necessarily, but usually.
        }

        private static void OnCompilationStarted(object context)
        {
            IsCompiling = true;
            LastCompileStartTime = DateTime.Now;
        }

        private static void OnCompilationFinished(object context)
        {
            IsCompiling = false;
            LastCompileEndTime = DateTime.Now;
            // We can't easily know success/fail here for all assemblies, 
            // but generally this event fires.
            // If there are compile errors, Domain Reload won't happen.
            
            LastCompileSuccess = !EditorUtility.scriptCompilationFailed;
            
            if (!LastCompileSuccess)
            {
                Debug.LogWarning("[CodeIntel] Compilation failed. OmniSharp might be out of sync.");
            }
        }
    }
}
