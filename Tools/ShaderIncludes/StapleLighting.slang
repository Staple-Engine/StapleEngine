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
    Spot,
    Directional,
    Point,
};

public struct Light
{
    float4 positionType;
    float4 diffuseRaw;
    float4 specularRaw;

    LightSample Sample(float3 target)
    {
        LightSample sample;

        LightType type = (LightType)positionType.x;
        float3 position = positionType.yzw;
        float3 diffuse = diffuseRaw.rgb;
        float3 specular = specularRaw.rgb;

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

        default: //Defaults to directional

            sample.direction = normalize(position);
            sample.diffuse = diffuse;

            break;
        }

        return sample;
    }
};

[[vk::binding(2, StapleUniformBufferSet)]]
public cbuffer StapleLightInfo
{
    float4 StapleLightCountViewPosition;
    float4 StapleLightAmbientColor;
};

public int StapleLightCount()
{
    return int(StapleLightCountViewPosition.x);
}

public float3 StapleLightViewPosition()
{
    return StapleLightCountViewPosition.yzw;
}

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
    int count = StapleLightCount();

    if (count == 0)
    {
        return StapleLightAmbientColor.rgb;
    }

    float3 diffuse = float3(0, 0, 0);

    for (int i = 0; i < count; i++)
    {
        diffuse += StapleLightDiffuse(worldPosition, normal, StapleLights[i]);
    }

    return StapleLightAmbientColor.rgb + diffuse;
}

public float3 StapleProcessLightsTangent(float3 worldPosition, float3 normal, float3x3 tbn)
{
    int count = StapleLightCount();

    if (count == 0)
    {
        return StapleLightAmbientColor.rgb;
    }

    float3 diffuse;

    for (int i = 0; i < count; i++)
    {
        diffuse += StapleLightDiffuseTangent(worldPosition, normal, tbn, StapleLights[i]);
    }

    return StapleLightAmbientColor.rgb + diffuse;
}
#endif
