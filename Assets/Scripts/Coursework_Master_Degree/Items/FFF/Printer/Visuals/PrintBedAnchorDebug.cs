using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// M0 calibration helper. Spawns a cube flush on the bed anchor so the
    /// plate position and orientation can be checked by eye.
    /// Editor-only helper: remove the spawned cube before saving the scene
    /// if you do not want it persisted.
    /// </summary>
    public class PrintBedAnchorDebug : MonoBehaviour
    {
        public PrintBedAnchor Anchor;

        [Tooltip("Cube size in metres. A real print scale is usually 0.01..0.05.")]
        public Vector3 CubeSize = new Vector3(0.02f, 0.02f, 0.02f);

        public Material CubeMaterial;

        [Tooltip("Spawn automatically when entering play mode")]
        public bool SpawnOnStart = true;

        private GameObject instance;

        private void Awake()
        {
            if (Anchor == null)
            {
                Anchor = GetComponent<PrintBedAnchor>();
            }
        }

        private void Start()
        {
            if (SpawnOnStart)
            {
                Spawn();
            }
        }

        [ContextMenu("Spawn Test Cube")]
        public void Spawn()
        {
            if (Anchor == null)
            {
                Debug.LogWarning("[PrintBedAnchorDebug] No PrintBedAnchor assigned.", this);
                return;
            }

            if (instance != null)
            {
                return;
            }

            instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = "print_bed_test_cube";
            instance.layer = LayerMask.NameToLayer("Ignore Raycast");

            Collider cubeCollider = instance.GetComponent<Collider>();
            if (cubeCollider != null)
            {
                DestroySafe(cubeCollider);
            }

            instance.transform.SetParent(Anchor.transform, false);
            instance.transform.localPosition = new Vector3(
                0.0f,
                Anchor.SurfaceOffset + CubeSize.y * 0.5f,
                0.0f);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = CubeSize;

            if (CubeMaterial != null)
            {
                MeshRenderer meshRenderer = instance.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sharedMaterial = CubeMaterial;
                }
            }
        }

        [ContextMenu("Clear Test Cube")]
        public void Clear()
        {
            if (instance == null)
            {
                return;
            }

            DestroySafe(instance);
            instance = null;
        }

        private void OnDestroy()
        {
            Clear();
        }

        private void DestroySafe(Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
