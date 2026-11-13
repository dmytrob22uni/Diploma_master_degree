using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Game.Helper
{
    /// <summary>
    /// Prepares an item with <see cref="PartsAssembly"/> for the parts wear game state.
    /// Shows the transparent frame and replaces installed parts with their wear ghosts.
    /// </summary>
    public class PartsWearStateSetup : PartsStateSetup
    {
        private const float DefaultWearLevelPercent = 100.0f;

        private static readonly int WearLevelPercentPropertyID = Shader.PropertyToID("_wear_level_percent");

        private readonly List<ItemGhostDataHolder> wearPartGhostInstances = new();
        private readonly List<MeshRenderer> partMeshRenderers = new();
        private readonly List<bool> partMeshRenderersStates = new();

        private MaterialPropertyBlock wearLevelPropertyBlock;

        protected override string PartsExitTriggerName => "parts_wear_exit_trigger";

        protected override bool IsPartsStateActive()
        {
            return GameStateManager.Instance.CurrentGameState is GameStatePartsWear;
        }

        public void SetupPartsWear(PartsAssembly partsAssembly, ItemDataHolder item)
        {
            ClearPartsWear();

            if (partsAssembly == null || item == null)
            {
                return;
            }

            SetupBackgroundGhost(item);
            SetupExitTrigger(item);
            SetupWearPartGhosts(partsAssembly, item);
        }

        public void ClearPartsWear()
        {
            // restore parts before the frame renderers, because a part renderer
            // may also be shared with the background frame renderers list
            RestorePartMeshRenderers();

            ClearBackgroundGhost();
            ClearExitTrigger();

            foreach (ItemGhostDataHolder wearPartGhost in wearPartGhostInstances)
            {
                if (wearPartGhost == null)
                {
                    continue;
                }

                wearPartGhost.gameObject.SetActive(false);
            }
            wearPartGhostInstances.Clear();
        }

        private void SetupWearPartGhosts(PartsAssembly partsAssembly, ItemDataHolder item)
        {
            SetupWearPartGhosts(partsAssembly.GetOccupiedRegularPartLocations(), item);
            SetupWearPartGhosts(partsAssembly.GetOccupiedOptionalPartLocations(), item);
        }

        private void SetupWearPartGhosts(List<PartLocation> partLocations, ItemDataHolder item)
        {
            if (partLocations == null)
            {
                return;
            }

            foreach (PartLocation partLocation in partLocations)
            {
                SetupWearPartGhost(partLocation, item);
            }
        }

        private void SetupWearPartGhost(PartLocation partLocation, ItemDataHolder item)
        {
            if (partLocation.InstalledPart == null)
            {
                return;
            }

            ItemDataHolder part = partLocation.InstalledPart.GetComponent<ItemDataHolder>();
            if (part == null)
            {
                return;
            }

            if (!IsGhostTypeAvailable(part.ItemGhostMetaSO, ItemGhostType.WearLevel))
            {
                return;
            }

            Transform partTransform = part.transform;

            ItemGhostDataHolder wearPartGhost = GetWearPartGhost(
                part,
                partTransform.position,
                partTransform.rotation,
                item.transform);

            if (wearPartGhost == null)
            {
                return;
            }

            SetupWearLevel(wearPartGhost, part);
            HidePartMeshRenderers(part);

            wearPartGhostInstances.Add(wearPartGhost);
        }

        private ItemGhostDataHolder GetWearPartGhost(ItemDataHolder part, Vector3 position, Quaternion rotation, Transform parent)
        {
            // Wear ghosts are stored on the part item data holder and reused,
            // but their positions are synced to the current part placement.
            if (part.TryGetGhostInstance(ItemGhostType.WearLevel, out ItemGhostDataHolder wearPartGhost) &&
                wearPartGhost != null)
            {
                wearPartGhost.transform.SetParent(parent, true);
                wearPartGhost.transform.position = position;
                wearPartGhost.transform.rotation = rotation;

                wearPartGhost.gameObject.SetActive(true);

                return wearPartGhost;
            }

            return GameStateManager.Instance.ItemInstancer.InstantiateItemGhost(
                part,
                ItemGhostType.WearLevel,
                position,
                rotation,
                parent);
        }

        private void SetupWearLevel(ItemGhostDataHolder wearPartGhost, ItemDataHolder part)
        {
            float wearLevelPercent = DefaultWearLevelPercent;

            ItemWearState partWearState = part.GetComponent<ItemWearState>();
            if (partWearState != null)
            {
                wearLevelPercent = partWearState.CurrentCondition;
            }

            if (wearPartGhost.MeshRenderersList == null)
            {
                return;
            }

            wearLevelPropertyBlock ??= new MaterialPropertyBlock();
            wearLevelPropertyBlock.SetFloat(WearLevelPercentPropertyID, wearLevelPercent);

            foreach (MeshRenderer meshRenderer in wearPartGhost.MeshRenderersList)
            {
                if (meshRenderer == null)
                {
                    continue;
                }

                meshRenderer.SetPropertyBlock(wearLevelPropertyBlock);
            }
        }

        private void HidePartMeshRenderers(ItemDataHolder part)
        {
            foreach (MeshRenderer meshRenderer in part.GetComponentsInChildren<MeshRenderer>())
            {
                if (meshRenderer == null)
                {
                    continue;
                }

                partMeshRenderers.Add(meshRenderer);
                partMeshRenderersStates.Add(meshRenderer.enabled);

                meshRenderer.enabled = false;
            }
        }

        private void RestorePartMeshRenderers()
        {
            for (int index = 0; index < partMeshRenderers.Count; index++)
            {
                MeshRenderer meshRenderer = partMeshRenderers[index];
                if (meshRenderer == null)
                {
                    continue;
                }

                meshRenderer.enabled = partMeshRenderersStates[index];
            }

            partMeshRenderers.Clear();
            partMeshRenderersStates.Clear();
        }
    }
}
