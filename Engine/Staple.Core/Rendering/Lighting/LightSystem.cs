using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

/// <summary>
/// Light rendering system
/// </summary>
public sealed class LightSystem : IRenderSystem
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct LightInstance
    {
        public Matrix4x4 transform;
        public Matrix3x3 normalMatrix;
        public Vector3 padding;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    private struct LightDetails
    {
        public LightType type;
        public Vector3 position;
        public Vector3 diffuse;
        public Vector3 specular;
        public Vector3 spotDirection;
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

    private readonly LightDetails[] cachedLights = new LightDetails[MaxLights];

    private readonly Dictionary<int, ShaderHandle[]> cachedInstancedMaterialInfo = [];

    private VertexBuffer lightDataBuffer;

    private readonly Lazy<VertexLayout> lightDataBufferLayout = new(() =>
    {
        return VertexLayoutBuilder.CreateNew()
            .Add(VertexAttribute.TexCoord0, VertexAttributeType.Float4)
            .Add(VertexAttribute.TexCoord1, VertexAttributeType.Float4)
            .Add(VertexAttribute.TexCoord2, VertexAttributeType.Float4)
            .Add(VertexAttribute.TexCoord3, VertexAttributeType.Float4)
            .Build();
    });

    public bool UsesOwnRenderProcess => false;

    public Type RelatedComponent => null;

    public Color AmbientColor => OverrideAmbientColor ?? AppSettings.Current.ambientLight;

    public (Entity, Transform, Light)[] Lights => OverrideLights ?? lightQuery.Contents;

    public LightSystem()
    {
    }

    #region Lifecycle
    public void Startup()
    {
    }

    public void Shutdown()
    {
    }

    public void Prepare()
    {
    }

    public void Preprocess(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Submit()
    {
    }
    #endregion

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
    /// <param name="material">The material to use</param>
    /// <param name="cameraPosition">The position of the camera</param>
    /// <param name="lighting">What lighting to use</param>
    /// <param name="state">The current rendering state</param>
    internal bool ApplyLightProperties(Material material, Vector3 cameraPosition,
        MaterialLighting lighting, ref RenderState state)
    {
        void EnsureStorageBuffer(ref RenderState state)
        {
            lightDataBuffer ??= VertexBuffer.Create(cachedLights.AsSpan(), lightDataBufferLayout.Value, RenderBufferFlags.GraphicsRead);

            if (lightDataBuffer == null)
            {
                return;
            }

            lightDataBuffer.Update(cachedLights.AsSpan());

            state.ApplyStorageBufferIfNeeded("StapleLights", lightDataBuffer);
        }

        if (Enabled == false ||
            lighting == MaterialLighting.Unlit ||
            (material?.IsValid ?? false) == false ||
            state.shaderInstance?.program == null)
        {
            EnsureStorageBuffer(ref state);

            return false;
        }

        var targets = Lights;

        var lightCount = targets.Length;

        if (lightCount > MaxLights)
        {
            lightCount = MaxLights;
        }

        var lightAmbient = AmbientColor;
        var lightCountValue = targets.Length;

        for (var i = 0; i < lightCount; i++)
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

            cachedLights[i].type = light.type;
            cachedLights[i].position = p;
            cachedLights[i].diffuse = (Vector3)light.color;
            cachedLights[i].spotDirection = forward;
        }

        var key = material.shader.Guid.GuidHash;

        static bool HandlesValid(Span<ShaderHandle> handles)
        {
            for (var i = 0; i < handles.Length; i++)
            {
                if (handles[i].IsValid == false)
                {
                    return false;
                }
            }

            return true;
        }

        if (cachedInstancedMaterialInfo.TryGetValue(key, out var handles) == false || HandlesValid(handles) == false)
        {
            handles = [
                material.GetShaderHandle("StapleLightViewPosition"),
                material.GetShaderHandle("StapleLightAmbientColor"),
                material.GetShaderHandle("StapleLightCount")
            ];

            cachedInstancedMaterialInfo.AddOrSetKey(key, handles);
        }

        if((handles?.Length ?? 0) != 3 ||
            HandlesValid(handles) == false)
        {
            EnsureStorageBuffer(ref state);

            return false;
        }

        var viewPosHandle = handles[0];
        var lightAmbientHandle = handles[1];
        var lightCountHandle = handles[2];

        material.shader.SetVector3(material.ShaderVariantKey, viewPosHandle, cameraPosition);
        material.shader.SetColor(material.ShaderVariantKey, lightAmbientHandle, lightAmbient);
        material.shader.SetFloat(material.ShaderVariantKey, lightCountHandle, lightCountValue);

        EnsureStorageBuffer(ref state);

        return true;
    }
}
