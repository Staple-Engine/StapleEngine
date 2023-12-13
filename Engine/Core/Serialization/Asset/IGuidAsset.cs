namespace Staple
{
    /// <summary>
    /// Describes an asset that can be serialized as its guid rather than its full data when used as a field
    /// </summary>
    public interface IGuidAsset
    {
        /// <summary>
        /// The asset's path (if any)
        /// </summary>
        string Guid { get; set; }

        static abstract object Create(string guid);
    }
}
