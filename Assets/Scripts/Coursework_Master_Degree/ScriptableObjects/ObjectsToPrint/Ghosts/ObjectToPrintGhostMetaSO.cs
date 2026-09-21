using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.ObjectsToPrint.Ghost;

namespace Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost
{
    [CreateAssetMenu(fileName = "object_to_print_ghost_meta_SO", menuName = "Game Items/Ghosts/Object To Print Ghost Meta SO", order = 0)]
    public class ObjectToPrintGhostMetaSO : ScriptableObject
    {
        public List<ObjectToPrintTypeToPrefab> ObjectToPrintTypesToPrefabsList;
    }
}
