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

        [SerializeField]
        [EnableIf("NotEnabled")]
        [Tooltip("Initial condition is randomly generated on Awake: 90 to 100")]
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

        public void GenerateInitialCondition()
        {
            currentCondition = UnityEngine.Random.Range(90.0f, 100.0f);
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
