using System;
using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Surface;

namespace Coursework_Master_Degree.Game.StateMachine.State.ItemPlace
{
    public class ItemPlaceData
    {
        public GameObject ItemGameObject;
        public GameState ReturnGameState;
        public Action ClearParentSurface;
        public Action<ItemSurfaceDataHolder> SetParentSurface;
        public string ItemName;
        public GameObject GhostGameObject;
        public List<MeshRenderer> GhostMeshRenderers;
        public List<Transform> GhostCorners;
    }
}
