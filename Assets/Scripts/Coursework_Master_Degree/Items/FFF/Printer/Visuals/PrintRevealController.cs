using System.Collections.Generic;
using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Drives the spool printing shader's reveal property on every object
    /// renderer. The property is expressed relative to each renderer pivot,
    /// so it stays correct while the bed moves and the object is scaled.
    /// </summary>
    internal sealed class PrintRevealController
    {
        private static readonly int RevealHeightId =
            Shader.PropertyToID("_reveal_height");

        private sealed class RendererBinding
        {
            public Renderer Renderer;
            public Material[] Materials;
            public MaterialPropertyBlock Block;
            public float Min;
            public float Max;
            public bool HasReveal;
        }

        private readonly List<RendererBinding> bindings =
            new List<RendererBinding>();

        public int RendererCount => bindings.Count;

        public void Begin(GameObject root, Material printingMaterial)
        {
            bindings.Clear();

            if (root == null)
            {
                return;
            }

            Renderer[] renderers =
                root.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null ||
                    (renderer is not MeshRenderer &&
                     renderer is not SkinnedMeshRenderer))
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;
                float height = bounds.size.y;

                if (height <= 0.0001f)
                {
                    continue;
                }

                float pivotY = renderer.transform.position.y;
                float padding = Mathf.Max(0.0005f, height * 0.001f);

                RendererBinding binding = new RendererBinding
                {
                    Renderer = renderer,
                    Min = bounds.min.y - pivotY - padding,
                    Max = bounds.max.y - pivotY + padding,
                    Block = new MaterialPropertyBlock(),
                };

                ApplyMaterial(binding, printingMaterial);
                binding.HasReveal =
                    renderer.sharedMaterial != null &&
                    renderer.sharedMaterial.HasProperty(RevealHeightId);

                if (!binding.HasReveal)
                {
                    Debug.LogWarning(
                        $"[PrintReveal] '{renderer.name}' material has no " +
                        $"'{RevealHeightId}' property; the object will not " +
                        "reveal progressively.",
                        renderer);
                }

                bindings.Add(binding);
            }

            SetProgress(0.0f);
        }

        public void SetProgress(float normalized)
        {
            float t = Mathf.Clamp01(normalized);

            for (int i = 0; i < bindings.Count; i++)
            {
                RendererBinding binding = bindings[i];

                if (!binding.HasReveal || binding.Renderer == null)
                {
                    continue;
                }

                binding.Block.SetFloat(
                    RevealHeightId,
                    Mathf.Lerp(binding.Min, binding.Max, t));
                binding.Renderer.SetPropertyBlock(binding.Block);
            }
        }

        public void SetFull()
        {
            SetProgress(1.0f);
        }

        public void SetFinalMaterial(Material finalMaterial)
        {
            if (finalMaterial == null)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                RendererBinding binding = bindings[i];

                if (binding.Renderer == null)
                {
                    continue;
                }

                ApplyMaterial(binding, finalMaterial);
                binding.Renderer.SetPropertyBlock(null);
                binding.HasReveal = false;
            }
        }

        public void SetPrintingMaterial(Material printingMaterial)
        {
            if (printingMaterial == null)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                RendererBinding binding = bindings[i];

                if (binding.Renderer == null)
                {
                    continue;
                }

                ApplyMaterial(binding, printingMaterial);
                binding.HasReveal =
                    binding.Renderer.sharedMaterial != null &&
                    binding.Renderer.sharedMaterial.HasProperty(RevealHeightId);
            }
        }

        public void SetVisible(bool visible)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                RendererBinding binding = bindings[i];

                if (binding.Renderer != null)
                {
                    binding.Renderer.enabled = visible;
                }
            }
        }

        private static void ApplyMaterial(
            RendererBinding binding,
            Material material)
        {
            Renderer renderer = binding.Renderer;

            if (renderer == null || material == null)
            {
                return;
            }

            int materialCount = renderer.sharedMaterials.Length;

            if (materialCount <= 0)
            {
                renderer.sharedMaterial = material;
                return;
            }

            if (binding.Materials == null ||
                binding.Materials.Length != materialCount)
            {
                binding.Materials = new Material[materialCount];
            }

            for (int i = 0; i < binding.Materials.Length; i++)
            {
                binding.Materials[i] = material;
            }

            renderer.sharedMaterials = binding.Materials;
        }
    }
}
