using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

/// <summary>
/// Light rendering system
/// </summary>
public sealed class LightSystem
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct LightInstance
    {
        public Matrix4x4 transform;
        public Matrix3x3 normalMatrix;
        public Vector3 padding;
    }

    /// <summary>
    /// Limit to 16 lights
    /// </summary>
    public const int MaxLights = 16;

    /// <summary>
    /// Enable or disable the light system
    /// </summary>
    public static bool Enabled = true;

    /// <summary>
    /// Override the lights used by the light system
    /// </summary>
    public static (Entity, Transform, Light)[] OverrideLights = null;

    /// <summary>
    /// Override the ambient color
    /// </summary>
    public static Color? OverrideAmbientColor = null;

    private readonly SceneQuery<Transform, Light> lightQuery = new();

    private readonly Vector4[] cachedLightTypePositions = new Vector4[MaxLights];
    private readonly Color[] cachedLightDiffuse = new Color[MaxLights];

    private readonly Dictionary<int, ShaderHandle[]> cachedInstancedMaterialInfo = [];

    public bool UsesOwnRenderProcess => false;

    public Type RelatedComponent => null;

    public Color AmbientColor => OverrideAmbientColor ?? AppSettings.Current.ambientLight;

    public (Entity, Transform, Light)[] Lights => OverrideLights ?? lightQuery.Contents;

    public static readonly LightSystem Instance = new();

    /// <summary>
    /// Applies the material-specific lighting state
    /// </summary>
    /// <param name="material">The material to use</param>
    /// <param name="lighting">The lighting type</param>
    public void ApplyMaterialLighting(Material material, MaterialLighting lighting)
    {
        if(!Enabled)
        {
            material.DisableShaderKeyword(Shader.LitKeyword);
            material.DisableShaderKeyword(Shader.HalfLambertKeyword);

            return;
        }

        switch (lighting)
        {
            case MaterialLighting.Lit:

                material.EnableShaderKeyword(Shader.LitKeyword);
                material.DisableShaderKeyword(Shader.HalfLambertKeyword);

                break;

            case MaterialLighting.Unlit:

                material.DisableShaderKeyword(Shader.LitKeyword);
                material.DisableShaderKeyword(Shader.HalfLambertKeyword);

                break;

            case MaterialLighting.HalfLambert:

                material.EnableShaderKeyword(Shader.LitKeyword);
                material.EnableShaderKeyword(Shader.HalfLambertKeyword);

                break;
        }
    }

    /// <summary>
    /// Applies light properties to the next render pass
    /// </summary>
    /// <param name="material">The material to use</param>
    /// <param name="cameraPosition">The position of the camera</param>
    /// <param name="lighting">What lighting to use</param>
    /// <param name="state">The current rendering state</param>
    internal bool ApplyLightProperties(Material material, Vector3 cameraPosition,
        MaterialLighting lighting)
    {
        if (!Enabled ||
            lighting == MaterialLighting.Unlit ||
            !(material?.IsValid ?? false))
        {
            return false;
        }

        var targets = Lights;

        var lightCount = targets.Length;

        if (lightCount > MaxLights)
        {
            lightCount = MaxLights;
        }

        var lightAmbient = AmbientColor;

        for (var i = 0; i < lightCount; i++)
        {
            var (_, t, light) = targets[i];
            var p = t.Position;
            var forward = t.Forward;

            if (light.type == LightType.Directional)
            {
                p = -forward;
            }

            cachedLightTypePositions[i] = new((float)light.type, p.X, p.Y, p.Z);
            cachedLightDiffuse[i] = light.color;
        }

        var key = HashCode.Combine(material.shader.Guid.GuidHash, material.ShaderVariantKey);

        static bool HandlesValid(Span<ShaderHandle> handles)
        {
            for (var i = 0; i < handles.Length; i++)
            {
                if (!handles[i].IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        if (!cachedInstancedMaterialInfo.TryGetValue(key, out var handles) || !HandlesValid(handles))
        {
            handles = [
                material.GetShaderHandle("StapleLightCountViewPosition"),
                material.GetShaderHandle("StapleLightAmbientColor"),
                material.GetShaderHandle("StapleLightTypePosition"),
                material.GetShaderHandle("StapleLightDiffuse"),
            ];

            cachedInstancedMaterialInfo.AddOrSetKey(key, handles);
        }

        if((handles?.Length ?? 0) != 4 ||
            !HandlesValid(handles))
        {
            return false;
        }

        var viewPosHandle = handles[0];
        var lightAmbientHandle = handles[1];
        var lightTypePositionHandle = handles[2];
        var lightDiffuseHandle = handles[3];

        material.shader.SetVector4(material.ShaderVariantKey, viewPosHandle, new Vector4(lightCount, cameraPosition.X, cameraPosition.Y, cameraPosition.Z));
        material.shader.SetColor(material.ShaderVariantKey, lightAmbientHandle, lightAmbient);
        material.shader.SetVector4(material.ShaderVariantKey, lightTypePositionHandle, cachedLightTypePositions);
        material.shader.SetColor(material.ShaderVariantKey, lightDiffuseHandle, cachedLightDiffuse);

        return true;
    }
}
