using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
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
    /// Normal light count
    /// </summary>
    public const int MaxLights = 32;

    /// <summary>
    /// For lights that are being faded out, so they cull graciously
    /// </summary>
    public const int MaxFadeOutLights = 8;

    /// <summary>
    /// The absolute light limit
    /// </summary>
    public const int TotalLights = MaxLights + MaxFadeOutLights;

    /// <summary>
    /// Maximum lights (platform-specific)
    /// </summary>
    public int PlatformMaxLights = MaxLights;

    /// <summary>
    /// Maximum fade out lights (platform-specific)
    /// </summary>
    public int PlatformMaxFadeOutLights = MaxFadeOutLights;

    /// <summary>
    /// Whether to use fade out lights
    /// </summary>
    public bool UseFadeOutLights = true;

    /// <summary>
    /// The total lights we use
    /// </summary>
    public int PlatformTotalLights => PlatformMaxLights + (UseFadeOutLights ? PlatformMaxFadeOutLights : 0);

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

    /// <summary>
    /// How fast the light transition should be
    /// </summary>
    public static float LightTransitionSpeed = 3;

    private readonly SceneQuery<Light> lightQuery = new();

    private readonly Vector4[] cachedLightTypePositions = new Vector4[TotalLights];
    private readonly Color[] cachedLightDiffuseIntensity = new Color[TotalLights];
    private readonly Vector4[] cachedLightExtras = new Vector4[TotalLights];

    private readonly Dictionary<int, ShaderHandle[]> cachedInstancedMaterialInfo = [];

    private Material previousMaterial;
    private Shader previousShader;
    private Vector4 previousLightCount;
    private Color previousLightAmbient;
    private int visibleLightCount;
    private readonly Vector4[] previousLightTypePositions = new Vector4[TotalLights];
    private readonly Color[] previousLightDiffuseIntensity = new Color[TotalLights];
    private readonly Vector4[] previousLightExtras = new Vector4[TotalLights];

    private readonly ExpandableContainer<Light> cachedLights = new();

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

        foreach(var light in Lights)
        {
            switch(light.transition)
            {
                case CullingState.Visible:

                    light.transitionTimer += Time.deltaTime * LightTransitionSpeed;

                    if (light.transitionTimer >= 1)
                    {
                        light.transition = CullingState.None;
                    }

                    break;

                case CullingState.Invisible:

                    light.transitionTimer -= Time.deltaTime * LightTransitionSpeed;

                    if (light.transitionTimer <= 0)
                    {
                        light.transition = CullingState.None;
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// Finds all visible lights in a frustum. You should call this when applying lighting to multiple render steps within the same camera.
    /// </summary>
    /// <param name="frustum">The camera's frustum culler</param>
    /// <param name="cameraPosition">The position of the camera</param>
    /// <param name="layerMask">The layer mask of the camera</param>
    /// <returns>Whether we gathered data or not</returns>
    internal bool GatherVisibleLights(FrustumCuller frustum, Vector3 cameraPosition, LayerMask layerMask)
    {
        visibleLightCount = 0;

        if (!Enabled)
        {
            return false;
        }

        cachedLights.Clear();

        var targets = Lights;
        BoundingSphere sphere = default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool PointLightTest(Light light)
        {
            sphere.center = light.Transform.Position;
            sphere.radius = light.range;

            return frustum.SphereTest(sphere) != FrustumResult.Invisible;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool CullLight(Light light)
        {
            return light.type switch
            {
                LightType.Directional => true,
                LightType.Point => PointLightTest(light),
                _ => false,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int CompareLights(Light a, Light b)
        {
            //Both directional? same
            if (a.type == LightType.Directional && b.type == LightType.Directional)
            {
                return 0;
            }

            //A is directional? A is more important
            if (a.type == LightType.Directional && b.type != LightType.Directional)
            {
                return -1;
            }

            //Same for B
            if (b.type == LightType.Directional)
            {
                return 1;
            }

            //Otherwise, distance

            var aDistance = (a.Transform.Position - cameraPosition).LengthSquared();
            var bDistance = (b.Transform.Position - cameraPosition).LengthSquared();

            return aDistance.CompareTo(bDistance);
        }

        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];

            if (!target.Entity.IsValid ||
                !target.Enabled ||
                !layerMask.HasLayer(target.Entity.Layer) ||
                !CullLight(target))
            {
                continue;
            }

            cachedLights.Add(target);
        }

        if(cachedLights.Length == 0)
        {
            return false;
        }

        cachedLights.Contents.Sort(CompareLights);

        if (cachedLights.Length > PlatformMaxLights)
        {
            var culledLights = cachedLights.Contents.Slice(PlatformMaxLights);

            for (var i = 0; i < culledLights.Length; i++)
            {
                var light = culledLights[i];

                switch (light.culling)
                {
                    case CullingState.None:

                        light.culling = CullingState.Invisible;

                        break;

                    case CullingState.Visible:

                        light.culling = CullingState.Invisible;
                        light.transition = i < PlatformMaxFadeOutLights ? CullingState.Invisible : CullingState.None;
                        light.transitionTimer = 1;

                        break;
                }
            }

            cachedLights.Resize(PlatformTotalLights, true);
        }

        var visibleLights = cachedLights.Contents.Slice(0, Math.Min(PlatformMaxLights, cachedLights.Contents.Length));

        foreach (var light in visibleLights)
        {
            if (light == null || light.culling == CullingState.Visible)
            {
                continue;
            }

            light.culling = light.transition = CullingState.Visible;
            light.transitionTimer = 0;
        }

        foreach (var light in cachedLights.Contents)
        {
            if (light == null ||
                (light.culling == CullingState.Invisible && light.transition == CullingState.None))
            {
                continue;
            }

            var t = light.Transform;
            var p = t.Position;
            var forward = t.Forward;

            if (light.type == LightType.Directional)
            {
                p = -forward;
            }

            cachedLightTypePositions[visibleLightCount] = new((float)light.type, p.X, p.Y, p.Z);

            ref var diffuseIntensity = ref cachedLightDiffuseIntensity[visibleLightCount];

            var intensity = light.intensity;

            if (light.transition != CullingState.None)
            {
                intensity *= light.transitionTimer;
            }

            diffuseIntensity = light.color.WithAlpha(intensity);

            switch (light.type)
            {
                case LightType.Point:

                    ref var extras = ref cachedLightExtras[visibleLightCount];

                    extras.X = 1.0f / Math.Max(light.range * light.range, 0.00001f);

                    break;
            }

            visibleLightCount++;
        }

        return visibleLightCount > 0;
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

        var lightAmbient = AmbientColor;
        var lightCount = 0;

        if(!disableLighting)
        {
            foreach(var light in cachedLights.Contents)
            {
                if(light == null ||
                    (light.culling == CullingState.Invisible && light.transition == CullingState.None))
                {
                    continue;
                }

                var t = light.Transform;
                var p = t.Position;
                var forward = t.Forward;

                if (light.type == LightType.Directional)
                {
                    p = -forward;
                }

                cachedLightTypePositions[lightCount] = new((float)light.type, p.X, p.Y, p.Z);

                ref var diffuseIntensity = ref cachedLightDiffuseIntensity[lightCount];

                var intensity = light.intensity;

                if(light.transition != CullingState.None)
                {
                    intensity *= light.transitionTimer;
                }

                diffuseIntensity = light.color.WithAlpha(intensity);

                switch(light.type)
                {
                    case LightType.Point:

                        ref var extras = ref cachedLightExtras[lightCount];

                        extras.X = 1.0f / Math.Max(light.range * light.range, 0.00001f);

                        break;
                }

                lightCount++;
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

            for(var i = 0; i < TotalLights; i++)
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

    /// <summary>
    /// Sets the maximum amount of lights to use for lighting
    /// </summary>
    /// <param name="count">The maximum amount of lights</param>
    public void SetMaxLightCount(int count)
    {
        count = Math.ClampToInt(count, 0, MaxLights);

        PlatformMaxLights = count;
        PlatformMaxFadeOutLights = count / 4;

        if(PlatformMaxFadeOutLights == 1)
        {
            PlatformMaxFadeOutLights = 0;
        }
    }
}
