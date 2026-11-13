using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Part;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.Items.Ghosts.Background;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Game.Helper
{
    public class PartsAssemblySetup : MonoBehaviour
    {
        [Header("Parts Integrity Settings")]
        [Tooltip("Minimal radius of the integrity mode exit trigger around the item")]
        public float ExitTriggerRadius = 3.0f;

        private static readonly int MissingPartStatePropertyID = Shader.PropertyToID("_missing_part_state");

        private ItemGhostBackgroundDataHolder backgroundGhostInstance;
        private PartsIntegrityExitTrigger exitTriggerInstance;

        private readonly List<MeshRenderer> frameMeshRenderers = new();
        private readonly List<bool> frameMeshRenderersStates = new();
        private readonly List<ItemGhostDataHolder> missingPartGhostInstances = new();

        private MaterialPropertyBlock missingPartPropertyBlock;
        private int playerLayer;

        private void Awake()
        {
            playerLayer = LayerMask.NameToLayer("player");
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
            RestoreFrameMeshRenderers();

            if (backgroundGhostInstance != null)
            {
                backgroundGhostInstance.gameObject.SetActive(false);
                backgroundGhostInstance = null;
            }

            foreach (ItemGhostDataHolder missingPartGhost in missingPartGhostInstances)
            {
                if (missingPartGhost == null)
                {
                    continue;
                }

                Destroy(missingPartGhost.gameObject);
            }
            missingPartGhostInstances.Clear();

            if (exitTriggerInstance != null)
            {
                exitTriggerInstance.OnTriggerExited -= HandleExitTriggerExited;

                Destroy(exitTriggerInstance.gameObject);
                exitTriggerInstance = null;
            }
        }

        private void SetupBackgroundGhost(ItemDataHolder item)
        {
            ItemSyncToGhostBackgroundDataHolder itemSync = item.GetComponent<ItemSyncToGhostBackgroundDataHolder>();
            if (itemSync == null)
            {
                return;
            }

            item.TryGetGhostInstance(ItemGhostType.Background, out ItemGhostDataHolder itemGhost);
            if (itemGhost == null)
            {
                if (!IsGhostTypeAvailable(item.ItemGhostMetaSO, ItemGhostType.Background))
                {
                    return;
                }

                itemGhost = GameStateManager.Instance.ItemInstancer.InstantiateItemGhost(
                    item,
                    ItemGhostType.Background,
                    item.transform.position,
                    item.transform.rotation,
                    item.transform);
            }

            if (itemGhost is not ItemGhostBackgroundDataHolder backgroundGhost)
            {
                return;
            }

            backgroundGhostInstance = backgroundGhost;

            SyncBackgroundGhostParts(itemSync, backgroundGhost);
            HideFrameMeshRenderers(itemSync);

            backgroundGhost.gameObject.SetActive(true);
        }

        private void SyncBackgroundGhostParts(ItemSyncToGhostBackgroundDataHolder itemSync, ItemGhostBackgroundDataHolder backgroundGhost)
        {
            if (!itemSync.IsHavePartsToSync)
            {
                return;
            }

            if (itemSync.PartsToTransformsToSyncDictionary == null ||
                backgroundGhost.PartsToTransformsToSyncDictionary == null)
            {
                return;
            }

            foreach (var itemPart in itemSync.PartsToTransformsToSyncDictionary)
            {
                if (itemPart.Value == null)
                {
                    continue;
                }

                if (!backgroundGhost.PartsToTransformsToSyncDictionary.TryGetValue(itemPart.Key, out Transform ghostPart))
                {
                    continue;
                }

                if (ghostPart == null)
                {
                    continue;
                }

                ghostPart.position = itemPart.Value.position;
                ghostPart.rotation = itemPart.Value.rotation;
            }
        }

        private void HideFrameMeshRenderers(ItemSyncToGhostBackgroundDataHolder itemSync)
        {
            if (itemSync.MeshRenderersToSyncList == null)
            {
                return;
            }

            foreach (MeshRenderer meshRenderer in itemSync.MeshRenderersToSyncList)
            {
                if (meshRenderer == null)
                {
                    continue;
                }

                frameMeshRenderers.Add(meshRenderer);
                frameMeshRenderersStates.Add(meshRenderer.enabled);

                meshRenderer.enabled = false;
            }
        }

        private void RestoreFrameMeshRenderers()
        {
            for (int index = 0; index < frameMeshRenderers.Count; index++)
            {
                MeshRenderer meshRenderer = frameMeshRenderers[index];
                if (meshRenderer == null)
                {
                    continue;
                }

                meshRenderer.enabled = frameMeshRenderersStates[index];
            }

            frameMeshRenderers.Clear();
            frameMeshRenderersStates.Clear();
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

        private static bool IsGhostTypeAvailable(ItemGhostMetaSO itemGhostMeta, ItemGhostType type)
        {
            if (itemGhostMeta == null || itemGhostMeta.ItemGhostTypesToPrefabsList == null)
            {
                return false;
            }

            foreach (var itemGhostTypeToPrefab in itemGhostMeta.ItemGhostTypesToPrefabsList)
            {
                if (itemGhostTypeToPrefab.ItemGhostType == type &&
                    itemGhostTypeToPrefab.ItemGhostGameObject != null)
                {
                    return true;
                }
            }

            return false;
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

        private void SetupExitTrigger(ItemDataHolder item)
        {
            Bounds itemBounds = CalculateItemBounds(item);

            float triggerRadius = Mathf.Max(ExitTriggerRadius, itemBounds.extents.magnitude);

            GameObject triggerGameObject = new("parts_integrity_exit_trigger");
            triggerGameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            triggerGameObject.transform.position = itemBounds.center;

            SphereCollider triggerCollider = triggerGameObject.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = triggerRadius;

            exitTriggerInstance = triggerGameObject.AddComponent<PartsIntegrityExitTrigger>();
            exitTriggerInstance.OnTriggerExited += HandleExitTriggerExited;
        }

        private Bounds CalculateItemBounds(ItemDataHolder item)
        {
            Bounds itemBounds = new(item.transform.position, Vector3.zero);

            Collider[] itemColliders = item.GetComponentsInChildren<Collider>();

            foreach (Collider itemCollider in itemColliders)
            {
                if (itemCollider == null || itemCollider.isTrigger)
                {
                    continue;
                }

                itemBounds.Encapsulate(itemCollider.bounds);
            }

            return itemBounds;
        }

        private void HandleExitTriggerExited(Collider other)
        {
            if (other.gameObject.layer != playerLayer)
            {
                return;
            }

            if (GameStateManager.Instance.CurrentGameState is not GameStatePartsIntegrity)
            {
                return;
            }

            GameStateManager.Instance.ChangeState(new GameStateFreeWalk());
        }
    }
}
