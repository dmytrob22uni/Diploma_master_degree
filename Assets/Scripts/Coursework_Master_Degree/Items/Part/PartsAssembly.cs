using UnityEngine;
using System.Collections.Generic;
using Coursework_Master_Degree.ScriptableObjects.Items.Ghosts.Types;
using Coursework_Master_Degree.Items.Ghosts.Background;
using Coursework_Master_Degree.Items.Ghosts;
using UnityEditor.TerrainTools;

namespace Coursework_Master_Degree.Items.Part
{
    public class PartsAssembly : MonoBehaviour
    {
        public List<PartLocation> PartsLocationsList;

        public Material GhostBackgroundMaterial;
        public Material GhostWearLevelMaterial;
        public Material GhostMissingPartMaterial;

        private void Awake()
        {
            // spawn parts ghosts wear level
            // int partIndex = -1;
            // foreach (var partLocation in PartsLocationsList)
            // {
            //     partIndex++;
            //     if (!partLocation.IsOccupied)
            //     {
            //         continue;
            //     }

            //     bool IsPartAttached = partLocation.InstalledPart.TryGetComponent<ItemDataHolder>(out ItemDataHolder partDataHolder);
            //     if (!IsPartAttached)
            //     {
            //         continue;
            //     }

            //     foreach (var ghostTypeToPrefab in partDataHolder.ItemGhostMetaSO.ItemGhostTypesToPrefabsList)
            //     {
            //         if (ghostTypeToPrefab.ItemGhostType == ItemGhostType.WearLevel)
            //         {
            //             GameObject partGhostWearLevel = Instantiate(
            //                 ghostTypeToPrefab.ItemGhostGameObject,
            //                 partLocation.gameObject.transform.parent);
            //             partGhostWearLevel.transform.localPosition = partLocation.gameObject.transform.localPosition;
            //             partGhostWearLevel.transform.localRotation = partLocation.gameObject.transform.localRotation;

            //             // part ghost wear level
            //             int wearLevelProperty = Shader.PropertyToID("_wear_level_percent");
            //             // int randomWearLevelPercent = Random.Range(20, 96);
            //             float randomWearLevelPercent = 0;
            //             switch (partIndex)
            //             {
            //                 // Spool:
            //                 case 31:
            //                 case 32:
            //                     randomWearLevelPercent = 50;
            //                     break;
            //                 // AxisBelt,
            //                 case 4:
            //                 case 20:
            //                     randomWearLevelPercent = 85;
            //                     break;
            //                 // AxisBeltBearing,
            //                 case 1:
            //                 case 3:
            //                 case 15:
            //                 case 19:
            //                     randomWearLevelPercent = 75;
            //                     break;
            //                 // AxisGuideBearing,
            //                 case 16:
            //                 case 17:
            //                     randomWearLevelPercent = 78;
            //                     break;
            //                 // AxisRotor,
            //                 case 2:
            //                 case 5:
            //                 case 6:
            //                 case 18:
            //                 case 23:
            //                 case 24:
            //                 case 25:
            //                     randomWearLevelPercent = 90;
            //                     break;
            //                 // AxisRotorBearing,
            //                 case 11:
            //                 case 12:
            //                 case 13:
            //                 case 14:
            //                 case 21:
            //                 case 22:
            //                     randomWearLevelPercent = 75;
            //                     break;
            //                 // BatteryCloset,
            //                 case 7:
            //                     randomWearLevelPercent = 98;
            //                     break;
            //                 // BatteryClosetAdapter,
            //                 case 0:
            //                     randomWearLevelPercent = 98;
            //                     break;
            //                 // ExtruderFan,
            //                 case 26:
            //                     randomWearLevelPercent = 80;
            //                     break;
            //                 // ExtruderFilamentBearing,
            //                 case 27:
            //                     randomWearLevelPercent = 65;
            //                     break;
            //                 // ExtruderFilamentHeater,
            //                 case 28:
            //                     randomWearLevelPercent = 70;
            //                     break;
            //                 // ExtruderNozzle,
            //                 case 29:
            //                     randomWearLevelPercent = 50;
            //                     break;
            //                 // ExtrderThermistor,
            //                 case 30:
            //                     randomWearLevelPercent = 85;
            //                     break;
            //                 // PrintBed,
            //                 case 8:
            //                     randomWearLevelPercent = 88;
            //                     break;
            //                 // PrintBedThermistor,
            //                 case 9:
            //                     randomWearLevelPercent = 85;
            //                     break;
            //                 // PrintPlate,
            //                 case 10:
            //                     randomWearLevelPercent = 60;
            //                     break;
            //                 default:
            //                     randomWearLevelPercent = 0;
            //                     break;
            //             }
            //             randomWearLevelPercent -= (100 - randomWearLevelPercent);

            //             ItemGhostWearLevelDataHolder partGhostWearLevelDataHolder = partGhostWearLevel.GetComponent<ItemGhostWearLevelDataHolder>();
            //             foreach (MeshRenderer renderer in partGhostWearLevelDataHolder.MeshRenderersList)
            //             {
            //                 var materials = renderer.sharedMaterials;

            //                 for (int j = 0; j < materials.Length; j++)
            //                     materials[j] = GhostWearLevelMaterial;

            //                 renderer.sharedMaterials = materials;

            //                 var block = new MaterialPropertyBlock();
            //                 renderer.GetPropertyBlock(block);
            //                 block.SetFloat(wearLevelProperty, randomWearLevelPercent);
            //                 renderer.SetPropertyBlock(block);
            //             }
            //         }
            //     }

            //     partLocation.InstalledPart.SetActive(false);
            // }

            Debug.Log("yes parts ghosts wear level");

            // foreach (var partLocation in PartsLocationsList)
            // {
            //     if (!partLocation.IsOccupied)
            //     {
            //         continue;
            //     }

            //     bool IsPartAttached = partLocation.InstalledPart.TryGetComponent<ItemDataHolder>(out ItemDataHolder partDataHolder);
            //     if (!IsPartAttached)
            //     {
            //         continue;
            //     }

            //     foreach (var ghostTypeToPrefab in partDataHolder.ItemGhostMetaSO.ItemGhostTypesToPrefabsList)
            //     {
            //         if (ghostTypeToPrefab.ItemGhostType == ItemGhostType.MissingPart)
            //         {
            //             if (Random.Range(0, 10) < 2)
            //             {
            //                 GameObject partGhostMissingPart = Instantiate(
            //                     ghostTypeToPrefab.ItemGhostGameObject,
            //                     partLocation.gameObject.transform.parent);
            //                 partGhostMissingPart.transform.localPosition = partLocation.gameObject.transform.localPosition;
            //                 partGhostMissingPart.transform.localRotation = partLocation.gameObject.transform.localRotation;
            //                 partGhostMissingPart.transform.localScale = partLocation.gameObject.transform.localScale;

            //                 // part ghost missing part
            //                 int missingPartProperty = Shader.PropertyToID("_missing_part_state");

            //                 ItemGhostMissingPartDataHolder partGhostMissingPartDataHolder = partGhostMissingPart.GetComponent<ItemGhostMissingPartDataHolder>();
            //                 foreach (MeshRenderer renderer in partGhostMissingPartDataHolder.MeshRenderersList)
            //                 {
            //                     var materials = renderer.sharedMaterials;

            //                     for (int j = 0; j < materials.Length; j++)
            //                     {
            //                         materials[j] = GhostMissingPartMaterial;
            //                     }

            //                     renderer.sharedMaterials = materials;

            //                     var block = new MaterialPropertyBlock();
            //                     renderer.GetPropertyBlock(block);
            //                     block.SetFloat(missingPartProperty, 1);
            //                     renderer.SetPropertyBlock(block);
            //                 }

            //                 partLocation.InstalledPart.SetActive(false);
            //             }
            //         }
            //     }
            // }

            Debug.Log("yes parts ghosts missing");

            ItemDataHolder printerDataHolder = gameObject.GetComponent<ItemDataHolder>();

            // spawn printer ghost background
            GameObject printerBackground = null;
            foreach (var ghostTypeToPrefab in printerDataHolder.ItemGhostMetaSO.ItemGhostTypesToPrefabsList)
            {
                if (ghostTypeToPrefab.ItemGhostType == ItemGhostType.Background)
                {
                    printerBackground = Instantiate(
                        ghostTypeToPrefab.ItemGhostGameObject,
                        gameObject.transform.parent);
                    printerBackground.transform.localPosition = gameObject.transform.localPosition;
                    printerBackground.transform.localRotation = gameObject.transform.localRotation;
                }
            }

            Debug.Log("yes printer ghost background");

            ItemSyncToGhostBackgroundDataHolder printerSyncToGhostBackgroundDataHolder = GetComponent<ItemSyncToGhostBackgroundDataHolder>();
            ItemGhostBackgroundDataHolder printerGhostBackgroundDataHolder = printerBackground.GetComponent<ItemGhostBackgroundDataHolder>();

            foreach (var keyValue in printerSyncToGhostBackgroundDataHolder.PartsToTransformsToSyncDictionary)
            {
                if (printerGhostBackgroundDataHolder.PartsToTransformsToSyncDictionary[keyValue.Key] == null)
                {
                    Debug.Log(keyValue.Key);
                }

                if (printerSyncToGhostBackgroundDataHolder.PartsToTransformsToSyncDictionary[keyValue.Key] == null)
                {
                    Debug.Log(keyValue.Key);
                }

                printerGhostBackgroundDataHolder.PartsToTransformsToSyncDictionary[keyValue.Key].localPosition =
                    printerSyncToGhostBackgroundDataHolder.PartsToTransformsToSyncDictionary[keyValue.Key].localPosition;
            }

            Debug.Log("yes printer ghost background parts transforms");

            for (int i = 0; i < printerGhostBackgroundDataHolder.MeshRenderersList.Count; i++)
            {
                MeshRenderer renderer = printerGhostBackgroundDataHolder.MeshRenderersList[i];
                Material[] materials = renderer.sharedMaterials;
                for (int j = 0; j < materials.Length; j++)
                {
                    materials[j] = GhostBackgroundMaterial;
                }
                renderer.sharedMaterials = materials;
            }

            Debug.Log("yes printer ghost background mesh material");

            for (int i = 0; i < printerSyncToGhostBackgroundDataHolder.MeshRenderersToSyncList.Count; i++)
            {
                MeshRenderer renderer = printerSyncToGhostBackgroundDataHolder.MeshRenderersToSyncList[i];
                renderer.enabled = false;
            }

            Debug.Log("yes printer mesh disable");
        }
    }
}
