implementing Staple;

__include StapleInternal;

#ifdef LIT
public struct LightSample
{
    public LightType type;
    public float3 diffuse;
    public float3 direction;
    public float attenuation;
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

    public LightSample Sample(float3 target)
    {
        LightSample sample;

        sample.type = type;

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
    return int(StapleLightData[0].x);
}

public int StapleMaxPixelLightCount()
{
    return int(StapleLightData[1].x);
}

public float3 StapleLightAmbient()
{
    return StapleLightData[1].yzw;
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

public float StapleRimLightingFactor(float3 viewDirection, float3 normal)
{
    return 1 - dot(viewDirection, normal);
}

public float3 StapleGetTangentNormal(float3 normal, float3 tangent, float3 bitangent, float2 uv, Sampler2D normalMap)
{
    float3x3 tbn = float3x3(normalize(tangent), normalize(bitangent), normalize(normal));

#ifdef STAPLE_VERTEX_SHADER
    float3 normalMapNormal = normalize(normalMap.SampleLevel(uv, 0).xyz * 2.0 - 1.0);
#else
    float3 normalMapNormal = normalize(normalMap.Sample(uv).xyz * 2.0 - 1.0);
#endif

    return normalize(mul(normalMapNormal, tbn));
}

public float3 StapleLightDiffuseFactor(float3 position, float3 normal, Light light)
{
    LightSample sample = light.Sample(position);

    float nDotL = saturate(StapleLightScaling(dot(normal, sample.direction) * sample.attenuation));

    return sample.diffuse * nDotL;
}

public Light StapleGetLight(int index)
{
    int count = StapleLightCount();

    int bufferIndex = 2 + index * 3;

    float4 typePosition = StapleLightData[bufferIndex];
    float4 diffuseIntensity = StapleLightData[bufferIndex + 1];
    float4 extras = StapleLightData[bufferIndex + 2];

    LightType lightType = (LightType)typePosition.x;
    float3 lightPosition = typePosition.yzw;
    float3 lightDiffuse = diffuseIntensity.rgb;
    float lightIntensity = diffuseIntensity.a;

    Light light = { lightType, lightPosition, lightDiffuse * lightIntensity, float3(0, 0, 0), extras };

    return light;
}

public float3 StapleProcessPixelLights(float3 worldPosition, float3 normal)
{
    int count = min(StapleLightCount(), StapleMaxPixelLightCount());

    float3 ambient = StapleLightAmbient();

    if (count == 0)
    {
        return ambient;
    }

    float3 diffuse = float3(0, 0, 0);

    for (int i = 0; i < count; i++)
    {
        Light light = StapleGetLight(i);

        diffuse += StapleLightDiffuseFactor(worldPosition, normal, light);
    }

    return ambient + diffuse;
}

public bool StapleShouldProcessVertexLights()
{
    int count = StapleLightCount();
    int maxPixelLights = StapleMaxPixelLightCount();

    return count > maxPixelLights;
}

public float3 StapleProcessVertexLights(float3 worldPosition, float3 normal)
{
    int count = StapleLightCount();
    int maxPixelLights = StapleMaxPixelLightCount();

    if (count <= maxPixelLights)
    {
        return float3(0, 0, 0);
    }

    float3 diffuse = float3(0, 0, 0);

    int start = maxPixelLights;

    for (int i = 0; i < count; i++)
    {
        Light light = StapleGetLight(i + start);

        diffuse += StapleLightDiffuseFactor(worldPosition, normal, light);
    }

    return diffuse;
}
#endif
