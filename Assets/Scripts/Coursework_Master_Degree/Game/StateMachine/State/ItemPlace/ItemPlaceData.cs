using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Surface;

namespace Coursework_Master_Degree.Game.StateMachine.State.ItemPlace
{
    public class ItemPlaceData
    {
        public GameObject ItemGameObject;
        public Transform MovableTransform;
        public GameState ReturnGameState;
        public bool IsCancelAvailable;
        public bool IsInstallModeAvailable;
        public PartsAssembly InstallTargetAssembly;
        public string InstallBlockReason;
        public string ItemName;
        public GameObject GhostGameObject;
        public List<MeshRenderer> GhostMeshRenderers;
        public List<Transform> GhostCorners;
        public ItemSurfaceDataHolder RequiredSurface;
        public bool IsSurfaceRestricted;
    }
}
