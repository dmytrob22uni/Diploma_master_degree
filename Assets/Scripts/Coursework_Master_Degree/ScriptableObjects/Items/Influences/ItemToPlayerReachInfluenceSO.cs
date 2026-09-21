using UnityEngine;
using NaughtyAttributes;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Influences
{
    [CreateAssetMenu(fileName = "item_to_player_reach_influence_SO", menuName = "Game Items/Influence/Item To Player Reach Influence SO", order = 2)]
    public class ItemToPlayerReachInfluenceSO : ScriptableObject
    {
        [MinValue(1.0f), MaxValue(300.0f)]
        public float PlacementReachPercent = 100.0f;

        public bool IsInstallableItem = false;
        [ShowIf("IsInstallableItem")]
        [MinValue(1.0f), MaxValue(100.0f)]
        public float InstallationReachPercent = 100.0f;

        public bool IsActionableItem = false;
        [ShowIf("IsActionableItem")]
        [MinValue(1.0f), MaxValue(100.0f)]
        public float ActionReachPercent = 100.0f;

        public bool IsSubactionableItem = false;
        [ShowIf("IsSubactionableItem")]
        [MinValue(1.0f), MaxValue(100.0f)]
        public float SubactionReachPercent = 100.0f;
    }
}
