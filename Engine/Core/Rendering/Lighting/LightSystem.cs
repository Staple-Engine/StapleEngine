using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Internal;

public class LightSystem : IRenderSystem, IWorldChangeReceiver
{
    public const int MaxLights = 16;

    private static readonly string LightAmbientKey = "u_lightAmbient";
    private static readonly string LightCountKey = "u_lightCount";
    private static readonly string LightDiffuseKey = "u_lightDiffuse";
    private static readonly string LightSpecularKey = "u_lightSpecular";
    private static readonly string LightTypePositionKey = "u_lightTypePosition";
    private static readonly string LightSpotDirectionKey = "u_lightSpotDirection";
    private static readonly string LightSpotValuesKey = "u_lightSpotValues";
    private static readonly string NormalMatrixKey = "u_normalMatrix";
    private static readonly string ViewPosKey = "u_viewPos";

    private readonly List<(Transform, Light)> lights = [];

    private readonly SceneQuery<Transform, Light> lightQuery = new();

    private readonly Vector4[] cachedLightTypePositions = new Vector4[MaxLights];
    private readonly Vector4[] cachedLightDiffuse = new Vector4[MaxLights];
    private readonly Vector4[] cachedLightSpotDirection = new Vector4[MaxLights];

    private readonly Dictionary<int, (ShaderHandle, ShaderHandle, ShaderHandle,
        ShaderHandle, ShaderHandle, ShaderHandle,
        ShaderHandle, ShaderHandle, ShaderHandle)> cachedMaterialInfo = [];

    public static bool Enabled = true;

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

    public void Preprocess(Entity entity, Transform transform, IComponent relatedComponent, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent, Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
    }

    public Type RelatedComponent()
    {
        return null;
    }

    public void Submit()
    {
    }

    public void ApplyMaterialLighting(Material material, MeshLighting lighting)
    {
        if(Enabled == false)
        {
            return;
        }

        switch (lighting)
        {
            case MeshLighting.Lit:

                material.EnableShaderKeyword(Shader.LitKeyword);

                if (material.metadata.enabledShaderVariants.Contains(Shader.HalfLambertKeyword) == false)
                {
                    material.DisableShaderKeyword(Shader.HalfLambertKeyword);
                }

                break;

            case MeshLighting.Unlit:

                if (material.metadata.enabledShaderVariants.Contains(Shader.LitKeyword) == false)
                {
                    material.DisableShaderKeyword(Shader.LitKeyword);
                }

                if (material.metadata.enabledShaderVariants.Contains(Shader.HalfLambertKeyword) == false)
                {
                    material.DisableShaderKeyword(Shader.HalfLambertKeyword);
                }

                break;

            case MeshLighting.HalfLambert:

                material.EnableShaderKeyword(Shader.LitKeyword);
                material.EnableShaderKeyword(Shader.HalfLambertKeyword);

                break;
        }
    }

    public void ApplyLightProperties(Vector3 position, Matrix4x4 transform, Material material, Vector3 cameraPosition, List<(Transform, Light)> lights)
    {
        if (Enabled == false ||
            (material?.IsValid ?? false) == false ||
            lights.Count == 0)
        {
            return;
        }

        var targets = lights;

        if (lights.Count > MaxLights)
        {
            targets = lights
                .OrderBy(x => Vector3.DistanceSquared(x.Item1.Position, position))
                .Take(MaxLights)
                .ToList();
        }

        Matrix4x4.Invert(transform, out var invTransform);

        var transTransform = Matrix4x4.Transpose(invTransform);

        var normalMatrix = transTransform.ToMatrix3x3();

        var lightAmbient = AppSettings.Current.ambientLight;
        var lightCount = new Vector4(targets.Count);

        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            var light = target.Item2;
            var t = target.Item1;
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

    public void ApplyLightProperties(Vector3 position, Matrix4x4 transform, Material material, Vector3 cameraPosition)
    {
        ApplyLightProperties(position, transform, material, cameraPosition, lights);
    }

    public void WorldChanged()
    {
        if(Enabled == false)
        {
            return;
        }

        lights.Clear();

        foreach (var pair in lightQuery)
        {
            lights.Add((pair.Item2, pair.Item3));
        }
    }
}
