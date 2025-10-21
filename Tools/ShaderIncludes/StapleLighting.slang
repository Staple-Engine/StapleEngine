implementing Staple;

#define STAPLE_MAX_LIGHTS 16

public struct LightSample
{
    float3 diffuse;
    float3 direction;
};

public interface ILight
{
    LightSample Sample(float3 target);
}

public struct PointLight : ILight
{
    float3 position;
    float3 diffuse;

    LightSample Sample(float3 target)
    {
        float3 delta = target - position;
        float distance = length(delta);

        LightSample sample;

        sample.direction = normalize(delta);
        sample.diffuse = diffuse;

        return sample;
    }
};

public struct DirectionalLight : ILight
{
    float3 direction;
    float3 diffuse;

    LightSample Sample(float3 target)
    {
        LightSample sample;

        sample.direction = normalize(direction);
        sample.diffuse = diffuse;

        return sample;
    }
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

public float4 StapleLightDiffuse<L: ILight>(float3 position, float3 normal, L light)
{
    LightSample sample = light.Sample(position);

    float nDotL = max(0, StapleLightScaling(dot(normal, sample.direction)));

    return float4(sample.diffuse * nDotL, 1);
}

public float4 StapleProcessLights<L: ILight>(float3 viewPosition, float3 worldPosition, float3 normal, float4 ambient,
    float4 albedo, ILight[] lights)
{
    if(lights.getCount() == 0)
    {
        return ambient;
    }

    float4 diffuse;

    for(int i = 0; i < lights.getCount(); i++)
    {
        diffuse += StapleLightDiffuse(worldPosition, normal, lights[i]);
    }

    return ambient + diffuse;
}
