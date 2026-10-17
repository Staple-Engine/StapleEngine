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

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.variants.Count, Is.EqualTo(3));

        Assert.That(parsedData.variants[0], Is.EqualTo("A"));
        Assert.That(parsedData.variants[1], Is.EqualTo("B"));
        Assert.That(parsedData.variants[2], Is.EqualTo("C"));
    
        Assert.That(parsedData.blendMode, Has.Count.EqualTo(1));
        Assert.That(parsedData.instanceParameters, Is.Null);

        Assert.That(parsedData.parameters.Length, Is.EqualTo(3));

        Assert.That(parsedData.parameters[0].type, Is.Null);
        Assert.That(parsedData.parameters[0].dataType, Is.EqualTo("float2"));
        Assert.That(parsedData.parameters[0].name, Is.EqualTo("v_texcoord0"));
        Assert.That(parsedData.parameters[0].vertexAttribute, Is.EqualTo("TEXCOORD0"));
        Assert.That(parsedData.parameters[0].initializer, Is.EqualTo("float2(0.0, 0.0)"));

        Assert.That(parsedData.parameters[1].type, Is.Null);
        Assert.That(parsedData.parameters[1].dataType, Is.EqualTo("float2"));
        Assert.That(parsedData.parameters[1].name, Is.EqualTo("v_texcoord1"));
        Assert.That(parsedData.parameters[1].vertexAttribute, Is.EqualTo("TEXCOORD1"));
        Assert.That(parsedData.parameters[1].initializer, Is.Null);

        Assert.That(parsedData.parameters[2].type, Is.Null);
        Assert.That(parsedData.parameters[2].dataType, Is.EqualTo("float2"));
        Assert.That(parsedData.parameters[2].name, Is.EqualTo("v_texcoord2"));
        Assert.That(parsedData.parameters[2].vertexAttribute, Is.Null);
        Assert.That(parsedData.parameters[2].initializer, Is.Null);

        Assert.That(parsedData.vertex, Is.Not.Null);
        Assert.That(parsedData.vertex.content, Is.EqualTo("vertex A\nvertex B"));

        Assert.That(parsedData.fragment, Is.Not.Null);
        Assert.That(parsedData.fragment.content, Is.EqualTo("fragment A\nfragment B"));

        Assert.That(parsedData.compute, Is.Null);

        Assert.That(parsedData.renderQueue[""].Item1, Is.EqualTo(MaterialRenderQueue.Opaque));
        Assert.That(parsedData.renderQueue[""].Item2, Is.Zero);
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

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.variants.Length, Is.EqualTo(3));

        Assert.That(parsedData.variants[0], Is.EqualTo("A"));
        Assert.That(parsedData.variants[1], Is.EqualTo("B"));
        Assert.That(parsedData.variants[2], Is.EqualTo("C"));

        Assert.That(parsedData.blendMode, Has.Count.EqualTo(1));

        Assert.That(parsedData.instanceParameters, Is.Not.Null);

        Assert.That(parsedData.instanceParameters.Count, Is.EqualTo(2));

        Assert.That(parsedData.instanceParameters[0].name, Is.EqualTo("rotation"));
        Assert.That(parsedData.instanceParameters[0].type, Is.EqualTo("float"));

        Assert.That(parsedData.instanceParameters[1].name, Is.EqualTo("color"));
        Assert.That(parsedData.instanceParameters[1].type, Is.EqualTo("float4"));

        Assert.That(parsedData.parameters.Length, Is.EqualTo(3));

        Assert.That(parsedData.parameters[0].type, Is.Null);
        Assert.That(parsedData.parameters[0].dataType, Is.EqualTo("float2"));
        Assert.That(parsedData.parameters[0].name, Is.EqualTo("v_texcoord0"));
        Assert.That(parsedData.parameters[0].vertexAttribute, Is.EqualTo("TEXCOORD0"));
        Assert.That(parsedData.parameters[0].initializer, Is.EqualTo("float2(0.0, 0.0)"));

        Assert.That(parsedData.parameters[1].type, Is.Null);
        Assert.That(parsedData.parameters[1].dataType, Is.EqualTo("float2"));
        Assert.That(parsedData.parameters[1].name, Is.EqualTo("v_texcoord1"));
        Assert.That(parsedData.parameters[1].vertexAttribute, Is.EqualTo("TEXCOORD1"));
        Assert.That(parsedData.parameters[1].initializer, Is.Null);

        Assert.That(parsedData.parameters[2].type, Is.Null);
        Assert.That(parsedData.parameters[2].dataType, Is.EqualTo("float2"));
        Assert.That(parsedData.parameters[2].name, Is.EqualTo("v_texcoord2"));
        Assert.That(parsedData.parameters[2].vertexAttribute, Is.Null);
        Assert.That(parsedData.parameters[2].initializer, Is.EqualTo("float2(1.0, 1.0)"));

        Assert.That(parsedData.vertex, Is.Not.Null);
        Assert.That(parsedData.vertex.content, Is.EqualTo("vertex A\nvertex B"));

        Assert.That(parsedData.fragment, Is.Not.Null);
        Assert.That(parsedData.fragment.content, Is.EqualTo("fragment A\nfragment B"));

        Assert.That(parsedData.compute, Is.Null);
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

        Assert.That(ShaderParser.Parse(shader, ShaderType.Compute, out var parsedData), Is.True);

        Assert.That(parsedData.variants.Length, Is.EqualTo(0));

        Assert.That(parsedData.blendMode, Has.Count.EqualTo(0));

        Assert.That(parsedData.instanceParameters, Is.Null);

        Assert.That(parsedData.parameters.Length, Is.EqualTo(4));

        Assert.That(parsedData.parameters[0].type, Is.Null);
        Assert.That(parsedData.parameters[0].dataType, Is.EqualTo("float2"));
        Assert.That(parsedData.parameters[0].name, Is.EqualTo("v_texcoord0"));
        Assert.That(parsedData.parameters[0].vertexAttribute, Is.EqualTo("TEXCOORD0"));
        Assert.That(parsedData.parameters[0].initializer, Is.EqualTo("float2(0.0, 0.0)"));

        Assert.That(parsedData.parameters[1].type, Is.Null);
        Assert.That(parsedData.parameters[1].dataType, Is.EqualTo("float2"));
        Assert.That(parsedData.parameters[1].name, Is.EqualTo("v_texcoord1"));
        Assert.That(parsedData.parameters[1].vertexAttribute, Is.EqualTo("TEXCOORD1"));
        Assert.That(parsedData.parameters[1].initializer, Is.Null);

        Assert.That(parsedData.parameters[2].type, Is.Null);
        Assert.That(parsedData.parameters[2].dataType, Is.EqualTo("float2"));
        Assert.That(parsedData.parameters[2].name, Is.EqualTo("v_texcoord2"));
        Assert.That(parsedData.parameters[2].vertexAttribute, Is.Null);
        Assert.That(parsedData.parameters[2].initializer, Is.Null);

        Assert.That(parsedData.parameters[3].type, Is.EqualTo("ROBuffer"));
        Assert.That(parsedData.parameters[3].dataType, Is.EqualTo("float4"));
        Assert.That(parsedData.parameters[3].name, Is.EqualTo("myBuffer"));
        Assert.That(parsedData.parameters[3].vertexAttribute, Is.Null);
        Assert.That(parsedData.parameters[3].initializer, Is.Null);

        Assert.That(parsedData.vertex, Is.Null);
        Assert.That(parsedData.fragment, Is.Null);

        Assert.That(parsedData.compute, Is.Not.Null);
        Assert.That(parsedData.compute.content, Is.EqualTo("compute A\ncompute B"));
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

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.blendMode, Has.Count.EqualTo(1));

        Assert.That(parsedData.instanceParameters, Is.Not.Null);

        Assert.That(parsedData.instanceParameters.Count, Is.EqualTo(0));
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

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.parameters.Length, Is.EqualTo(1));

        Assert.That(parsedData.parameters[0].attribute, Is.EqualTo("Attribute"));

        Assert.That(parsedData.parameters[0].variant, Is.EqualTo("Variant"));
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

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.variantDependencies, Has.Length.EqualTo(3));

        Assert.That(parsedData.variantDependencies[0].Key, Is.EqualTo("B"));
        Assert.That(parsedData.variantDependencies[0].Value, Is.EqualTo("A"));

        Assert.That(parsedData.variantDependencies[1].Key, Is.EqualTo("D"));
        Assert.That(parsedData.variantDependencies[1].Value, Is.EqualTo("A"));

        Assert.That(parsedData.variantDependencies[2].Key, Is.EqualTo("D"));
        Assert.That(parsedData.variantDependencies[2].Value, Is.EqualTo("C"));
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
            "VERTEX_COLORS", "LIT", "HALF_LAMBERT", "PER_VERTEX_LIGHTING", "NORMALMAP",
        };

        var variantDependencies = new List<KeyValuePair<string, string>>()
        {
            new("HALF_LAMBERT", "LIT"),
            new("PER_VERTEX_LIGHTING", "LIT"),
            new("NORMALMAP", "LIT"),
        };

        var processedCombinations = ShaderParser.ProcessVariants(variants.Concat(Shader.DefaultVariants).ToList(), variantDependencies);

        Assert.That(processedCombinations, Has.Count.EqualTo(36));
    }

    [Test]
    public void TestProcessVariantsStandardShaderNegations()
    {
        var variants = new List<string>()
        {
            "VERTEX_COLORS", "LIT", "HALF_LAMBERT", "PER_VERTEX_LIGHTING", "NORMALMAP",
        };

        var variantDependencies = new List<KeyValuePair<string, string>>()
        {
            new("HALF_LAMBERT", "LIT"),
            new("PER_VERTEX_LIGHTING", "LIT"),
            new("NORMALMAP", "LIT"),
            new("NORMALMAP", "-PER_VERTEX_LIGHTING")
        };

        var processedCombinations = ShaderParser.ProcessVariants([.. variants, .. Shader.DefaultVariants], variantDependencies);

        Assert.That(processedCombinations, Has.Count.EqualTo(28));
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

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.renderQueue[""].Item1, Is.EqualTo(MaterialRenderQueue.Transparent));

        Assert.That(parsedData.renderQueue[""].Item2, Is.EqualTo(5));
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

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.renderQueue[""].Item1, Is.EqualTo(MaterialRenderQueue.Opaque));

        Assert.That(parsedData.renderQueue[""].Item2, Is.Zero);
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

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.renderQueue[""].Item1, Is.EqualTo(MaterialRenderQueue.Transparent));

        Assert.That(parsedData.renderQueue[""].Item2, Is.Zero);
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

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.renderQueue[""].Item1, Is.EqualTo(MaterialRenderQueue.Opaque));

        Assert.That(parsedData.renderQueue[""].Item2, Is.Zero);
    }

    [Test]
    public void TestParseNegativeVariantDependency()
    {
        var shader = $$"""
Type VertexFragment

Variants A B C

VariantDependency A B
VariantDependency C -B

Begin Parameters
End Parameters

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.variantDependencies, Has.Length.EqualTo(2));

        Assert.That(parsedData.variantDependencies[0].Key, Is.EqualTo("A"));
        Assert.That(parsedData.variantDependencies[0].Value, Is.EqualTo("B"));

        Assert.That(parsedData.variantDependencies[1].Key, Is.EqualTo("C"));
        Assert.That(parsedData.variantDependencies[1].Value, Is.EqualTo("-B"));

        Assert.That(parsedData.renderQueue[""].Item2, Is.Zero);
    }

    [Test]
    public void TestParseShaderDefines()
    {
        var shader = $$"""
Type VertexFragment

ShaderDefines LIT HALF_LAMBERT SKINNING

Variants A B C

VariantDependency A B
VariantDependency C -B

Begin Parameters
End Parameters

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.shaderDefines, Has.Length.EqualTo(3));

        Assert.That(parsedData.shaderDefines[0], Is.EqualTo("LIT"));
        Assert.That(parsedData.shaderDefines[1], Is.EqualTo("HALF_LAMBERT"));
        Assert.That(parsedData.shaderDefines[2], Is.EqualTo("SKINNING"));
    }

    [Test]
    public void TestParseZWriteDepthDefaults()
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

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.enableZWrite[""], Is.True);
        Assert.That(parsedData.depthTest[""], Is.EqualTo(DepthTest.LessEqual));
    }

    [Test]
    public void TestParseZWriteOff()
    {
        var shader = $$"""
Type VertexFragment

ZWrite Off

Begin Parameters
End Parameters

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.enableZWrite[""], Is.False);
        Assert.That(parsedData.depthTest[""], Is.EqualTo(DepthTest.LessEqual));
    }

    [Test]
    public void TestParseDepthAlways()
    {
        var shader = $$"""
Type VertexFragment

DepthTest Always

Begin Parameters
End Parameters

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.enableZWrite[""], Is.True);
        Assert.That(parsedData.depthTest[""], Is.EqualTo(DepthTest.Always));
    }

    [Test]
    public void TestParseDepth()
    {
        var shader = $$"""
Type VertexFragment

Depth Off

Begin Parameters
End Parameters

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.enableZWrite[""], Is.True);
        Assert.That(parsedData.enableDepth[""], Is.False);
    }

    [Test]
    public void TestParseBlendDepthZWriteVariants()
    {
        var shader = $$"""
Type VertexFragment

Variants A,B

variant: A ZWrite On
variant: B ZWrite Off

variant: A Depth On
variant: B Depth Off

variant: A DepthTest GreaterEqual
variant: B DepthTest Less

variant: A Blend SrcAlpha OneMinusSrcAlpha
variant: B Blend One One

Begin Parameters
End Parameters

Begin Instancing
End Instancing

Begin Vertex
End Vertex

Begin Fragment
End Fragment
""";

        Assert.That(ShaderParser.Parse(shader, ShaderType.VertexFragment, out var parsedData), Is.True);

        Assert.That(parsedData.enableZWrite[""], Is.True);
        Assert.That(parsedData.enableDepth[""], Is.True);
        Assert.That(parsedData.depthTest[""], Is.EqualTo(DepthTest.LessEqual));
        Assert.That(parsedData.blendMode[""], Is.EqualTo((BlendMode.Off, BlendMode.Off)));

        Assert.That(parsedData.enableZWrite["A"], Is.True);
        Assert.That(parsedData.enableDepth["A"], Is.True);
        Assert.That(parsedData.depthTest["A"], Is.EqualTo(DepthTest.GreaterEqual));
        Assert.That(parsedData.blendMode["A"], Is.EqualTo((BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha)));

        Assert.That(parsedData.enableZWrite["B"], Is.False);
        Assert.That(parsedData.enableDepth["B"], Is.False);
        Assert.That(parsedData.depthTest["B"], Is.EqualTo(DepthTest.Less));
        Assert.That(parsedData.blendMode["B"], Is.EqualTo((BlendMode.One, BlendMode.One)));
    }
}
