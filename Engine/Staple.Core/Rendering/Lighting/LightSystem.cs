using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple.Internal;

/// <summary>
/// Light rendering system
/// </summary>
public sealed class LightSystem
{
    /// <summary>
    /// Normal light count
    /// </summary>
    public const int MaxLights = 32;

    /// <summary>
    /// Maximum lights (platform-specific)
    /// </summary>
    public int PlatformMaxLights = MaxLights;

    /// <summary>
    /// The total lights we use
    /// </summary>
    public int PlatformTotalLights => PlatformMaxLights;

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

    private readonly ExpandableContainer<Vector4> lightData = new(false);

    private readonly Lazy<VertexLayout> lightBufferLayout = new(() =>
    {
        return VertexLayoutBuilder.CreateNew()
            .Add(VertexAttribute.Position, VertexAttributeType.Float4)
            .Build();
    });

    private VertexBuffer lightBuffer;

    private VertexBuffer emptyBuffer;

    private Material previousMaterial;
    private Shader previousShader;
    private int visibleLightCount;

    private readonly ExpandableContainer<Light> cachedLights = new(false);

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
        lightData.Clear();

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

        lightData.Add(new(cachedLights.Length, cameraPosition.X, cameraPosition.Y, cameraPosition.Z));
        lightData.Add(new(PlatformMaxLights, AmbientColor.r, AmbientColor.g, AmbientColor.b));

        foreach (var light in cachedLights.Contents)
        {
            var t = light.Transform;
            var p = t.Position;
            var forward = t.Forward;

            if (light.type == LightType.Directional)
            {
                p = -forward;
            }

            lightData.Add(new((float)light.type, p.X, p.Y, p.Z));

            var intensity = light.intensity;

            lightData.Add(light.color.WithAlpha(intensity));

            var extras = Vector4.Zero;

            switch (light.type)
            {
                case LightType.Point:

                    extras.X = 1.0f / Math.Max(light.range * light.range, 0.00001f);

                    break;
            }

            lightData.Add(extras);
        }

        if(lightBuffer?.Disposed ?? true)
        {
            lightBuffer = VertexBuffer.Create(lightData.Contents, lightBufferLayout.Value, RenderBufferFlags.GraphicsRead);
        }
        else
        {
            lightBuffer.Update(lightData.Contents);
        }

        if(emptyBuffer?.Disposed ?? true)
        {
            emptyBuffer = VertexBuffer.Create([new Vector4(0, 1, 1, 1)], lightBufferLayout.Value, RenderBufferFlags.GraphicsRead);
        }

        return cachedLights.Length > 0;
    }

    /// <summary>
    /// Applies light properties to the next render pass
    /// </summary>
    /// <param name="material">The material to use</param>
    /// <param name="cameraPosition">The position of the camera</param>
    /// <param name="disableLighting">Whether lighting should be disabled</param>
    internal bool ApplyLightProperties(ref RenderState state)
    {
        if (!Enabled)
        {
            return false;
        }

        state.ApplyStorageBufferIfNeeded("StapleLightData", (lightBuffer?.Disposed ?? true) ? emptyBuffer : lightBuffer);

        return true;
    }

    /// <summary>
    /// Sets the maximum amount of lights to use for lighting
    /// </summary>
    /// <param name="count">The maximum amount of lights</param>
    public void SetMaxLightCount(int count)
    {
        count = Math.ClampToInt(count, 0, 1000);

        PlatformMaxLights = count;
    }
}
