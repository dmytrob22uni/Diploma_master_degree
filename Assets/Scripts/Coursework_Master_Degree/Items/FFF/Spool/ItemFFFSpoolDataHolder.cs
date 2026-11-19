using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.ScriptableObjects.Items.Types;

namespace Coursework_Master_Degree.Items.FFF.Spool
{
    public class ItemFFFSpoolDataHolder : MonoBehaviour
    {
        [Range(0.0f, 1.0f)]
        public float MaterialHardness;

        [Range(0.0f, 1.0f)]
        public float MaterialAbrasiveness;

        public ItemType MaterialType;
        public ItemType ColorType;

        [Tooltip("Optional thermal profile used by M3; M2 checks MaterialType only.")]
        public FilamentMaterialProfileSO MaterialProfile;

        public Material FinalMaterial;
        public Material PrintingMaterial;

        [SerializeField]
        public Dictionary<float, GameObject> SpoolWearLevelsToGameObjectsDictionary;

        [Header("Consumption Visual")]
        [Tooltip("Transform rotated while filament is consumed. Auto-finds a child named 'spool'.")]
        public Transform RotatingPart;

        [Tooltip("Local axis around which the spool turns.")]
        public Vector3 RotationAxis = Vector3.forward;

        [Min(0.001f)]
        [Tooltip("Effective filament radius in metres. Rotation = metres / circumference.")]
        public float EffectiveRadiusMeters = 0.1f;

        [Tooltip("Flip the rotation direction if the filament unwinds the other way.")]
        public bool InvertRotation;

        private readonly List<float> wearKeys = new List<float>();
        private float lastWearKey = float.NaN;

        [Min(10.0f)]
        [Tooltip(
            "Initial amount of filament on the spool in metres.\n" +
            "Good length is about 300 m. Examples:\n" +
            "PLA: 330 m\n" +
            "PETG: 300 m\n" +
            "ABS: 400 m\n" +
            "TPU: 360 m\n")]
        public float FilamentLengthSpoolMeters = 300.0f;

        private void Awake()
        {
            ResolveVisuals();
        }

        private void Start()
        {
            ItemWearState wear =
                GetComponentInChildren<ItemWearState>(true);

            RefreshWearVisual(wear != null ? wear.CurrentCondition : 100.0f);
        }

        /// <summary>
        /// Rotates the spool by the amount of filament just consumed.
        /// The caller passes simulated metres, so the visual speed follows the
        /// printer's simulation multiplier automatically.
        /// </summary>
        public void RotateForFilament(float meters)
        {
            if (RotatingPart == null || meters <= 0.0f)
            {
                return;
            }

            float radius = Mathf.Max(0.001f, EffectiveRadiusMeters);
            float circumference = 2.0f * Mathf.PI * radius;
            float degrees = meters / circumference * 360.0f;

            if (InvertRotation)
            {
                degrees = -degrees;
            }

            RotatingPart.Rotate(RotationAxis, degrees, Space.Self);
        }

        /// <summary>
        /// Shows the child whose authored wear threshold is nearest to the
        /// current condition. The dictionary keys are 0..100 condition values.
        /// </summary>
        public void RefreshWearVisual(float condition)
        {
            if (SpoolWearLevelsToGameObjectsDictionary == null ||
                SpoolWearLevelsToGameObjectsDictionary.Count == 0)
            {
                return;
            }

            if (wearKeys.Count == 0)
            {
                wearKeys.AddRange(
                    SpoolWearLevelsToGameObjectsDictionary.Keys);
                wearKeys.Sort();
            }

            float clamped = Mathf.Clamp(condition, 0.0f, 100.0f);
            float selected = wearKeys[0];
            float bestDelta = Mathf.Abs(selected - clamped);

            for (int i = 1; i < wearKeys.Count; i++)
            {
                float delta = Mathf.Abs(wearKeys[i] - clamped);

                if (delta < bestDelta)
                {
                    selected = wearKeys[i];
                    bestDelta = delta;
                }
            }

            if (!float.IsNaN(lastWearKey) &&
                Mathf.Approximately(lastWearKey, selected))
            {
                return;
            }

            lastWearKey = selected;

            foreach (KeyValuePair<float, GameObject> pair in
                SpoolWearLevelsToGameObjectsDictionary)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                pair.Value.SetActive(
                    Mathf.Approximately(pair.Key, selected));
            }
        }

        private void ResolveVisuals()
        {
            if (RotatingPart != null)
            {
                return;
            }

            Transform candidate = transform.Find("spool");

            if (candidate == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.IndexOf(
                            "spool",
                            System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        candidate = child;
                        break;
                    }
                }
            }

            RotatingPart = candidate != null ? candidate : transform;
        }
    }
}
