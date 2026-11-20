using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.FFF;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.Items.Wear;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Game.Helper
{
    /// <summary>
    /// Prepares an item for the parts wear game state.
    /// Shows the transparent frame and replaces installed parts with their wear ghosts.
    /// A standalone wearable item uses its own wear ghost instead of an assembly.
    /// </summary>
    public class PartsWearStateSetup : PartsStateSetup
    {
        private const float DefaultWearLevelPercent = 100.0f;

        private static readonly int WearLevelPercentPropertyID = Shader.PropertyToID("_wear_level_percent");

        private readonly List<ItemGhostDataHolder> wearPartGhostInstances = new();
        private readonly HashSet<PartsAssembly> inspectedAssemblies = new();
        private readonly List<MeshRenderer> partMeshRenderers = new();
        private readonly List<bool> partMeshRenderersStates = new();
        private readonly HashSet<MeshRenderer> hiddenPartMeshRenderers = new();

        private MaterialPropertyBlock wearLevelPropertyBlock;

        protected override string PartsExitTriggerName => "parts_wear_exit_trigger";

        protected override bool IsPartsStateActive()
        {
            return GameStateManager.Instance.CurrentGameState is GameStatePartsWear;
        }

        public void SetupPartsWear(PartsAssembly partsAssembly, ItemDataHolder item)
        {
            ClearPartsWear();

            if (item == null)
            {
                return;
            }

            SetupBackgroundGhost(item);
            SetupExitTrigger(item);

            if (partsAssembly != null)
            {
                SetupWearPartGhosts(partsAssembly);

                return;
            }

            // A standalone wearable item has no assembly, but it still exposes
            // its own wear ghost through the same item ghost type.
            SetupWearPartGhost(item, item.transform);
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
            inspectedAssemblies.Clear();
        }

        private void SetupWearPartGhosts(PartsAssembly partsAssembly)
        {
            if (partsAssembly == null ||
                !inspectedAssemblies.Add(partsAssembly))
            {
                return;
            }

            SetupWearPartGhosts(partsAssembly.GetOccupiedRegularPartLocations());
            SetupWearPartGhosts(partsAssembly.GetOccupiedOptionalPartLocations());

            SetupNestedPrinterAssemblies(partsAssembly);
        }

        private void SetupWearPartGhosts(List<PartLocation> partLocations)
        {
            if (partLocations == null)
            {
                return;
            }

            foreach (PartLocation partLocation in partLocations)
            {
                SetupWearPartGhost(partLocation);
            }
        }

        private void SetupNestedPrinterAssemblies(PartsAssembly partsAssembly)
        {
            SetupNestedPrinterAssemblies(
                partsAssembly.GetOccupiedRegularPartLocations());
            SetupNestedPrinterAssemblies(
                partsAssembly.GetOccupiedOptionalPartLocations());
        }

        private void SetupNestedPrinterAssemblies(
            List<PartLocation> partLocations)
        {
            if (partLocations == null)
            {
                return;
            }

            foreach (PartLocation partLocation in partLocations)
            {
                if (partLocation == null || partLocation.InstalledPart == null)
                {
                    continue;
                }

                Printer printer =
                    partLocation.InstalledPart.GetComponent<Printer>();

                if (printer != null && printer.Assembly != null)
                {
                    ItemDataHolder printerItem =
                        printer.GetComponent<ItemDataHolder>();

                    if (printerItem != null)
                    {
                        // The printer is represented by its internal wear
                        // ghosts while inspecting the enclosure. Hide the
                        // printer root so its frame does not obscure them.
                        HidePartMeshRenderers(printerItem);
                    }

                    SetupWearPartGhosts(printer.Assembly);
                }
            }
        }

        private void SetupWearPartGhost(PartLocation partLocation)
        {
            if (partLocation.InstalledPart == null)
            {
                return;
            }

            ItemDataHolder part =
                partLocation.InstalledPart.GetComponent<ItemDataHolder>();

            if (part == null || part.GetComponent<Printer>() != null)
            {
                return;
            }

            SetupWearPartGhost(part, partLocation.transform);
        }

        private void SetupWearPartGhost(ItemDataHolder part, Transform anchor)
        {
            if (!IsGhostTypeAvailable(part.ItemGhostMetaSO, ItemGhostType.WearLevel))
            {
                return;
            }

            ItemGhostDataHolder wearPartGhost = GetWearPartGhost(part, anchor);

            if (wearPartGhost == null)
            {
                return;
            }

            SetupWearLevel(wearPartGhost, part);
            HidePartMeshRenderers(part);

            wearPartGhostInstances.Add(wearPartGhost);
        }

        private ItemGhostDataHolder GetWearPartGhost(ItemDataHolder part, Transform anchor)
        {
            // Wear ghosts are stored on the part item data holder and reused,
            // but their transforms are synced to the current part placement.
            if (part.TryGetGhostInstance(ItemGhostType.WearLevel, out ItemGhostDataHolder wearPartGhost) &&
                wearPartGhost != null)
            {
                GameStateManager.Instance.ItemInstancer.SnapItemGhostToAnchor(
                    wearPartGhost,
                    anchor);

                wearPartGhost.gameObject.SetActive(true);

                return wearPartGhost;
            }

            ItemGhostDataHolder ghost = GameStateManager.Instance.ItemInstancer.InstantiateItemGhost(
                part,
                ItemGhostType.WearLevel,
                anchor.position,
                anchor.rotation,
                anchor);

            GameStateManager.Instance.ItemInstancer.SnapItemGhostToAnchor(
                ghost,
                anchor);

            return ghost;
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

                // The item may be its own inspection target; never hide the
                // ghost which is currently replacing its renderers.
                if (meshRenderer.GetComponentInParent<ItemGhostDataHolder>() != null ||
                    !hiddenPartMeshRenderers.Add(meshRenderer))
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
            hiddenPartMeshRenderers.Clear();
        }
    }
}
