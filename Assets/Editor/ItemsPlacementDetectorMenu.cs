using UnityEditor;
using UnityEngine;
using Coursework_Master_Degree.Game.Helper;

namespace Coursework_Master_Degree.Editor
{
    public static class ItemsPlacementDetectorMenu
    {
        [MenuItem("Tools/Detect Placed Items")]
        public static void DetectPlacedItems()
        {
            var detectors = Object.FindObjectsByType<ItemsPlacementDetector>(FindObjectsInactive.Include);

            if (detectors.Length == 0)
            {
                Debug.LogWarning(
                    "[ItemsPlacementDetector] No ItemsPlacementDetector was found in the open scenes.");
                return;
            }

            foreach (var detector in detectors)
            {
                detector.DetectPlacedItems();
            }
        }
    }
}
