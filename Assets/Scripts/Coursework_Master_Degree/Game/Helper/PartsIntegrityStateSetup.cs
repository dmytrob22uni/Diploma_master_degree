using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Game.Helper
{
    public class PartsIntegrityStateSetup : PartsStateSetup
    {
        private static readonly int MissingPartStatePropertyID = Shader.PropertyToID("_missing_part_state");

        private readonly List<ItemGhostDataHolder> missingPartGhostInstances = new();

        private MaterialPropertyBlock missingPartPropertyBlock;

        protected override string PartsExitTriggerName => "parts_integrity_exit_trigger";

        protected override bool IsPartsStateActive()
        {
            return GameStateManager.Instance.CurrentGameState is GameStatePartsIntegrity;
        }

        public void SetupPartsIntegrity(PartsAssembly partsAssembly, ItemDataHolder item)
        {
            ClearPartsIntegrity();

            if (partsAssembly == null || item == null)
            {
                return;
            }

            SetupBackgroundGhost(item);
            SetupExitTrigger(item);
            SetupMissingPartGhosts(partsAssembly, item);
        }

        public void ClearPartsIntegrity()
        {
            ClearBackgroundGhost();
            ClearExitTrigger();

            foreach (ItemGhostDataHolder missingPartGhost in missingPartGhostInstances)
            {
                if (missingPartGhost == null)
                {
                    continue;
                }

                Destroy(missingPartGhost.gameObject);
            }
            missingPartGhostInstances.Clear();
        }

        private void SetupMissingPartGhosts(PartsAssembly partsAssembly, ItemDataHolder item)
        {
            List<PartLocation> freeRegularLocations = partsAssembly.GetFreeRegularPartLocations();
            if (freeRegularLocations != null)
            {
                foreach (PartLocation partLocation in freeRegularLocations)
                {
                    InstantiateMissingPartGhost(partLocation, item);
                }
            }

            if (partsAssembly.OptionalPartLocationsList == null)
            {
                return;
            }

            foreach (OptionalPart optionalPart in partsAssembly.OptionalPartLocationsList)
            {
                int occupiedPartLocationsNumber = 0;

                foreach (PartLocation partLocation in optionalPart.PartLocationsList)
                {
                    if (partLocation.IsOccupied)
                    {
                        occupiedPartLocationsNumber++;
                    }
                }

                // do not highlight free locations of an already satisfied optional part group
                if (occupiedPartLocationsNumber >= optionalPart.EssentialPartsNumber)
                {
                    continue;
                }

                foreach (PartLocation partLocation in optionalPart.PartLocationsList)
                {
                    if (!partLocation.IsOccupied)
                    {
                        InstantiateMissingPartGhost(partLocation, item);
                    }
                }
            }
        }

        private void InstantiateMissingPartGhost(PartLocation partLocation, ItemDataHolder item)
        {
            ItemMetaSO partMetaData = GetExpectedPartMetaData(partLocation);
            if (partMetaData == null || partMetaData.Prefab == null)
            {
                return;
            }

            ItemDataHolder partPrefabDataHolder = partMetaData.Prefab.GetComponent<ItemDataHolder>();
            if (partPrefabDataHolder == null || partPrefabDataHolder.ItemGhostMetaSO == null)
            {
                return;
            }

            if (!IsGhostTypeAvailable(partPrefabDataHolder.ItemGhostMetaSO, ItemGhostType.MissingPart))
            {
                return;
            }

            Transform partLocationTransform = partLocation.transform;

            ItemGhostDataHolder missingPartGhost = GameStateManager.Instance.ItemInstancer.InstantiateItemGhost(
                partPrefabDataHolder.ItemGhostMetaSO,
                ItemGhostType.MissingPart,
                partLocationTransform.position,
                partLocationTransform.rotation,
                item.transform);

            if (missingPartGhost is not ItemGhostMissingPartDataHolder missingPart)
            {
                Destroy(missingPartGhost.gameObject);
                return;
            }

            missingPart.PartMetaData = partMetaData;

            SetupMissingPartState(missingPart);

            missingPartGhostInstances.Add(missingPart);
        }

        private ItemMetaSO GetExpectedPartMetaData(PartLocation partLocation)
        {
            if (partLocation.IsSingleMetaDataReference)
            {
                return partLocation.PartMetaData;
            }

            if (partLocation.PartMetaDatasList == null || partLocation.PartMetaDatasList.Count == 0)
            {
                return null;
            }

            return partLocation.PartMetaDatasList[0];
        }

        private void SetupMissingPartState(ItemGhostDataHolder missingPartGhost)
        {
            if (missingPartGhost.MeshRenderersList == null)
            {
                return;
            }

            missingPartPropertyBlock ??= new MaterialPropertyBlock();
            missingPartPropertyBlock.SetFloat(MissingPartStatePropertyID, 1.0f);

            foreach (MeshRenderer meshRenderer in missingPartGhost.MeshRenderersList)
            {
                if (meshRenderer == null)
                {
                    continue;
                }

                meshRenderer.SetPropertyBlock(missingPartPropertyBlock);
            }
        }
    }
}
