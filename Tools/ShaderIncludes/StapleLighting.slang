implementing Staple;

__include StapleInternal;

#define MAX_STAPLE_LIGHT_COUNT 128

public static const int MaxStapleLightCount = MAX_STAPLE_LIGHT_COUNT;

#ifdef LIT
public struct LightSample
{
    float3 diffuse;
    float3 direction;
    float attenuation;
};

public enum LightType
{
    Spot,
    Directional,
    Point,
};

public struct Light
{
    LightType type;
    float3 position;
    float3 diffuse;
    float3 specular;
    float4 extras;

    __init(LightType type, float3 position, float3 diffuse, float3 specular, float4 extras)
    {
        this.type = type;
        this.position = position;
        this.diffuse = diffuse;
        this.specular = specular;
        this.extras = extras;
    }

    LightSample Sample(float3 target)
    {
        LightSample sample;

        switch (type)
        {
        case LightType.Directional:

            sample.direction = normalize(position);
            sample.diffuse = diffuse;
            sample.attenuation = 1;

            break;

        case LightType.Point:

            float3 delta = position - target;

            float lengthSquare = max(dot(delta, delta), 0.00001);

            float range = extras.r;

            float rangeAttenuation = sqr(saturate(1.0 - sqr(lengthSquare * range)));

            sample.direction = normalize(delta);
            sample.diffuse = diffuse;
            sample.attenuation = rangeAttenuation / lengthSquare;

            break;

        default: //Defaults to directional

            sample.direction = normalize(position);
            sample.diffuse = diffuse;
            sample.attenuation = 1;

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

    return normalize(mul(float3x3(inverseMatrix), normal));
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

    float nDotL = saturate(StapleLightScaling(dot(normal, sample.direction) * sample.attenuation));

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
        float4 typePosition = StapleLightTypePosition[i];
        float4 diffuseIntensity = StapleLightDiffuseIntensity[i];
        float4 extras = StapleLightExtras[i];

        LightType lightType = (LightType)typePosition.x;
        float3 lightPosition = typePosition.yzw;
        float3 lightDiffuse = diffuseIntensity.rgb;
        float lightIntensity = diffuseIntensity.a;

        Light light = { lightType, lightPosition, lightDiffuse * lightIntensity, float3(0, 0, 0), extras };

        diffuse += StapleLightDiffuseFactor(worldPosition, normal, light);
    }

    return StapleLightAmbientColor.rgb + diffuse;
}
#endif
