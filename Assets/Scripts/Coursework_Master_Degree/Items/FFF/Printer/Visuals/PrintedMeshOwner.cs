using UnityEngine;

namespace Coursework_Master_Degree.Items.FFF
{
    /// <summary>
    /// Owns a generated mesh for a printed object and destroys it with the
    /// object. Used by the sliced partial surface/collider so selling or
    /// clearing a print leaves no generated mesh behind.
    /// </summary>
    public sealed class PrintedMeshOwner : MonoBehaviour
    {
        public Mesh Mesh;

        private void OnDestroy()
        {
            if (Mesh != null)
            {
                Destroy(Mesh);
                Mesh = null;
            }
        }
    }
}
