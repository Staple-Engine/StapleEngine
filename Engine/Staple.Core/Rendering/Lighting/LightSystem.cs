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
    /// Limit light count
    /// </summary>
    public const int MaxLights = 128;

    /// <summary>
    /// Enable or disable the light system
    /// </summary>
    public static bool Enabled = true;

    /// <summary>
    /// Override the lights used by the light system
    /// </summary>
    public static Light[] OverrideLights = null;

    /// <summary>
    /// Override the ambient color
    /// </summary>
    public static Color? OverrideAmbientColor = null;

    private readonly SceneQuery<Light> lightQuery = new();

    private readonly Vector4[] cachedLightTypePositions = new Vector4[MaxLights];
    private readonly Color[] cachedLightDiffuseIntensity = new Color[MaxLights];
    private readonly Vector4[] cachedLightExtras = new Vector4[MaxLights];

    private readonly Dictionary<int, ShaderHandle[]> cachedInstancedMaterialInfo = [];

    private Material previousMaterial;
    private Shader previousShader;
    private Vector4 previousLightCount;
    private Color previousLightAmbient;
    private readonly Vector4[] previousLightTypePositions = new Vector4[MaxLights];
    private readonly Color[] previousLightDiffuseIntensity = new Color[MaxLights];
    private readonly Vector4[] previousLightExtras = new Vector4[MaxLights];

    public Color AmbientColor => OverrideAmbientColor ?? AppSettings.Active.ambientLight;

    public Span<Light> Lights => OverrideLights != null ? OverrideLights.AsSpan() : lightQuery.Contents;

    public static readonly LightSystem Instance = new();

    /// <summary>
    /// Called at the start of a frame
    /// </summary>
    internal void StartFrame()
    {
        previousShader = null;
        previousMaterial = null;
    }

    /// <summary>
    /// Applies light properties to the next render pass
    /// </summary>
    /// <param name="material">The material to use</param>
    /// <param name="cameraPosition">The position of the camera</param>
    /// <param name="disableLighting">Whether lighting should be disabled</param>
    internal bool ApplyLightProperties(Material material, Vector3 cameraPosition, bool disableLighting)
    {
        if (!Enabled ||
            !(material?.IsValid ?? false))
        {
            return false;
        }

        var key = HashCode.Combine(material.materialResource.shader.Guid.GuidHash, material.ShaderVariantKey);

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
                material.GetShaderHandle("StapleLightDiffuseIntensity"),
                material.GetShaderHandle("StapleLightExtras"),
            ];

            cachedInstancedMaterialInfo.AddOrSetKey(key, handles);
        }

        if (!HandlesValid(handles))
        {
            return false;
        }

        var targets = Lights;

        var lightCount = 0;

        var layerMask = RenderSystem.CurrentCamera.camera.cullingLayers;

        if (!disableLighting)
        {
            for (var i = 0; i < targets.Length; i++)
            {
                if (!targets[i].Entity.IsValid || !layerMask.HasLayer(targets[i].Entity.Layer))
                {
                    continue;
                }

                lightCount++;

                if(lightCount >= MaxLights)
                {
                    break;
                }
            }
        }

        var lightAmbient = AmbientColor;

        if(!disableLighting)
        {
            for (int i = 0, counter = 0; i < targets.Length; i++)
            {
                if (!targets[i].Entity.IsValid || !layerMask.HasLayer(targets[i].Entity.Layer))
                {
                    continue;
                }

                var light = targets[i];
                var t = light.Transform;
                var p = t.Position;
                var forward = t.Forward;

                if (light.type == LightType.Directional)
                {
                    p = -forward;
                }

                cachedLightTypePositions[counter] = new((float)light.type, p.X, p.Y, p.Z);

                ref var diffuseIntensity = ref cachedLightDiffuseIntensity[counter];

                diffuseIntensity = light.color;
                diffuseIntensity.a = light.intensity;

                switch(light.type)
                {
                    case LightType.Point:

                        ref var extras = ref cachedLightExtras[counter];

                        extras.X = 1.0f / Math.Max(light.range * light.range, 0.00001f);

                        break;
                }

                counter++;

                if(counter >= lightCount)
                {
                    break;
                }
            }
        }

        var viewPosHandle = handles[0];
        var lightAmbientHandle = handles[1];
        var lightTypePositionHandle = handles[2];
        var lightDiffuseHandle = handles[3];
        var lightExtrasHandle = handles[4];

        var needsReplaceCount = false;
        var needsReplaceAmbient = false;
        var needsReplaceTypePositions = false;
        var needsReplaceDiffuse = false;
        var needsReplaceExtras = false;

        var newLightCount = new Vector4(lightCount, cameraPosition.X, cameraPosition.Y, cameraPosition.Z);

        if (previousShader != material.materialResource.shader ||
            previousMaterial != material)
        {
            previousShader = material.materialResource.shader;
            previousMaterial = material;

            needsReplaceCount = needsReplaceAmbient = needsReplaceTypePositions = needsReplaceDiffuse = needsReplaceExtras = true;
        }
        else
        {
            needsReplaceCount = previousLightCount != newLightCount;

            needsReplaceAmbient = previousLightAmbient != lightAmbient;

            for(var i = 0; i < MaxLights; i++)
            {
                if(!needsReplaceTypePositions && cachedLightTypePositions[i] != previousLightTypePositions[i])
                {
                    needsReplaceTypePositions = true;
                }

                if(!needsReplaceDiffuse && cachedLightDiffuseIntensity[i] != previousLightDiffuseIntensity[i])
                {
                    needsReplaceDiffuse = true;
                }

                if(!needsReplaceExtras && cachedLightExtras[i] != previousLightExtras[i])
                {
                    needsReplaceExtras = true;
                }

                if(needsReplaceDiffuse && needsReplaceTypePositions && needsReplaceExtras)
                {
                    break;
                }
            }
        }

        if(needsReplaceCount)
        {
            previousLightCount = newLightCount;

            material.materialResource.shader.SetVector4(material.ShaderVariantKey, viewPosHandle, newLightCount);
        }

        if(needsReplaceAmbient)
        {
            previousLightAmbient = lightAmbient;

            material.materialResource.shader.SetColor(material.ShaderVariantKey, lightAmbientHandle, lightAmbient);
        }

        if(needsReplaceTypePositions)
        {
            cachedLightTypePositions.AsSpan().CopyTo(previousLightTypePositions);

            material.materialResource.shader.SetVector4(material.ShaderVariantKey, lightTypePositionHandle, cachedLightTypePositions);
        }

        if(needsReplaceDiffuse)
        {
            cachedLightDiffuseIntensity.AsSpan().CopyTo(previousLightDiffuseIntensity);

            material.materialResource.shader.SetColor(material.ShaderVariantKey, lightDiffuseHandle, cachedLightDiffuseIntensity);
        }

        if (needsReplaceExtras)
        {
            cachedLightExtras.AsSpan().CopyTo(previousLightExtras);

            material.materialResource.shader.SetVector4(material.ShaderVariantKey, lightExtrasHandle, cachedLightExtras);
        }

        return true;
    }
}
