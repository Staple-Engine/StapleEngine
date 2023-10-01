namespace Staple.Internal
{
    /// <summary>
    /// Describes an asset that can be serialized as its path rather than its full data when used as a field
    /// </summary>
    public interface IPathAsset
    {
        /// <summary>
        /// The asset's path (if any)
        /// </summary>
        string Path { get; }

        static abstract object Create(string path);
    }
}
