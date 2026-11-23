module Staple;

__include StapleMath;
__include StapleInternal;
__include StapleLighting;
__include StapleSkinning;

public enum RenderQueue
{
    Opaque = 0,
    AlphaTest = 1000,
    Transparent = 2000,
    Overlay = 3000,
};

[[vk::binding(0, StapleUniformBufferSet)]]
public cbuffer StapleRenderData
{
    public float4x4 world;
    public float4x4 view;
    public float4x4 projection;
    public bool useWorldMatrix;
	public RenderQueue renderQueue;
    public float time;
    public float deltaTime;
    public int frame;
    public float2 viewportSize;
    public float3 cameraPosition;
    public float3 cameraDirection;
};

#ifdef STAPLE_VERTEX_SHADER
public float4x4 StapleWorldMatrix(uint baseInstance, uint instanceID)
{
    if (useWorldMatrix)
    {
        return world;
    }

    uint transformIndex = StapleEntityTransformIDs[baseInstance + instanceID];

    return StapleEntityTransforms[transformIndex];
}

public float4x4 ProjectionViewWorld(float4x4 world)
{
    return mul(projection, mul(view, world));
}

public float4x4 ViewWorld(float4x4 world)
{
    return mul(view, world);
}

public float4x4 ProjectionView()
{
    return mul(projection, view);
}
#endif

public float3 StapleWorldSpaceViewDirection(float3 vertex, float4x4 model)
{
    float3 transformed = mul(model, float4(vertex, 1.0)).xyz;

    return cameraPosition - transformed;
}

public float3 StapleObjectSpaceViewDirection(float3 vertex)
{
    return cameraPosition - vertex;
}
