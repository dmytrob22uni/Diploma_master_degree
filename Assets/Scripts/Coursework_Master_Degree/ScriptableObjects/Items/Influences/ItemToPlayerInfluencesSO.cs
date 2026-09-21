using UnityEngine;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Influences
{
    [CreateAssetMenu(fileName = "item_to_player_influences_SO", menuName = "Game Items/Influence/Item To Player Influences SO", order = 0)]
    public class ItemToPlayerInfluencesSO : ScriptableObject
    {
        public ItemToPlayerMovementInfluenceSO ItemToPlayerMovementInfluenceData;
        public ItemToPlayerReachInfluenceSO ItemToPlayerReachInfluenceData;
    }
}
