using System;
using UnityEngine;
using NaughtyAttributes;
using Coursework_Master_Degree.Items.Wear.Delta;
using Coursework_Master_Degree.ScriptableObjects.Items.Wear;

namespace Coursework_Master_Degree.Items.Wear
{
    public class ItemWearState : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        public event Action OnItemWornOut;

        [Tooltip("Is allow initial random condition generation in range from 95 to 100")]
        public bool IsAllowRandomInitialCondition = true;

        [SerializeField]
        [Tooltip("Set on Awake. Exposed for debugging")]
        private float currentCondition;
        public float CurrentCondition { get { return currentCondition; } }

        private float maxCondition = 100.0f;
        public float MaxCondition { get { return maxCondition; } }

        private float minCondition = 0.0f;
        public float MinCondition { get { return minCondition; } }

        [SerializeField]
        [EnableIf("NotEnabled")]
        private bool isWornOut = false;
        public bool IsWornOut { get { return isWornOut; } }

        public ItemWearLogicSO ItemWearLogicSO;

        private void OnValidate()
        {
            GenerateInitialCondition();
        }

        private void Awake()
        {
            GenerateInitialCondition();
        }

        private void GenerateInitialCondition()
        {
            if (IsAllowRandomInitialCondition)
            {
                currentCondition = UnityEngine.Random.Range(95.0f, 100.0f);
            }
            else if (currentCondition <= minCondition)
            {
                // A disabled randomizer is an authored condition; only an
                // unset (0) value falls back to pristine.
                currentCondition = maxCondition;
            }

            isWornOut = currentCondition <= minCondition;
        }

        [ContextMenu("Debug/Set Condition 100%")]
        private void DebugSetCondition100()
        {
            SetConditionForDebug(100.0f);
        }

        [ContextMenu("Debug/Set Condition 50%")]
        private void DebugSetCondition50()
        {
            SetConditionForDebug(50.0f);
        }

        [ContextMenu("Debug/Set Condition 10%")]
        private void DebugSetCondition10()
        {
            SetConditionForDebug(10.0f);
        }

        /// <summary>
        /// Calibration helper. Not used by gameplay; lets the M2 gate test
        /// spool and critical-part thresholds without editing assets.
        /// </summary>
        public void SetConditionForDebug(float condition)
        {
            currentCondition = Mathf.Clamp(condition, minCondition, maxCondition);
            isWornOut = currentCondition <= minCondition;
        }

        public void ApplyWear(WearDelta wearDelta)
        {
            if (isWornOut)
            {
                return;
            }

            float wearValue = ItemWearLogicSO.CalculateWear(wearDelta);

            currentCondition = Mathf.Clamp(
                currentCondition - wearValue,
                minCondition,
                maxCondition
            );

            if (currentCondition == minCondition)
            {
                isWornOut = true;

                OnItemWornOut?.Invoke();

                return;
            }
        }
    }
}
