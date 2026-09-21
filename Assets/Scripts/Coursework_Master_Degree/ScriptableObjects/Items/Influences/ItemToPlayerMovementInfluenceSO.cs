using UnityEngine;
using NaughtyAttributes;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Influences
{
    [CreateAssetMenu(fileName = "item_to_player_movement_influence_SO", menuName = "Game Items/Influences/Item To Player Movement Influence SO", order = 1)]
    public class ItemToPlayerMovementInfluenceSO : ScriptableObject
    {
        [MinValue(1.0f), MaxValue(100.0f)]
        public float MoveSpeedPercent = 90.0f;

        public bool IsAllowedCrouch = true;
        [ShowIf("IsAllowedCrouch")]
        [MinValue(1.0f), MaxValue(100.0f)]
        public float CrouchSpeedPercent = 100.0f;

        public bool IsAllowedSlowDown = true;
        [ShowIf("IsAllowedSlowDown")]
        [MinValue(1.0f), MaxValue(100.0f)]
        public float SlowDownSpeedPercent = 100.0f;

        public bool IsAllowedSpeedUp = false;
        [ShowIf("IsAllowedSpeedUp")]
        [MinValue(1.0f), MaxValue(100.0f)]
        public float SpeedUpSpeedPercent = 50.0f;

        public bool IsAllowedZoom = false;

        public bool IsRestrictedLook = true;
        [ShowIf("IsRestrictedLook")]
        [MinValue(1.0f), MaxValue(100.0f)]
        public float UpLimitLookAnglePercent = 80.0f;
        [ShowIf("IsRestrictedLook")]
        [MinValue(1.0f), MaxValue(100.0f)]
        public float DownLimitLookAnglePercent = 100.0f;
    }
}
