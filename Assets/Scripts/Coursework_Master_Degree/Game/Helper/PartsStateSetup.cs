using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.Items.Ghosts.Background;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;
using Coursework_Master_Degree.Game.StateMachine;
using Coursework_Master_Degree.Game.StateMachine.State;

namespace Coursework_Master_Degree.Game.Helper
{
    /// <summary>
    /// Shared setup for game states which inspect a <see cref="PartsAssembly"/>.
    /// Prepares the transparent frame, the exit trigger and helpers needed to
    /// restore the item when the player leaves the inspected area.
    /// </summary>
    public abstract class PartsStateSetup : MonoBehaviour
    {
        [Header("Parts State Settings")]
        [Tooltip("Minimal radius of the parts mode exit trigger around the item")]
        public float ExitTriggerRadius = 3.0f;

        private ItemGhostBackgroundDataHolder backgroundGhostInstance;
        private PartsExitTrigger exitTriggerInstance;

        private readonly List<MeshRenderer> frameMeshRenderers = new();
        private readonly List<bool> frameMeshRenderersStates = new();

        private int playerLayer;

        protected abstract string PartsExitTriggerName { get; }

        protected virtual void Awake()
        {
            playerLayer = LayerMask.NameToLayer("player");
        }

        protected abstract bool IsPartsStateActive();

        protected void SetupBackgroundGhost(ItemDataHolder item)
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

        protected void ClearBackgroundGhost()
        {
            RestoreFrameMeshRenderers();

            if (backgroundGhostInstance != null)
            {
                backgroundGhostInstance.gameObject.SetActive(false);
                backgroundGhostInstance = null;
            }
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

        protected static bool IsGhostTypeAvailable(ItemGhostMetaSO itemGhostMeta, ItemGhostType type)
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

        protected void SetupExitTrigger(ItemDataHolder item)
        {
            Bounds itemBounds = CalculateItemBounds(item);

            float triggerRadius = Mathf.Max(ExitTriggerRadius, itemBounds.extents.magnitude);

            GameObject triggerGameObject = new(PartsExitTriggerName);
            triggerGameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            triggerGameObject.transform.position = itemBounds.center;

            SphereCollider triggerCollider = triggerGameObject.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = triggerRadius;

            exitTriggerInstance = triggerGameObject.AddComponent<PartsExitTrigger>();
            exitTriggerInstance.OnTriggerExited += HandleExitTriggerExited;
        }

        protected void ClearExitTrigger()
        {
            if (exitTriggerInstance == null)
            {
                return;
            }

            exitTriggerInstance.OnTriggerExited -= HandleExitTriggerExited;

            Destroy(exitTriggerInstance.gameObject);
            exitTriggerInstance = null;
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

            if (!IsPartsStateActive())
            {
                return;
            }

            GameStateManager.Instance.ChangeState(new GameStateFreeWalk());
        }
    }
}
