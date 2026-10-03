using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts;

namespace Coursework_Master_Degree.Items
{
    public class ItemSyncToGhostBackgroundDataHolder : MonoBehaviour
    {
        public bool IsHavePartsToSync;

        [SerializeField]
        [ShowIf("IsHavePartsToSync")]
        public Dictionary<ItemSyncToGhostPartTypeSO, Transform> PartsToTransformsToSyncDictionary;

        public List<MeshRenderer> MeshRenderersToSyncList;
    }
}
