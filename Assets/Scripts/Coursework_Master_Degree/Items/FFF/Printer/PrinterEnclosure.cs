using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Marks a physical enclosure. A printer inside its renderer bounds gets
    /// the enclosure thermal and quality treatment automatically.
    /// </summary>
    public class PrinterEnclosure : MonoBehaviour
    {
        [Tooltip("Extra space around the renderer bounds when testing containment.")]
        public Vector3 BoundsPadding = new Vector3(0.05f, 0.05f, 0.05f);

        public Printer Printer { get; private set; }

        public void BindPrinter(Printer printer)
        {
            Printer = printer;
        }

        public bool Contains(Vector3 worldPosition)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            if (renderers == null || renderers.Length == 0)
            {
                return false;
            }

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            bounds.Expand(BoundsPadding * 2.0f);
            return bounds.Contains(worldPosition);
        }
    }
}
