using System;
using UnityEngine;
using Coursework_Master_Degree.Items;
using Coursework_Master_Degree.Items.Ghosts;
using Coursework_Master_Degree.ObjectsToPrint;
using Coursework_Master_Degree.ObjectsToPrint.Ghost;
using Coursework_Master_Degree.ScriptableObjects.Items;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint.Ghost.Types;

namespace Coursework_Master_Degree.Game.Helper
{
    public class ItemInstancer : MonoBehaviour
    {
        [Header("Item Ghost Materials")]
        public Material TumblerDiodeItemGhostMaterial;
        public Material BackgroundItemGhostMaterial;
        public Material PlacementItemGhostMaterial;
        public Material SellingItemGhostMaterial;

        [Header("Part Ghost Materials")]
        public Material InstallationPartGhostMaterial;
        public Material MissingPartGhostMaterial;
        public Material WearLevelPartGhostMaterial;

        [Header("ObjectToPrint Ghost Materials")]
        public Material PlacementObjectToPrintGhostMaterial;
        public Material PrinterBedPlacementObjectToPrintGhostMaterial;
        public Material QualityObjectToPrintGhostMaterial;

        // Item

        public GameObject InstantiateItem(ItemMetaSO itemMeta, Vector3 position = default, Quaternion rotation = default, Transform parent = default)
        {
            return GameObject.Instantiate(
                itemMeta.Prefab,
                position,
                rotation,
                parent);
        }

        public ItemGhostDataHolder InstantiateItemGhost(ItemDataHolder item, ItemGhostType type, Vector3 position = default, Quaternion rotation = default, Transform parent = default)
        {
            GameObject itemGhostPrefab = default;
            foreach (var itemGhostTypeToPrefab in item.ItemGhostMetaSO.ItemGhostTypesToPrefabsList)
            {
                if (itemGhostTypeToPrefab.ItemGhostType == type)
                {
                    itemGhostPrefab = itemGhostTypeToPrefab.ItemGhostGameObject;
                    break;
                }
            }

            if (itemGhostPrefab == null)
            {
                throw new NullReferenceException(
                    $"Item \'{item.ItemMetaData.Name}\' doesn't have ghost form of type \'${type}\'");
            }

            GameObject itemGhostGameObject = GameObject.Instantiate(
                itemGhostPrefab,
                position,
                rotation,
                parent);

            var itemGhost = itemGhostGameObject.GetComponent<ItemGhostDataHolder>();
            if (itemGhost.IsNotPreconfiguredMaterials())
            {
                SetupItemGhostMaterial(in itemGhost);
            }

            // setup mutual referencing
            itemGhost.ItemInstance = item.gameObject;
            item.TryAddGhostType(type, itemGhost);

            return itemGhost;
        }

