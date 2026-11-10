using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;
using Coursework_Master_Degree.Game.StateMachine.State.ItemPlace;
using Coursework_Master_Degree.Items.Surface;

namespace Coursework_Master_Degree.Game.Logic.Hover
{
    public class ItemHoverPlace : MonoBehaviour
    {
        // naughty attributes enableif / disableif states
        private bool NotEnabled => false;
        private bool Enabled => true;

        [Header("Hover Settings")]
        [Tooltip(
            "Use player eyes instead of camera, because current setup " +
            "uses Cinemachine, which catch up the player camera in LateUpdate, " +
            "(not in Update, which is default / expectable place to raycast)")]
        public Transform Eyes;
        public float HoverDistance;
        [Tooltip(
            "Layers against which to check for surface, e.g.:\n" +
            "Surface\n" +
            "etc.")]
        public LayerMask SurfaceLayer;

        [Header("Input Translation")]
        [Tooltip("Degrees of rotation to apply on item per input unit")]
        public float RotationPerInput = 15.0f;

        [Header("Corners Check")]
        [Tooltip("Distance vertically up from each corner position to start raycasting against surface from")]
        public float UpOffset = 0.1f;
        [Tooltip("Distance to raycast vertically down against surface")]
        public float DownCheck = 0.2f;

        [Header("Interface Units")]
        public TextMeshProUGUI ItemHoverPlaceText;
        public GameObject PlaceRoot;
        public GameObject CancelRoot;

        private MaterialPropertyBlock propertyBlock;
        private int propertyID;

        // passed
        private ItemPlaceData itemPlaceData;
        public ItemPlaceData ItemPlaceData
        {
            get => itemPlaceData;
            set
            {
                itemPlaceData = value;

                itemGameObject = value.ItemGameObject;
                ghostPosition = value.ItemGameObject.transform.position;
                ghostRotation = value.ItemGameObject.transform.rotation;
                itemName = value.ItemName;

                rightPlacementInfo = "Place " + itemName;
                wrongPlacementInfo = "Wrong placement";

                ghostGameObject = value.GhostGameObject;
                ghostGameObject.transform.position = ghostPosition;
                ghostGameObject.transform.rotation = ghostRotation;

                ghostCorners = value.GhostCorners;
                ghostMeshRenderers = value.GhostMeshRenderers;

                if (GameStateManager.Instance.PreviousGameState is not GameStateItemsMarket)
                {
                    CancelRoot.SetActive(true);
                }
            }
        }

        private GameObject itemGameObject;
        private string itemName;

        private GameObject ghostGameObject;
        private List<Transform> ghostCorners;
        private List<MeshRenderer> ghostMeshRenderers;

        private Influence influence;
        public Influence Influence
        {
            get => influence;
            set => influence = value;
        }

        // local on pass
        private string rightPlacementInfo;
        private string wrongPlacementInfo;

        // collider
        private Collider cachedCollider;

        // local
        private bool isRightPlacement;

        private Vector3 ghostPosition;
        private Quaternion ghostRotation;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            propertyID = Shader.PropertyToID("_placement_state");

            ClearHover();
            CancelRoot.SetActive(false);
        }

        private void Update()
        {
            PerformHoverCheck();
        }

        private bool IsCameraRaycastHit(out Ray ray, out RaycastHit hit)
        {
            ray = new Ray(
                Eyes.position,
                Eyes.forward);

            return Physics.Raycast(
                ray,
                out hit,
                HoverDistance,
                SurfaceLayer,
                QueryTriggerInteraction.Ignore);
        }

        private void PerformHoverCheck()
        {
            if (IsCameraRaycastHit(out var ray, out var hit))
            {
                cachedCollider = hit.collider;

                SetGhostPosition(hit.point);

                bool isRight = IsAllCornersOverSurface();
                TrySetPlacementState(isRight);
            }
            else
            {
                ClearCachedCollider();

                SetGhostPosition(ray.GetPoint(HoverDistance));

                TrySetPlacementState(false);
            }
        }

        private void SetGhostPosition(Vector3 position)
        {
            ghostPosition = position;

            ghostGameObject.transform.position = ghostPosition;
        }

        private bool IsCornerDownRaycastHit(Transform corner, out RaycastHit hit)
        {
            Vector3 rayOrigin = corner.position + (Vector3.up * UpOffset);

            return
                Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out hit,
                    DownCheck,
                    SurfaceLayer,
                    QueryTriggerInteraction.Ignore);
        }

        private bool IsAllCornersOverSurface()
        {
            foreach (Transform corner in ghostCorners)
            {
                if (IsCornerDownRaycastHit(corner, out var hit))
                {
                    // ensure all corners are over same surface
                    if (hit.collider != cachedCollider)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private void TrySetPlacementState(bool isRight)
        {
            if (isRightPlacement == isRight)
            {
                return;
            }

            isRightPlacement = isRight;

            SetupInfoAndPlace();
            SetupGhost();
        }

        private void SetupInfoAndPlace()
        {
            if (isRightPlacement)
            {
                SetupInfo(rightPlacementInfo);
                SetupPlace(true);
            }
            else
            {
                SetupInfo(wrongPlacementInfo);
                SetupPlace(false);
            }
        }

        private void SetupGhost()
        {
            float placementState = isRightPlacement ? 0.0f : 1.0f;

            propertyBlock.SetFloat(propertyID, placementState);

            foreach (var renderer in ghostMeshRenderers)
            {
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void SetupInfo(string info)
        {
            ItemHoverPlaceText.text = info;
            ItemHoverPlaceText.gameObject.SetActive(true);
        }

        private void SetupPlace(bool isActive)
        {
            PlaceRoot.SetActive(isActive);
        }

        private void ClearPassedData()
        {
            itemGameObject = default;
            itemName = default;
            ghostGameObject = default;
            ghostCorners = default;
            influence = default;
        }

        private void ClearCachedCollider()
        {
            cachedCollider = default;
        }

        private void ClearState()
        {
            rightPlacementInfo = default;
            wrongPlacementInfo = default;
            isRightPlacement = default;
            ghostPosition = default;
            ghostRotation = default;
        }

        private void ClearHover()
        {
            SetupInfo("");
            SetupPlace(false);
        }

        private void OnDisable()
        {
            ClearPassedData();
            ClearCachedCollider();
            ClearState();
            ClearHover();
            CancelRoot.SetActive(false);
        }

        // === Player Controller invoked ===

        public void ApplyPlaceItem()
        {
            if (!isRightPlacement)
            {
                return;
            }

            ghostGameObject.SetActive(false);

            itemGameObject.SetActive(true);
            itemGameObject.transform.position = ghostPosition;
            itemGameObject.transform.rotation = ghostRotation;

            var surface = cachedCollider.GetComponent<ItemSurfaceDataHolder>();

            itemPlaceData.ClearParentSurface();
            itemPlaceData.SetParentSurface(surface);

            GameState previousGameState = GameStateManager.Instance.PreviousGameState;
            GameStateManager.Instance.ChangeState(previousGameState);
        }

        public void ApplyRotateItem(float direction)
        {
            Quaternion delta = Quaternion.Euler(0f, direction * RotationPerInput, 0f);
            ghostRotation = delta * ghostRotation;

            ghostGameObject.transform.rotation = ghostRotation;
        }

        public void ApplyCancelItemPlace()
        {
            ghostGameObject.SetActive(false);

            itemGameObject.SetActive(true);

            GameState previousGameState = GameStateManager.Instance.PreviousGameState;
            GameStateManager.Instance.ChangeState(previousGameState);
        }
    }
}
