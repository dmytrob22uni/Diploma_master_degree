using System.Collections.Generic;
using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.ObjectsToPrint;

namespace Coursework_Master_Degree.Items.FFF.Jobs
{
    /// <summary>
    /// Derives object dimensions from the catalog prefab. The result is cached
    /// per meta for the session; no size authoring is required.
    /// </summary>
    public static class ObjectToPrintSizing
    {
        private static readonly Dictionary<ObjectToPrintMetaSO, Vector3> cache =
            new Dictionary<ObjectToPrintMetaSO, Vector3>();

        public static Vector3 GetSizeMeters(ObjectToPrintMetaSO meta)
        {
            if (meta == null)
            {
                return Vector3.zero;
            }

            if (cache.TryGetValue(meta, out Vector3 cached))
            {
                return cached;
            }

            Vector3 size = Vector3.zero;

            if (meta.Prefab != null)
            {
                GameObject instance = Object.Instantiate(meta.Prefab);
                instance.SetActive(true);

                Renderer[] renderers =
                    instance.GetComponentsInChildren<Renderer>(true);
                bool found = false;
                Bounds bounds = new Bounds(instance.transform.position, Vector3.zero);

                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];

                    if (renderer == null ||
                        !renderer.enabled ||
                        renderer.bounds.size.sqrMagnitude < 0.0000001f)
                    {
                        continue;
                    }

                    if (!found)
                    {
                        bounds = renderer.bounds;
                        found = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                Object.Destroy(instance);

                if (found)
                {
                    size = bounds.size;
                }
            }

            cache[meta] = size;
            return size;
        }
    }
}
