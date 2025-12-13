implementing Staple;

__include StapleInternal;

#ifdef LIT
public struct LightSample
{
    float3 diffuse;
    float3 direction;
};

public enum LightType
{
    Directional,
    Point,
    Spotlight,
};

public struct Light
{
    LightType type;
    float3 position;
    float3 diffuse;
    float3 specular;
    float2 padding;

    LightSample Sample(float3 target)
    {
        LightSample sample;

        switch (type)
        {
        case LightType.Directional:

            sample.direction = normalize(position);
            sample.diffuse = diffuse;

            break;

        case LightType.Point:

            float3 delta = target - position;
            float distance = length(delta);

            sample.direction = normalize(delta);
            sample.diffuse = diffuse;

            break;

        default:

            break;
        }

        return sample;
    }
};

[[vk::binding(2, StapleUniformBufferSet)]]
public cbuffer StapleLightInfo
{
    float3 StapleLightViewPosition;
    float4 StapleLightAmbientColor;
    int StapleLightCount;
};

public float3 StapleLightNormal(float3 normal, float4x4 modelView)
{
    float4x4 inverseMatrix = transpose(inverse(modelView));

    float3x3 normalMatrix = float3x3(inverseMatrix[0].xyz,
        inverseMatrix[1].xyz,
        inverseMatrix[2].xyz);

    return normalize(mul(normalMatrix, normal));
}

public float StapleLightScaling(float nDotL)
{
#ifdef HALF_LAMBERT
    return pow(nDotL * 0.5 + 0.5, 2);
#else
    return nDotL;
#endif
}

public float3 StapleLightDiffuse(float3 position, float3 normal, Light light)
{
    LightSample sample = light.Sample(position);

    float nDotL = max(0, StapleLightScaling(dot(normal, sample.direction)));

    return sample.diffuse * nDotL;
}

public float3 StapleLightDiffuseTangent(float3 position, float3 normal, float3x3 tbn, Light light)
{
    LightSample sample = light.Sample(position);

    float3 direction = normalize(mul(sample.direction, tbn));

    float nDotL = max(0, StapleLightScaling(dot(normal, direction)));

    return sample.diffuse * nDotL;
}

public float3 StapleProcessLights(float3 worldPosition, float3 normal)
{
    if (StapleLightCount == 0)
    {
        return StapleLightAmbientColor.rgb;
    }

    float3 diffuse;

    for (int i = 0; i < StapleLightCount; i++)
    {
        diffuse += StapleLightDiffuse(worldPosition, normal, StapleLights[i]);
    }

    return StapleLightAmbientColor.rgb + diffuse;
}

public float3 StapleProcessLightsTangent(float3 worldPosition, float3 normal, float3x3 tbn)
{
    if (StapleLightCount == 0)
    {
        return StapleLightAmbientColor.rgb;
    }

    float3 diffuse;

    for (int i = 0; i < StapleLightCount; i++)
    {
        diffuse += StapleLightDiffuseTangent(worldPosition, normal, tbn, StapleLights[i]);
    }

    return StapleLightAmbientColor.rgb + diffuse;
}
#endif
