namespace Staple;

/// <summary>
/// Camera clearing mode
/// </summary>
public enum CameraClearMode
{
    /// <summary>
    /// Don't clear
    /// </summary>
    None,

    /// <summary>
    /// Clear depth
    /// </summary>
    Depth,

    /// <summary>
    /// Clear depth and color
    /// </summary>
    SolidColor,

    /// <summary>
    /// Use a skybox. Requires a skybox material to be applied to the camera.
    /// </summary>
    Skybox,
}