        private void SetupItemGhostMaterial(in ItemGhostDataHolder itemGhost)
        {
            Material ghostMaterial = PickItemGhostMaterial(itemGhost.ItemGhostType);

            foreach (MeshRenderer renderer in itemGhost.MeshRenderersList)
            {
                Material[] materials = renderer.sharedMaterials;

                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = ghostMaterial;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private Material PickItemGhostMaterial(ItemGhostType itemGhostType)
        {
            return itemGhostType switch
            {
                ItemGhostType.Background => BackgroundItemGhostMaterial,
                ItemGhostType.Installation => InstallationPartGhostMaterial,
                ItemGhostType.MissingPart => MissingPartGhostMaterial,
                ItemGhostType.Placement => PlacementItemGhostMaterial,
                ItemGhostType.Selling => SellingItemGhostMaterial,
                ItemGhostType.WearLevel => WearLevelPartGhostMaterial,
                _ => throw new NullReferenceException($"Ghost type \'{itemGhostType}\' was not expected"),
            };
        }

        // ItemFreePart

        public ItemFreePartGhostPlacementDataHolder InstantiateItemFreePartGhost(ItemFreePartDataHolder itemFreePart, Vector3 position = default, Quaternion rotation = default, Transform parent = default)
        {
            GameObject itemFreePartGhostGameObject = GameObject.Instantiate(
                itemFreePart.ItemFreePartGhostPlacementPrefab,
                position,
                rotation,
                parent);

            var itemFreePartGhost = itemFreePartGhostGameObject.GetComponent<ItemFreePartGhostPlacementDataHolder>();
            SetupItemFreePartGhostMaterial(in itemFreePartGhost);

            // setup mutual referencing
            itemFreePartGhost.ItemFreePartInstance = itemFreePart.gameObject;
            itemFreePart.TryAddGhost(itemFreePartGhost);

            return itemFreePartGhost;
        }

        private void SetupItemFreePartGhostMaterial(in ItemFreePartGhostPlacementDataHolder itemFreePartGhost)
        {
            Material ghostMaterial = PickItemFreePartGhostMaterial();

            foreach (MeshRenderer renderer in itemFreePartGhost.MeshRenderersList)
            {
                Material[] materials = renderer.sharedMaterials;

                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = ghostMaterial;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private Material PickItemFreePartGhostMaterial()
        {
            return PlacementItemGhostMaterial;
        }

        // ObjectToPrint

        public GameObject InstantiateObjectToPrint(ObjectToPrintMetaSO objectToPrint, Vector3 position = default, Quaternion rotation = default, Transform parent = default)
        {
            return GameObject.Instantiate(
                objectToPrint.Prefab,
                position,
                rotation,
                parent);
        }

        public ObjectToPrintGhostDataHolder InstantiateObjectToPrintGhost(ObjectToPrintDataHolder objectToPrint, ObjectToPrintGhostType type, Vector3 position = default, Quaternion rotation = default, Transform parent = default)
        {
            GameObject objectToPrintGhostPrefab = default;
            foreach (var objectToPrintGhostTypeToPrefab in objectToPrint.ObjectToPrintGhostMetaSO.ObjectToPrintTypesToPrefabsList)
            {
                if (objectToPrintGhostTypeToPrefab.ObjectToPrintGhostType == type)
                {
                    objectToPrintGhostPrefab = objectToPrintGhostTypeToPrefab.ObjectToPrintGhostGameObject;
                    break;
                }
            }

            if (objectToPrintGhostPrefab == null)
            {
                throw new NullReferenceException(
                    $"ObjectToPrint \'{objectToPrint.ObjectToPrintMetaData.Name}\' doesn't have ghost form of type \'${type}\'");
            }

            GameObject objectToPrintGhostGameObject = GameObject.Instantiate(
                objectToPrintGhostPrefab,
                position,
                rotation,
                parent);

            var objectToPrintGhost = objectToPrintGhostGameObject.GetComponent<ObjectToPrintGhostDataHolder>();
            SetupObjectToPrintGhostMaterial(in objectToPrintGhost, objectToPrint);

            // setup mutual referencing
            objectToPrintGhost.ObjectToPrintInstance = objectToPrint.gameObject;
            objectToPrint.TryAddGhostType(type, objectToPrintGhost);

            return objectToPrintGhost;
        }

        private void SetupObjectToPrintGhostMaterial(in ObjectToPrintGhostDataHolder objectToPrintGhost, ObjectToPrintDataHolder objectToPrint)
        {
            Material ghostMaterial = PickObjectToPrintGhostMaterial(
                objectToPrintGhost.ObjectToPrintGhostType,
                objectToPrint);

            foreach (MeshRenderer renderer in objectToPrintGhost.MeshRenderersList)
            {
                Material[] materials = renderer.sharedMaterials;

                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = ghostMaterial;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private Material PickObjectToPrintGhostMaterial(ObjectToPrintGhostType objectToPrintGhostType, ObjectToPrintDataHolder objectToPrint)
        {
            return objectToPrintGhostType switch
            {
                ObjectToPrintGhostType.Placement => PlacementObjectToPrintGhostMaterial,
                ObjectToPrintGhostType.PrinterBedPlacement => PrinterBedPlacementObjectToPrintGhostMaterial,
                ObjectToPrintGhostType.Printing => objectToPrint.PrintingMaterial,
                ObjectToPrintGhostType.Quality => QualityObjectToPrintGhostMaterial,
                ObjectToPrintGhostType.Selling => SellingItemGhostMaterial,
                _ => throw new NullReferenceException($"Ghost type \'{objectToPrintGhostType}\' was not expected"),
            };
        }
    }
}
