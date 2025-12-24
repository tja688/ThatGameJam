using System.Collections.Generic;
using UnityEngine;

namespace MermaidPreviewer
{
    internal enum ArchitectureFeatureMode
    {
        Path,
        Namespace
    }

    internal sealed class ArchitectureScannerSettings : ScriptableObject
    {
        public string scanRootPath = "Assets/Scripts";
        public string outputFolderPath = "Assets/ArchitectureScanResults";
        public List<string> excludePatterns = new List<string>();
        public ArchitectureFeatureMode featureMode = ArchitectureFeatureMode.Path;
        public string featurePathToken = "Features";
        public string namespacePrefix = "ThatGameJam.Features";
        public string lastSelectedNavId;

        public void EnsureDefaults()
        {
            if (excludePatterns == null || excludePatterns.Count == 0)
            {
                excludePatterns = new List<string>
                {
                    "**/Editor/**",
                    "**/Tests/**",
                    "**/ThirdParty/**",
                    "**/Temp/**"
                };
            }

            if (string.IsNullOrWhiteSpace(scanRootPath))
            {
                scanRootPath = "Assets/Scripts";
            }

            if (string.IsNullOrWhiteSpace(outputFolderPath))
            {
                outputFolderPath = "Assets/ArchitectureScanResults";
            }

            if (string.IsNullOrWhiteSpace(featurePathToken))
            {
                featurePathToken = "Features";
            }

            if (string.IsNullOrWhiteSpace(namespacePrefix))
            {
                namespacePrefix = "ThatGameJam.Features";
            }
        }
    }
}
