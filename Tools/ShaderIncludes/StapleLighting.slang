implementing Staple;

__include StapleInternal;

#define MAX_STAPLE_LIGHT_COUNT 16

public static const int MaxStapleLightCount = MAX_STAPLE_LIGHT_COUNT;

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

    return normalize(mul(normal, float3x3(inverseMatrix)));
}

public float StapleLightScaling(float nDotL)
{
#ifdef HALF_LAMBERT
    return pow(nDotL * 0.5 + 0.5, 2);
#else
    return nDotL;
#endif
}

public float3 StapleGetTangentNormal(float3 normal, float3 tangent, float3 bitangent, float2 uv, Sampler2D normalMap)
{
    float3x3 tbn = float3x3(normalize(tangent), normalize(bitangent), normalize(normal));

    float3 normalMapNormal = normalize(normalMap.Sample(uv).xyz * 2.0 - 1.0);

    return normalize(mul(normalMapNormal, tbn));
}

public float3 StapleLightDiffuseFactor(float3 position, float3 normal, Light light)
{
    LightSample sample = light.Sample(position);

    float nDotL = max(0, StapleLightScaling(dot(normal, sample.direction)));

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
        Light light;

        light.positionType = StapleLightTypePosition[i];
        light.diffuseRaw = StapleLightDiffuse[i];
        light.specularRaw = float4(0, 0, 0, 0); // TODO

        diffuse += StapleLightDiffuseFactor(worldPosition, normal, light);
    }

    return StapleLightAmbientColor.rgb + diffuse;
}
#endif
