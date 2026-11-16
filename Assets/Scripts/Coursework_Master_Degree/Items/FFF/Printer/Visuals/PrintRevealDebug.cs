using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// M0 calibration helper for the object reveal shader.
    /// Drives _reveal_height on one renderer so the cutoff can be checked at
    /// empty, mid-height, and full height before print logic exists.
    /// The shader uses a strict "below cutoff" test, so the debug range is
    /// padded slightly beyond the mesh bounds at both ends. Works with either
    /// centre or bottom pivots because the range is measured relative to the
    /// renderer pivot.
    /// </summary>
    public class PrintRevealDebug : MonoBehaviour
    {
        private const string DefaultPropertyName = "_reveal_height";

        public Renderer TargetRenderer;

        [Tooltip("Shader float property used as the reveal cutoff")]
        public string PropertyName = DefaultPropertyName;

        [Range(0.0f, 1.0f)]
        public float Normalized = 0.0f;

        [Tooltip(
            "Extra margin beyond the mesh bounds at t=0 and t=1.\n" +
            "The shader clips fragments at the exact cutoff, so without this " +
            "the bottom or top face can remain visible.")]
        [Min(0.0f)]
        public float EndpointPadding = 0.0005f;

        [Tooltip("Show an in-game slider while playing")]
        public bool ShowGui = true;

        private MaterialPropertyBlock propertyBlock;
        private int propertyId;
        private float minLocalReveal;
        private float maxLocalReveal;
        private float lastAppliedNormalized = -1.0f;
        private bool isInitialized;

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            if (!Application.isPlaying || !isInitialized)
            {
                return;
            }

            if (!Mathf.Approximately(Normalized, lastAppliedNormalized))
            {
                Apply();
            }
        }

        private void OnValidate()
        {
            propertyId = Shader.PropertyToID(PropertyName);

            if (Application.isPlaying && isInitialized)
            {
                CacheLocalRevealRange();
                Apply();
            }
        }

        [ContextMenu("Reinitialize")]
        public void Initialize()
        {
            if (TargetRenderer == null)
            {
                TargetRenderer = GetComponentInChildren<Renderer>();
            }

            propertyId = Shader.PropertyToID(
                string.IsNullOrWhiteSpace(PropertyName)
                    ? DefaultPropertyName
                    : PropertyName);

            if (TargetRenderer == null)
            {
                Debug.LogWarning(
                    "[PrintRevealDebug] No Renderer assigned or found in children.",
                    this);
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            CacheLocalRevealRange();
            isInitialized = true;
            Apply();

            Material material = TargetRenderer.sharedMaterial;
            if (material == null || !material.HasProperty(propertyId))
            {
                Debug.LogWarning(
                    $"[PrintRevealDebug] Material on '{TargetRenderer.name}' " +
                    $"has no '{PropertyName}' property. Assign a material that " +
                    "uses the reveal shader.",
                    this);
            }
        }

        public void SetNormalized(float normalized)
        {
            Normalized = Mathf.Clamp01(normalized);
            Apply();
        }

        public void SetEmpty()
        {
            SetNormalized(0.0f);
        }

        public void SetMid()
        {
            SetNormalized(0.5f);
        }

        public void SetFull()
        {
            SetNormalized(1.0f);
        }

        private void CacheLocalRevealRange()
        {
            // The shader compares world-space Y against this renderer's pivot,
            // so measure the world Y extent relative to the pivot. This also
            // handles renderers that are rotated or scaled under the object.
            Bounds worldBounds = TargetRenderer.bounds;
            float pivotY = TargetRenderer.transform.position.y;

            minLocalReveal = worldBounds.min.y - pivotY;
            maxLocalReveal = worldBounds.max.y - pivotY;

            float height = Mathf.Max(0.0f, maxLocalReveal - minLocalReveal);
            float padding = Mathf.Max(
                EndpointPadding,
                height * 0.001f);

            minLocalReveal -= padding;
            maxLocalReveal += padding;
        }

        private void Apply()
        {
            if (!isInitialized || TargetRenderer == null)
            {
                return;
            }

            float value = Mathf.Lerp(
                minLocalReveal,
                maxLocalReveal,
                Mathf.Clamp01(Normalized));

            propertyBlock ??= new MaterialPropertyBlock();
            TargetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(propertyId, value);
            TargetRenderer.SetPropertyBlock(propertyBlock);

            lastAppliedNormalized = Normalized;
        }

        private void OnGUI()
        {
            if (!ShowGui || !Application.isPlaying || !isInitialized)
            {
                return;
            }

            const float width = 260.0f;
            const float height = 96.0f;
            Rect rect = new Rect(Screen.width - width - 10.0f, 10.0f, width, height);
            GUI.Box(rect, "Print reveal debug");

            Normalized = GUI.HorizontalSlider(
                new Rect(rect.x + 10.0f, rect.y + 24.0f, rect.width - 20.0f, 20.0f),
                Normalized,
                0.0f,
                1.0f);

            GUI.Label(
                new Rect(rect.x + 10.0f, rect.y + 44.0f, rect.width - 20.0f, 20.0f),
                $"t={Normalized:0.00}  y={Mathf.Lerp(minLocalReveal, maxLocalReveal, Normalized):0.000} m");

            if (GUI.Button(new Rect(rect.x + 10.0f, rect.y + 66.0f, 72.0f, 22.0f), "Empty"))
            {
                SetEmpty();
            }

            if (GUI.Button(new Rect(rect.x + 88.0f, rect.y + 66.0f, 72.0f, 22.0f), "Mid"))
            {
                SetMid();
            }

            if (GUI.Button(new Rect(rect.x + 166.0f, rect.y + 66.0f, 72.0f, 22.0f), "Full"))
            {
                SetFull();
            }
        }
    }
}
