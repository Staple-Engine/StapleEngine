namespace Staple
{
    /// <summary>
    /// Interface for logging
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Logs a normal message
        /// </summary>
        /// <param name="message">The message to log</param>
        void Info(string message);
        
        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The message to log</param>
        void Warning(string message);
        
        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">The message to log</param>
        void Error(string message);

        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">The message to log</param>
        void Debug(string message);

        /// <summary>
        /// Cleans up the log, finishing execution
        /// </summary>
        void Cleanup();
    }
}