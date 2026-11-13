using Coursework_Master_Degree.Items.Surface;

namespace Coursework_Master_Degree.Items.Contracts
{
    /// <summary>
    /// Common contract for objects that can be stored on an <see cref="ItemSurfaceDataHolder"/> shelf.
    /// Keeps shelf registration and the <c>IsPlaced</c> / <c>ParentSurfaceDataHolder</c> state in sync.
    /// </summary>
    public interface ISurfacePlaceable
    {
        public bool IsPlaced { get; }
        public ItemSurfaceDataHolder ParentSurfaceDataHolder { get; }

        /// <summary>
        /// Registers the object on the given surface and marks it as placed.
        /// Returns true when the placement state or shelf registration changed.
        /// </summary>
        public bool PlaceOnSurface(ItemSurfaceDataHolder surface);

        /// <summary>
        /// Removes the object from its current surface and marks it as not placed.
        /// Returns true when the placement state or shelf registration changed.
        /// </summary>
        public bool RemoveFromSurface();
    }
}
