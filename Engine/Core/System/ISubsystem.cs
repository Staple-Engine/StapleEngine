namespace Staple
{
    /// <summary>
    /// The type of subsystem
    /// </summary>
    public enum SubsystemType
    {
        /// <summary>
        /// Runs at the fixed tick rate
        /// </summary>
        FixedUpdate,

        /// <summary>
        /// Runs every frame
        /// </summary>
        Update
    }

    /// <summary>
    /// Subsystem interface
    /// </summary>
    public interface ISubsystem
    {
        /// <summary>
        /// The type of subsystem
        /// </summary>
        SubsystemType type { get; }

        /// <summary>
        /// Run at startup. Initialize the subsystem here.
        /// </summary>
        void Startup();

        /// <summary>
        /// Run at cleanup. Cleanup the subsystem here.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Run during the update time.
        /// </summary>
        void Update();
    }
}
