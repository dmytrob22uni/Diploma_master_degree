using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.Items.Types;

namespace Coursework_Master_Degree.ScriptableObjects.Items
{
    /// <summary>
    /// Thermal and physical data for one filament material. M2 only stores
    /// it; M3 uses the temperatures and cooling rate.
    /// </summary>
    [CreateAssetMenu(
        fileName = "filament_material_profile_SO",
        menuName = "Game Items/Filament Material Profile SO",
        order = 4)]
    public class FilamentMaterialProfileSO : ScriptableObject
    {
        public ItemType MaterialType = ItemType.PLA;

        [Header("Temperatures (C)")]
        [Min(0.0f)]
        public float NozzleTemperature = 215.0f;

        [Min(0.0f)]
        public float BedTemperature = 60.0f;

        [Min(0.0f)]
        public float RecommendedChamberTemperature = 21.0f;

        [Header("Physical")]
        [Min(0.01f)]
        public float Density = 1.24f;

        [Range(0.0f, 1.0f)]
        public float WarpFactor = 0.1f;

        [Min(0.0f)]
        public float CoolingRateMultiplier = 1.0f;
    }
}
