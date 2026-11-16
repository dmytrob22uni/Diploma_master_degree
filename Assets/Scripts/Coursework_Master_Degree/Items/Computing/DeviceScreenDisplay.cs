using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Coursework_Master_Degree.Items.Computing
{
    /// <summary>
    /// Swaps the physical display between its authored material and a black
    /// "screen off" material. The device keeps running while the screen is off.
    /// </summary>
    public class DeviceScreenDisplay : MonoBehaviour
    {
        [Header("References")]
        public ItemComputingDataHolder Computing;

        [Tooltip("Renderers that make up the display. Auto-filled from ScreenGameObject.")]
        public List<Renderer> ScreenRenderers = new List<Renderer>();

        [Tooltip("Optional black material. A runtime black material is created when empty.")]
        public Material ScreenOffMaterial;

        [Tooltip(
            "Apply ItemComputingDataHolder.IsTurnedOn in Awake.\n" +
            "Leave off in M0 so screens keep their authored look until tested.")]
        public bool ApplyStateOnAwake = false;

        [SerializeField]
        [Tooltip("Set via script. Exposed for debugging")]
        private bool isOn = true;

        public bool IsOn => isOn;

        private readonly List<Material[]> originalMaterials = new List<Material[]>();
        private Material runtimeOffMaterial;

        private void Awake()
        {
            AutoAssign();

            if (ScreenRenderers.Count == 0)
            {
                CollectRenderers();
            }

            CaptureOriginalMaterials();

            if (ApplyStateOnAwake && Computing != null)
            {
                SetOn(Computing.IsTurnedOn);
            }
        }

        [ContextMenu("Screen Off")]
        public void SetOff()
        {
            SetOn(false);
        }

        [ContextMenu("Screen On")]
        public void SetOn()
        {
            SetOn(true);
        }

        [ContextMenu("Toggle Screen")]
        public void Toggle()
        {
            SetOn(!isOn);
        }

        public void SetOn(bool on)
        {
            isOn = on;

            if (Computing != null)
            {
                Computing.IsTurnedOn = on;
            }

            ApplyVisualState();
        }

        [ContextMenu("Re-collect Renderers")]
        public void CollectRenderers()
        {
            ScreenRenderers.Clear();

            if (Computing != null && Computing.ScreenGameObject != null)
            {
                ScreenRenderers.AddRange(
                    Computing.ScreenGameObject.GetComponentsInChildren<Renderer>(true));
            }

            if (ScreenRenderers.Count == 0)
            {
                ScreenRenderers.AddRange(
                    GetComponentsInChildren<Renderer>(true));
            }

            CaptureOriginalMaterials();
        }

        private void AutoAssign()
        {
            if (Computing == null)
            {
                Computing = GetComponent<ItemComputingDataHolder>();
            }
        }

        private void CaptureOriginalMaterials()
        {
            originalMaterials.Clear();

            for (int i = 0; i < ScreenRenderers.Count; i++)
            {
                Renderer renderer = ScreenRenderers[i];
                originalMaterials.Add(
                    renderer != null ? renderer.sharedMaterials : System.Array.Empty<Material>());
            }
        }

        private void ApplyVisualState()
        {
            for (int i = 0; i < ScreenRenderers.Count; i++)
            {
                Renderer renderer = ScreenRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (isOn)
                {
                    if (i < originalMaterials.Count && originalMaterials[i].Length > 0)
                    {
                        renderer.sharedMaterials = originalMaterials[i];
                    }
                }
                else
                {
                    Material offMaterial = GetOffMaterial();
                    int slotCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                    Material[] offMaterials = new Material[slotCount];

                    for (int slot = 0; slot < slotCount; slot++)
                    {
                        offMaterials[slot] = offMaterial;
                    }

                    renderer.sharedMaterials = offMaterials;
                }
            }
        }

        private Material GetOffMaterial()
        {
            if (ScreenOffMaterial != null)
            {
                return ScreenOffMaterial;
            }

            if (runtimeOffMaterial != null)
            {
                return runtimeOffMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            runtimeOffMaterial = new Material(shader)
            {
                name = "device_screen_off_runtime",
                renderQueue = (int)RenderQueue.Geometry
            };

            if (runtimeOffMaterial.HasProperty("_BaseColor"))
            {
                runtimeOffMaterial.SetColor("_BaseColor", Color.black);
            }

            if (runtimeOffMaterial.HasProperty("_Color"))
            {
                runtimeOffMaterial.SetColor("_Color", Color.black);
            }

            return runtimeOffMaterial;
        }

        private void OnDestroy()
        {
            if (runtimeOffMaterial != null)
            {
                Destroy(runtimeOffMaterial);
                runtimeOffMaterial = null;
            }
        }
    }
}
