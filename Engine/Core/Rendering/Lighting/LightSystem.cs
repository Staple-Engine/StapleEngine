using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Light rendering system
/// </summary>
public sealed class LightSystem : IRenderSystem
{
    /// <summary>
    /// Limit to 16 lights
    /// </summary>
    public const int MaxLights = 16;

    /// <summary>
    /// Enable or disable the light system
    /// </summary>
    public static bool Enabled = true;

    private static readonly string LightAmbientKey = "u_lightAmbient";
    private static readonly string LightCountKey = "u_lightCount";
    private static readonly string LightDiffuseKey = "u_lightDiffuse";
    private static readonly string LightSpecularKey = "u_lightSpecular";
    private static readonly string LightTypePositionKey = "u_lightTypePosition";
    private static readonly string LightSpotDirectionKey = "u_lightSpotDirection";
    private static readonly string LightSpotValuesKey = "u_lightSpotValues";
    private static readonly string NormalMatrixKey = "u_normalMatrix";
    private static readonly string ViewPosKey = "u_viewPos";

    private readonly SceneQuery<Transform, Light> lightQuery = new();

    private readonly Vector4[] cachedLightTypePositions = new Vector4[MaxLights];
    private readonly Vector4[] cachedLightDiffuse = new Vector4[MaxLights];
    private readonly Vector4[] cachedLightSpotDirection = new Vector4[MaxLights];

    private readonly Dictionary<int, (ShaderHandle, ShaderHandle, ShaderHandle,
        ShaderHandle, ShaderHandle, ShaderHandle,
        ShaderHandle, ShaderHandle, ShaderHandle)> cachedMaterialInfo = [];

    public LightSystem()
    {
        Shader.DefaultUniforms.Add((LightAmbientKey, ShaderUniformType.Color));
        Shader.DefaultUniforms.Add((LightCountKey, ShaderUniformType.Vector4));
        Shader.DefaultUniforms.Add(($"{LightDiffuseKey}[{MaxLights}]", ShaderUniformType.Vector4));
        Shader.DefaultUniforms.Add(($"{LightSpecularKey}[{MaxLights}]", ShaderUniformType.Vector4));
        Shader.DefaultUniforms.Add(($"{LightTypePositionKey}[{MaxLights}]", ShaderUniformType.Vector4));
        Shader.DefaultUniforms.Add(($"{LightSpotDirectionKey}[{MaxLights}]", ShaderUniformType.Vector4));
        Shader.DefaultUniforms.Add(($"{LightSpotValuesKey}[{MaxLights}]", ShaderUniformType.Vector4));
        Shader.DefaultUniforms.Add((NormalMatrixKey, ShaderUniformType.Matrix3x3));
        Shader.DefaultUniforms.Add((ViewPosKey, ShaderUniformType.Vector3));
    }

    public void Destroy()
    {
    }

    public void Prepare()
    {
    }

    public void Preprocess((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
    }

    public Type RelatedComponent()
    {
        return null;
    }

    public void Submit()
    {
    }

    /// <summary>
    /// Applies the material-specific lighting state
    /// </summary>
    /// <param name="material">The material to use</param>
    /// <param name="lighting">The lighting type</param>
    public void ApplyMaterialLighting(Material material, MaterialLighting lighting)
    {
        if(Enabled == false)
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
    /// <param name="position">The position of the renderable</param>
    /// <param name="transform">The transform of the renderable</param>
    /// <param name="material">The material to use</param>
    /// <param name="cameraPosition">The position of the camera</param>
    /// <param name="lighting">What lighting to use</param>
    public void ApplyLightProperties(Vector3 position, Matrix4x4 transform, Material material, Vector3 cameraPosition,
        MaterialLighting lighting)
    {
        if (Enabled == false ||
            lighting == MaterialLighting.Unlit ||
            (material?.IsValid ?? false) == false ||
            lightQuery.Length == 0)
        {
            return;
        }

        var targets = lightQuery.Contents;

        if (targets.Length > MaxLights)
        {
            static (Entity, Transform, Light)[] Trim((Entity, Transform, Light)[] targets, Vector3 position)
            {
                return targets
                    .OrderBy(x => Vector3.DistanceSquared(x.Item2.Position, position))
                    .Take(MaxLights)
                    .ToArray();
            }

            targets = Trim(targets, position);
        }

        Matrix4x4.Invert(transform, out var invTransform);

        var transTransform = Matrix4x4.Transpose(invTransform);

        var normalMatrix = transTransform.ToMatrix3x3();

        var lightAmbient = AppSettings.Current.ambientLight;
        var lightCount = new Vector4(targets.Length);

        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            var light = target.Item3;
            var t = target.Item2;
            var p = t.Position;
            var forward = t.Forward;

            if (light.type == LightType.Directional)
            {
                p = -forward;
            }

            cachedLightTypePositions[i] = new((float)light.type, p.X, p.Y, p.Z);

            cachedLightDiffuse[i] = light.color;

            cachedLightSpotDirection[i] = forward.ToVector4();
        }

        var key = material.shader.Guid.GetHashCode();

        if(cachedMaterialInfo.TryGetValue(key, out var handles) == false)
        {
            handles = (material.GetShaderHandle(ViewPosKey),
                material.GetShaderHandle(NormalMatrixKey),
                material.GetShaderHandle(LightAmbientKey),
                material.GetShaderHandle(LightCountKey),
                material.GetShaderHandle(LightTypePositionKey),
                material.GetShaderHandle(LightDiffuseKey),
                material.GetShaderHandle(LightSpecularKey),
                material.GetShaderHandle(LightSpotDirectionKey),
                material.GetShaderHandle(LightSpotValuesKey));

            cachedMaterialInfo.Add(key, handles);
        }

        var viewPosHandle = handles.Item1;
        var normalMatrixHandle = handles.Item2;
        var lightAmbientHandle = handles.Item3;
        var lightCountHandle = handles.Item4;
        var lightTypePositionHandle = handles.Item5;
        var lightDiffuseHandle = handles.Item6;
        var lightSpecularHandle = handles.Item7;
        var lightSpotDirectionHandle = handles.Item8;
        var lightSpotValues = handles.Item9;

        material.shader.SetVector3(viewPosHandle, cameraPosition);
        material.shader.SetMatrix3x3(normalMatrixHandle, normalMatrix);
        material.shader.SetColor(lightAmbientHandle, lightAmbient);
        material.shader.SetVector4(lightCountHandle, lightCount);
        material.shader.SetVector4(lightTypePositionHandle, cachedLightTypePositions);
        material.shader.SetVector4(lightDiffuseHandle, cachedLightDiffuse);
        material.shader.SetVector4(lightSpotDirectionHandle, cachedLightSpotDirection);
    }
}
