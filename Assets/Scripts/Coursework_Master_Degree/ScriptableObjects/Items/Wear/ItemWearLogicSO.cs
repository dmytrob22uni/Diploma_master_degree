using System;
using UnityEngine;
using NaughtyAttributes;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.Items.Wear.Delta;

namespace Coursework_Master_Degree.ScriptableObjects.Items.Wear
{
    public abstract class ItemWearLogicSO : ScriptableObject
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        [SerializeField]
        [EnableIf("NotEnabled")]
        [Tooltip(
            "Global multiplier to speed up / slow down items wear all at once.\n" +
            "Doesn't influence:\n" +
            "- Uses wear type\n" +
            "- UsesHardness wear type\n" +
            "Value is set via code")]
        private float WearRatio = 1.0f;

        public float CalculateWearByRatio(WearDelta wearDelta)
        {
            wearDelta.WearRatio = WearRatio;

            return CalculateWear(wearDelta);
        }

        public abstract float CalculateWear(WearDelta wearDelta);
    }
}
