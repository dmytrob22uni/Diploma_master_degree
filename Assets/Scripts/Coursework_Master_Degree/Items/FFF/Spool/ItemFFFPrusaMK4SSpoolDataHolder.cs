using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.Items.Types;

namespace Coursework_Master_Degree.Items.FFF.Spool
{
    public class ItemFFFPrusaMK4SSpoolDataHolder : MonoBehaviour
    {
        [Range(0.0f, 1.0f)]
        public float MaterialHardness;

        [Range(0.0f, 1.0f)]
        public float MaterialAbrasiveness;

        public ItemType MaterialType;
        public ItemType ColorType;
        public Material Material;

        [Min(10.0f)]
        [Tooltip(
            "Initial amount of filament on the spool in metres.\n" +
            "Good length is about 300 m. Examples:\n" +
            "PLA: 330 m\n" +
            "PETG: 300 m\n" +
            "ABS: 400 m\n" +
            "TPU: 360 m\n")]
        public float FilamentLengthSpoolMeters = 300.0f;
    }
}
