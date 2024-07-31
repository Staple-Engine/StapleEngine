using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Internal;

public class LightSystem : IRenderSystem
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
        lights.Clear();
    }

    public void Preprocess(Entity entity, Transform transform, IComponent relatedComponent, Camera activeCamera, Transform activeCameraTransform)
    {
        if(relatedComponent is Light light)
        {
            lights.Add((transform, light));
        }
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent, Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
    }

    public Type RelatedComponent()
    {
        return typeof(Light);
    }

    public void Submit()
    {
    }

    public void ApplyLightProperties(Matrix4x4 transform, Material material, Vector3 cameraPosition)
    {
        if ((material?.IsValid ?? false) == false)
        {
            return;
        }

        var targets = lights;

        Matrix4x4.Decompose(transform, out _, out _, out var position);

        if (lights.Count > MaxLights)
        {
            targets = lights
                .OrderBy(x => Vector3.DistanceSquared(x.Item1.Position, position))
                .Take(MaxLights)
                .ToList();

            targets = lights.Take(MaxLights).ToList();
        }

        Matrix4x4.Invert(transform, out var invTransform);

        var transTransform = Matrix4x4.Transpose(invTransform);

        var normalMatrix = transTransform.ToMatrix3x3();

        var lightAmbient = AppSettings.Current.ambientLight;
        var lightCount = new Vector4(targets.Count);
        var lightTypePositions = new Vector4[targets.Count];
        var lightDiffuse = new Vector4[targets.Count];
        var lightSpotDirection = new Vector4[targets.Count];

        lightCount.X = targets.Count;

        for(var i = 0; i < targets.Count; i++)
        {
            lightTypePositions[i] = new((float)targets[i].Item2.type,
                targets[i].Item1.Position.X,
                targets[i].Item1.Position.Y,
                targets[i].Item1.Position.Z);

            var forward = targets[i].Item1.Forward;

            if (targets[i].Item2.type == LightType.Directional)
            {
                (lightTypePositions[i].Y, lightTypePositions[i].Z, lightTypePositions[i].W) = (-forward.X, -forward.Y, -forward.Z);
            }

            lightDiffuse[i] = targets[i].Item2.color;

            lightSpotDirection[i] = forward.ToVector4();
        }

        material.shader.SetVector3(ViewPosKey, cameraPosition);
        material.shader.SetMatrix3x3(NormalMatrixKey, normalMatrix);
        material.shader.SetColor(LightAmbientKey, lightAmbient);
        material.shader.SetVector4(LightCountKey, lightCount);
        material.shader.SetVector4(LightTypePositionKey, lightTypePositions);
        material.shader.SetVector4(LightDiffuseKey, lightDiffuse);
        material.shader.SetVector4(LightSpotDirectionKey, lightSpotDirection);
    }
}
