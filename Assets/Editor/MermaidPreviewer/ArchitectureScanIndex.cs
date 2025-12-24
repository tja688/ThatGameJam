using System;
using System.Collections.Generic;

namespace MermaidPreviewer
{
    [Serializable]
    internal sealed class ArchitectureScanIndex
    {
        public string timestamp;
        public string scanRoot;
        public string l0MermaidPath;
        public List<ArchitectureFeatureIndex> features = new List<ArchitectureFeatureIndex>();
    }

    [Serializable]
    internal sealed class ArchitectureFeatureIndex
    {
        public string id;
        public string name;
        public string l1MermaidPath;
        public string l2MermaidPath;
        public List<ArchitectureComponentIndex> components = new List<ArchitectureComponentIndex>();
    }

    [Serializable]
    internal sealed class ArchitectureComponentIndex
    {
        public string name;
        public string category;
        public string l2MermaidPath;
    }
}
