using System.Collections.Generic;
using UnityEngine;

namespace ThatGameJam.Features.BackpackFeature.Models
{
    [CreateAssetMenu(menuName = "ThatGameJam/Items/ItemDefinition", fileName = "ItemDefinition")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Texture2D icon;
        [SerializeField] private GameObject dropPrefab;
        [SerializeField] private bool stackable;
        [SerializeField] private int maxStack;
        [SerializeField] private string[] tags;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;
        public Texture2D Icon => icon;
        public GameObject DropPrefab => dropPrefab;
        public bool Stackable => stackable;
        public int MaxStack => maxStack;
        public IReadOnlyList<string> Tags => tags;
    }
}
