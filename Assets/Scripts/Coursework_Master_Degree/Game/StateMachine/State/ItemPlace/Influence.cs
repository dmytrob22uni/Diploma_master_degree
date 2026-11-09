using Coursework_Master_Degree.Items;

namespace Coursework_Master_Degree.Game.StateMachine.State.ItemPlace
{
    public class Influence
    {
        // Movement
        public bool IsAllowedJump = true;
        public bool IsAllowedCrouch = true;
        public bool IsAllowedWalk = true;
        public bool IsAllowedRun = true;
        public bool IsAllowedZoom = true;
        public bool IsRestrictedLook = true;
        public float Move = 100.0f;
        public float Jump = 100.0f;
        public float Crouch = 100.0f;
        public float Walk = 100.0f;
        public float Run = 100.0f;
        public float UpLimitLook = 100.0f;
        public float DownLimitLook = 100.0f;

        // Reach
        public bool IsInstallable = false;
        public bool IsActionable = false;
        public float Placement = 100.0f;
        public float Installation = 100.0f;
        public float Action = 100.0f;

        public static Influence CreateFrom(ItemDataHolder item)
        {
            return new Influence()
            {
                // Movement
                // TODO: rename ugly and old-architecture-based fields of influence SO
                IsAllowedJump = item.ItemToPlayerInfluencesSO.ItemToPlayerMovementInfluenceData.IsAllowedJump,
                IsAllowedCrouch = item.ItemToPlayerInfluencesSO.ItemToPlayerMovementInfluenceData.IsAllowedCrouch,
                IsAllowedWalk = item.ItemToPlayerInfluencesSO.ItemToPlayerMovementInfluenceData.IsAllowedSlowDown,
                IsAllowedRun = item.ItemToPlayerInfluencesSO.ItemToPlayerMovementInfluenceData.IsAllowedSpeedUp,
                IsAllowedZoom = item.ItemToPlayerInfluencesSO.ItemToPlayerMovementInfluenceData.IsAllowedZoom,
                IsRestrictedLook = item.ItemToPlayerInfluencesSO.ItemToPlayerMovementInfluenceData.IsRestrictedLook,
                Move = item.ItemToPlayerInfluencesSO.ItemToPlayerMovementInfluenceData.MoveSpeedPercent,
                Jump = item.ItemToPlayerInfluencesSO.ItemToPlayerMovementInfluenceData.JumpSpeedPercent,
                Crouch = item.ItemToPlayerInfluencesSO.ItemToPlayerMovementInfluenceData.CrouchSpeedPercent,
                Walk = item.ItemToPlayerInfluencesSO.ItemToPlayerMovementInfluenceData.SlowDownSpeedPercent,
                Run = item.ItemToPlayerInfluencesSO.ItemToPlayerMovementInfluenceData.SpeedUpSpeedPercent,
                UpLimitLook = item.ItemToPlayerInfluencesSO.ItemToPlayerMovementInfluenceData.UpLimitLookAnglePercent,
                DownLimitLook = item.ItemToPlayerInfluencesSO.ItemToPlayerMovementInfluenceData.DownLimitLookAnglePercent,

                // Reach
                // TODO: rename ugly and old-architecture-based fields of influence SO
                IsInstallable = item.ItemToPlayerInfluencesSO.ItemToPlayerReachInfluenceData.IsInstallableItem,
                IsActionable = item.ItemToPlayerInfluencesSO.ItemToPlayerReachInfluenceData.IsActionableItem,
                Placement = item.ItemToPlayerInfluencesSO.ItemToPlayerReachInfluenceData.PlacementReachPercent,
                Installation = item.ItemToPlayerInfluencesSO.ItemToPlayerReachInfluenceData.InstallationReachPercent,
                Action = item.ItemToPlayerInfluencesSO.ItemToPlayerReachInfluenceData.ActionReachPercent
            };
        }
    }
}
