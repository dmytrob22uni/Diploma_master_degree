using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;

namespace Coursework_Master_Degree.Items.Ghosts.Background
{
    public class ItemGhostBackgroundDataHolder : ItemGhostDataHolder
    {
        public bool IsHavePartsToSync;

        [SerializeField]
        [ShowIf("IsHavePartsToSync")]
        public Dictionary<ItemSyncToGhostPartTypeSO, Transform> PartsToTransformsToSyncDictionary;

        private void OnValidate()
        {
            base.ItemGhostType = ItemGhostType.Background;
        }

        private void Awake()
        {
            base.ItemGhostType = ItemGhostType.Background;
        }
    }
}
