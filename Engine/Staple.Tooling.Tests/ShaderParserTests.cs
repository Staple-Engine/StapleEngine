using Staple;
using Staple.Internal;
using Staple.Tooling;

namespace StapleToolingTests;

public class ShaderParserTests
{
    [Test]
    public void TestParse()
    {
        var shader = $$"""
Variants A, B, C

Begin Parameters
float2 v_texcoord0 : TEXCOORD0 = float2(0.0, 0.0)
float2 v_texcoord1 : TEXCOORD1
float2 v_texcoord2
End Parameters

Begin Vertex
vertex A
vertex B
End Vertex

Begin Fragment
fragment A
fragment B
End Fragment

Begin Compute
compute A
compute B
End Compute
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var blend, out var parameters, out var variants,
            out var variantDependencies, out var instanceParameters, out var vertexAttributes, out var renderQueue, out var renderQueueOffset,
            out var vertex, out var fragment, out var compute), Is.True);

        Assert.That(variants.Count, Is.EqualTo(3));

        Assert.That(variants[0], Is.EqualTo("A"));
        Assert.That(variants[1], Is.EqualTo("B"));
        Assert.That(variants[2], Is.EqualTo("C"));
    
        Assert.That(blend, Is.Null);
        Assert.That(instanceParameters, Is.Null);

        Assert.That(parameters.Count, Is.EqualTo(3));

        Assert.That(parameters[0].type, Is.Null);
        Assert.That(parameters[0].dataType, Is.EqualTo("float2"));
        Assert.That(parameters[0].name, Is.EqualTo("v_texcoord0"));
        Assert.That(parameters[0].vertexAttribute, Is.EqualTo("TEXCOORD0"));
        Assert.That(parameters[0].initializer, Is.EqualTo("float2(0.0, 0.0)"));

        Assert.That(parameters[1].type, Is.Null);
        Assert.That(parameters[1].dataType, Is.EqualTo("float2"));
        Assert.That(parameters[1].name, Is.EqualTo("v_texcoord1"));
        Assert.That(parameters[1].vertexAttribute, Is.EqualTo("TEXCOORD1"));
        Assert.That(parameters[1].initializer, Is.Null);

        Assert.That(parameters[2].type, Is.Null);
        Assert.That(parameters[2].dataType, Is.EqualTo("float2"));
        Assert.That(parameters[2].name, Is.EqualTo("v_texcoord2"));
        Assert.That(parameters[2].vertexAttribute, Is.Null);
        Assert.That(parameters[2].initializer, Is.Null);

        Assert.That(vertex, Is.Not.Null);
        Assert.That(vertex.content, Is.EqualTo("vertex A\nvertex B"));

        Assert.That(fragment, Is.Not.Null);
        Assert.That(fragment.content, Is.EqualTo("fragment A\nfragment B"));

        Assert.That(compute, Is.Null);

        Assert.That(renderQueue, Is.EqualTo(MaterialRenderQueue.Opaque));
        Assert.That(renderQueueOffset, Is.Zero);
    }

    [Test]
    public void TestParseWhitespaces()
    {
        var shader = $$"""
Type VertexFragment

Variants A, B, C

Begin Parameters
float2 v_texcoord0 : TEXCOORD0 = float2(0.0, 0.0)
float2 v_texcoord1: TEXCOORD1
float2 v_texcoord2=float2(1.0, 1.0)
End Parameters

Begin Instancing
float rotation
float4 color
End Instancing

Begin Vertex
vertex A
vertex B
End Vertex

Begin Fragment
fragment A
fragment B
End Fragment

Begin Compute
compute A
compute B
End Compute
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var blend, out var parameters, out var variants,
            out var variantDependencies, out var instanceParameters, out var vertexAttributes, out var renderQueue, out var renderQueueOffset,
            out var vertex, out var fragment, out var compute), Is.True);

        Assert.That(variants.Count, Is.EqualTo(3));

        Assert.That(variants[0], Is.EqualTo("A"));
        Assert.That(variants[1], Is.EqualTo("B"));
        Assert.That(variants[2], Is.EqualTo("C"));

        Assert.That(blend, Is.Null);

        Assert.That(instanceParameters, Is.Not.Null);

        Assert.That(instanceParameters.Count, Is.EqualTo(2));

        Assert.That(instanceParameters[0].name, Is.EqualTo("rotation"));
        Assert.That(instanceParameters[0].type, Is.EqualTo("float"));

        Assert.That(instanceParameters[1].name, Is.EqualTo("color"));
        Assert.That(instanceParameters[1].type, Is.EqualTo("float4"));

        Assert.That(parameters.Count, Is.EqualTo(3));

        Assert.That(parameters[0].type, Is.Null);
        Assert.That(parameters[0].dataType, Is.EqualTo("float2"));
        Assert.That(parameters[0].name, Is.EqualTo("v_texcoord0"));
        Assert.That(parameters[0].vertexAttribute, Is.EqualTo("TEXCOORD0"));
        Assert.That(parameters[0].initializer, Is.EqualTo("float2(0.0, 0.0)"));

        Assert.That(parameters[1].type, Is.Null);
        Assert.That(parameters[1].dataType, Is.EqualTo("float2"));
        Assert.That(parameters[1].name, Is.EqualTo("v_texcoord1"));
        Assert.That(parameters[1].vertexAttribute, Is.EqualTo("TEXCOORD1"));
        Assert.That(parameters[1].initializer, Is.Null);

        Assert.That(parameters[2].type, Is.Null);
        Assert.That(parameters[2].dataType, Is.EqualTo("float2"));
        Assert.That(parameters[2].name, Is.EqualTo("v_texcoord2"));
        Assert.That(parameters[2].vertexAttribute, Is.Null);
        Assert.That(parameters[2].initializer, Is.EqualTo("float2(1.0, 1.0)"));

        Assert.That(vertex, Is.Not.Null);
        Assert.That(vertex.content, Is.EqualTo("vertex A\nvertex B"));

        Assert.That(fragment, Is.Not.Null);
        Assert.That(fragment.content, Is.EqualTo("fragment A\nfragment B"));

        Assert.That(compute, Is.Null);
    }

    [Test]
    public void TestParseBuffers()
    {
        var shader = $$"""
Type Compute

Variants A, B, C

Begin Parameters
float2 v_texcoord0 : TEXCOORD0 = float2(0.0, 0.0)
float2 v_texcoord1 : TEXCOORD1
float2 v_texcoord2
ROBuffer<float4> myBuffer
End Parameters

Begin Instancing
float rotation
float4 color
End Instancing

Begin Vertex
vertex A
vertex B
End Vertex

Begin Fragment
fragment A
fragment B
End Fragment

Begin Compute
compute A
compute B
End Compute
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.Compute, out var blend, out var parameters, out var variants,
            out var variantDependencies, out var instanceParameters, out var vertexAttributes, out var renderQueue, out var renderQueueOffset,
            out var vertex, out var fragment, out var compute), Is.True);

        Assert.That(variants.Count, Is.EqualTo(0));

        Assert.That(blend, Is.Null);

        Assert.That(instanceParameters, Is.Null);

        Assert.That(parameters.Count, Is.EqualTo(4));

        Assert.That(parameters[0].type, Is.Null);
        Assert.That(parameters[0].dataType, Is.EqualTo("float2"));
        Assert.That(parameters[0].name, Is.EqualTo("v_texcoord0"));
        Assert.That(parameters[0].vertexAttribute, Is.EqualTo("TEXCOORD0"));
        Assert.That(parameters[0].initializer, Is.EqualTo("float2(0.0, 0.0)"));

        Assert.That(parameters[1].type, Is.Null);
        Assert.That(parameters[1].dataType, Is.EqualTo("float2"));
        Assert.That(parameters[1].name, Is.EqualTo("v_texcoord1"));
        Assert.That(parameters[1].vertexAttribute, Is.EqualTo("TEXCOORD1"));
        Assert.That(parameters[1].initializer, Is.Null);

        Assert.That(parameters[2].type, Is.Null);
        Assert.That(parameters[2].dataType, Is.EqualTo("float2"));
        Assert.That(parameters[2].name, Is.EqualTo("v_texcoord2"));
        Assert.That(parameters[2].vertexAttribute, Is.Null);
        Assert.That(parameters[2].initializer, Is.Null);

        Assert.That(parameters[3].type, Is.EqualTo("ROBuffer"));
        Assert.That(parameters[3].dataType, Is.EqualTo("float4"));
        Assert.That(parameters[3].name, Is.EqualTo("myBuffer"));
        Assert.That(parameters[3].vertexAttribute, Is.Null);
        Assert.That(parameters[3].initializer, Is.Null);

        Assert.That(vertex, Is.Null);

        Assert.That(compute, Is.Not.Null);
        Assert.That(compute.content, Is.EqualTo("compute A\ncompute B"));
    }

    [Test]
    public void TestParseEmptyInstancing()
    {
        var shader = $$"""
Type VertexFragment

Begin Parameters
End Parameters

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var blend, out var parameters, out var variants,
            out var variantDependencies, out var instanceParameters, out var vertexAttributes, out var renderQueue, out var renderQueueOffset,
            out var vertex, out var fragment, out var compute), Is.True);

        Assert.That(blend, Is.Null);

        Assert.That(instanceParameters, Is.Not.Null);

        Assert.That(instanceParameters.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestParseAttributeVariant()
    {
        var shader = $$"""
Type VertexFragment

Begin Parameters
[Attribute] variant: Variant texture myTexture
End Parameters

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var blend, out var parameters, out var variants,
            out var variantDependencies, out var instanceParameters, out var vertexAttributes, out var renderQueue, out var renderQueueOffset,
            out var vertex, out var fragment, out var compute), Is.True);

        Assert.That(parameters.Length, Is.EqualTo(1));

        Assert.That(parameters[0].attribute, Is.EqualTo("Attribute"));

        Assert.That(parameters[0].variant, Is.EqualTo("Variant"));
    }

    [Test]
    public void TestParseVertexAttributes()
    {
        var shader = $$"""
Type VertexFragment

Begin Parameters
texture myTexture
End Parameters

Begin Input
POSITION
COLOR0
TEXCOORD0
variant: SKINNING|LIGHTING BLENDINDICES
variant: SKINNING|LIGHTING BLENDWEIGHTS
End Input

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var blend, out var parameters, out var variants,
            out var variantDependencies, out var instanceParameters, out var vertexAttributes, out var renderQueue, out var renderQueueOffset,
            out var vertex, out var fragment, out var compute), Is.True);

        Assert.That(vertexAttributes, Is.Not.Null);

        Assert.That(vertexAttributes.Count, Is.EqualTo(3));

        Assert.That(vertexAttributes.TryGetValue("", out var list), Is.True);

        Assert.That(list, Is.Not.Null);

        Assert.That(list, Has.Count.EqualTo(3));

        Assert.That(list[0], Is.EqualTo(VertexAttribute.Position));

        Assert.That(list[1], Is.EqualTo(VertexAttribute.Color0));

        Assert.That(list[2], Is.EqualTo(VertexAttribute.TexCoord0));

        Assert.That(vertexAttributes.TryGetValue("SKINNING", out list), Is.True);

        Assert.That(list, Is.Not.Null);

        Assert.That(list, Has.Count.EqualTo(2));

        Assert.That(list[0], Is.EqualTo(VertexAttribute.BlendIndices));

        Assert.That(list[1], Is.EqualTo(VertexAttribute.BlendWeights));

        Assert.That(vertexAttributes.TryGetValue("LIGHTING", out list), Is.True);

        Assert.That(list, Is.Not.Null);

        Assert.That(list, Has.Count.EqualTo(2));

        Assert.That(list[0], Is.EqualTo(VertexAttribute.BlendIndices));

        Assert.That(list[1], Is.EqualTo(VertexAttribute.BlendWeights));
    }

    [Test]
    public void TestFailParseVertexAttributes()
    {
        var shader = $$"""
Type VertexFragment

Begin Parameters
texture myTexture
End Parameters

Begin Input
POSITION
asdf
End Input

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var blend, out var parameters, out var variants,
            out var variantDependencies, out var instanceParameters, out var vertexAttributes, out var renderQueue, out var renderQueueOffset,
            out var vertex, out var fragment, out var compute), Is.True);
    }

    [Test]
    public void TestParseVariantDependencies()
    {
        var shader = $$"""
Type VertexFragment

Variants A B C D

VariantDependency B A
VariantDependency D A
VariantDependency D C

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var blend, out var parameters, out var variants,
            out var variantDependencies, out var instanceParameters, out var vertexAttributes, out var renderQueue, out var renderQueueOffset,
            out var vertex, out var fragment, out var compute), Is.True);

        Assert.That(variantDependencies, Has.Count.EqualTo(3));

        Assert.That(variantDependencies[0].Key, Is.EqualTo("B"));
        Assert.That(variantDependencies[0].Value, Is.EqualTo("A"));

        Assert.That(variantDependencies[1].Key, Is.EqualTo("D"));
        Assert.That(variantDependencies[1].Value, Is.EqualTo("A"));

        Assert.That(variantDependencies[2].Key, Is.EqualTo("D"));
        Assert.That(variantDependencies[2].Value, Is.EqualTo("C"));
    }

    [Test]
    public void TestProcessVariants()
    {
        var variants = new List<string>()
        {
            "A", "B", "C", "D",
        };

        var variantDependencies = new List<KeyValuePair<string, string>>()
        {
            new("C", "B"),
            new("D", "C"),
        };

        var combinations = Utilities.Combinations(variants);

        var processedCombinations = ShaderParser.ProcessVariants(variants, variantDependencies);

        foreach(var pair in processedCombinations)
        {
            if(pair.Count > 1)
            {
                if(pair.Contains("C"))
                {
                    Assert.That(pair.Contains("B"), Is.True);
                }

                if(pair.Contains("D"))
                {
                    Assert.That(pair.Contains("C"), Is.True);
                }
            }
        }

        Assert.That(processedCombinations, Has.Count.LessThan(combinations.Count));
    }

    [Test]
    public void TestProcessVariantsStandardShader()
    {
        var variants = new List<string>()
        {
            "VERTEX_COLORS", "LIT", "HALF_LAMBERT", "PER_VERTEX_LIGHTING", "NORMALMAP", "CUTOUT",
        };

        var variantDependencies = new List<KeyValuePair<string, string>>()
        {
            new("HALF_LAMBERT", "LIT"),
            new("PER_VERTEX_LIGHTING", "LIT"),
            new("NORMALMAP", "LIT"),
        };

        var processedCombinations = ShaderParser.ProcessVariants(variants.Concat(Shader.DefaultVariants).ToList(), variantDependencies);

        Assert.That(processedCombinations, Has.Count.EqualTo(75));
    }

    [Test]
    public void TestParseRenderQueue()
    {
        var shader = $$"""
Type VertexFragment

RenderQueue Transparent 5

Begin Parameters
texture myTexture
End Parameters

Begin Input
POSITION
asdf
End Input

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var blend, out var parameters, out var variants,
            out var variantDependencies, out var instanceParameters, out var vertexAttributes, out var renderQueue, out var renderQueueOffset,
            out var vertex, out var fragment, out var compute), Is.True);

        Assert.That(renderQueue, Is.EqualTo(MaterialRenderQueue.Transparent));

        Assert.That(renderQueueOffset, Is.EqualTo(5));
    }

    [Test]
    public void TestParseInvalidRenderQueue()
    {
        var shader = $$"""
Type VertexFragment

RenderQueue whatever 5

Begin Parameters
texture myTexture
End Parameters

Begin Input
POSITION
asdf
End Input

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var blend, out var parameters, out var variants,
            out var variantDependencies, out var instanceParameters, out var vertexAttributes, out var renderQueue, out var renderQueueOffset,
            out var vertex, out var fragment, out var compute), Is.True);

        Assert.That(renderQueue, Is.EqualTo(MaterialRenderQueue.Opaque));

        Assert.That(renderQueueOffset, Is.EqualTo(5));
    }

    [Test]
    public void TestParseInvalidRenderQueue2()
    {
        var shader = $$"""
Type VertexFragment

RenderQueue Transparent whatever

Begin Parameters
texture myTexture
End Parameters

Begin Input
POSITION
asdf
End Input

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var blend, out var parameters, out var variants,
            out var variantDependencies, out var instanceParameters, out var vertexAttributes, out var renderQueue, out var renderQueueOffset,
            out var vertex, out var fragment, out var compute), Is.True);

        Assert.That(renderQueue, Is.EqualTo(MaterialRenderQueue.Opaque));

        Assert.That(renderQueueOffset, Is.Zero);
    }

    [Test]
    public void TestParseInvalidRenderQueue3()
    {
        var shader = $$"""
Type VertexFragment

RenderQueue Transparent

Begin Parameters
texture myTexture
End Parameters

Begin Input
POSITION
asdf
End Input

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var blend, out var parameters, out var variants,
            out var variantDependencies, out var instanceParameters, out var vertexAttributes, out var renderQueue, out var renderQueueOffset,
            out var vertex, out var fragment, out var compute), Is.True);

        Assert.That(renderQueue, Is.EqualTo(MaterialRenderQueue.Opaque));

        Assert.That(renderQueueOffset, Is.Zero);
    }
}
