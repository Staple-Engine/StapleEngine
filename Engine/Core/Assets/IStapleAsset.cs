namespace Staple.Internal
{
    /// <summary>
    /// Represents a staple asset
    /// </summary>
    public interface IStapleAsset
    {
        /// <summary>
        /// Called before serializing
        /// </summary>
        void OnBeforeSerialize();

        /// <summary>
        /// Called after serializing
        /// </summary>
        void OnAfterSerialize();

        /// <summary>
        /// Called before deserializing
        /// </summary>
        void OnBeforeDeserialize();

        /// <summary>
        /// Called after deserializing
        /// </summary>
        void OnAfterDeserialize();
    }
}
