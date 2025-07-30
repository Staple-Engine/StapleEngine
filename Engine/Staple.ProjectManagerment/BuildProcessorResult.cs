namespace Staple.ProjectManagement;

/// <summary>
/// The result of a build pre/postprocessor execution
/// </summary>
public enum BuildProcessorResult
{
    /// <summary>
    /// The processor failed, abort build
    /// </summary>
    Failed,

    /// <summary>
    /// The processor won't do anything, continue
    /// </summary>
    Continue,

    /// <summary>
    /// The processor was successful
    /// </summary>
    Success
}
