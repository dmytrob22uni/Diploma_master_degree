using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Contracts;

namespace Coursework_Master_Degree.Game.StateMachine.State.ItemPlace
{
    public class ItemPlaceData
    {
        public GameObject ItemGameObject;
        public GameState ReturnGameState;
        public ISurfacePlaceable SurfacePlaceable;
        public string ItemName;
        public GameObject GhostGameObject;
        public List<MeshRenderer> GhostMeshRenderers;
        public List<Transform> GhostCorners;
    }
}
