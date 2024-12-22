using Staple.Internal;
using Staple.Tooling;

namespace StapleToolingTests;

public class ShaderParserTests
{
    [Test]
    public void TestParse()
    {
        var shader = $$"""
Type VertexFragment

Variants A, B, C

Begin Parameters
varying vec2 v_texcoord0 : TEXCOORD0 = vec2(0.0, 0.0)
varying vec2 v_texcoord1 : TEXCOORD1
uniform vec2 v_texcoord2
End Parameters

Begin Vertex
$input v_texcoord0
$output v_texcoord1, v_texcoord2
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

        Assert.IsTrue(ShaderParser.Parse(shader, out var type, out var blend, out var parameters, out var variants, out var instanceParameters,
            out var vertex, out var fragment, out var compute));

        Assert.That(type, Is.EqualTo(ShaderType.VertexFragment));

        Assert.That(variants.Count, Is.EqualTo(3));

        Assert.That(variants[0], Is.EqualTo("A"));
        Assert.That(variants[1], Is.EqualTo("B"));
        Assert.That(variants[2], Is.EqualTo("C"));
    
        Assert.That(blend, Is.Null);
        Assert.That(instanceParameters, Is.Null);

        Assert.That(parameters.Count, Is.EqualTo(3));

        Assert.That(parameters[0].type, Is.EqualTo("varying"));
        Assert.That(parameters[0].dataType, Is.EqualTo("vec2"));
        Assert.That(parameters[0].name, Is.EqualTo("v_texcoord0"));
        Assert.That(parameters[0].attribute, Is.EqualTo("TEXCOORD0"));
        Assert.That(parameters[0].initializer, Is.EqualTo("vec2(0.0, 0.0)"));

        Assert.That(parameters[1].type, Is.EqualTo("varying"));
        Assert.That(parameters[1].dataType, Is.EqualTo("vec2"));
        Assert.That(parameters[1].name, Is.EqualTo("v_texcoord1"));
        Assert.That(parameters[1].attribute, Is.EqualTo("TEXCOORD1"));
        Assert.That(parameters[1].initializer, Is.Null);

        Assert.That(parameters[2].type, Is.EqualTo("uniform"));
        Assert.That(parameters[2].dataType, Is.EqualTo("vec2"));
        Assert.That(parameters[2].name, Is.EqualTo("v_texcoord2"));
        Assert.That(parameters[2].attribute, Is.Null);
        Assert.That(parameters[2].initializer, Is.Null);

        Assert.That(vertex, Is.Not.Null);
        Assert.That(vertex.content, Is.EqualTo("vertex A\nvertex B"));

        Assert.That(vertex.inputs.Count, Is.EqualTo(1));
        Assert.That(vertex.inputs[0], Is.EqualTo("v_texcoord0"));

        Assert.That(vertex.outputs.Count, Is.EqualTo(2));
        Assert.That(vertex.outputs[0], Is.EqualTo("v_texcoord1"));
        Assert.That(vertex.outputs[1], Is.EqualTo("v_texcoord2"));

        Assert.That(fragment, Is.Not.Null);
        Assert.That(fragment.content, Is.EqualTo("fragment A\nfragment B"));

        Assert.That(compute, Is.Null);
    }

    [Test]
    public void TestParseWhitespaces()
    {
        var shader = $$"""
Type VertexFragment

Variants A, B, C

Begin Parameters
varying vec2 v_texcoord0 : TEXCOORD0 = vec2(0.0, 0.0)
varying vec2 v_texcoord1: TEXCOORD1
uniform vec2 v_texcoord2=vec2(1.0, 1.0)
End Parameters

Begin Instancing
float rotation
vec4 color
End Instancing

Begin Vertex
$input v_texcoord0
$output v_texcoord1, v_texcoord2
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

        Assert.IsTrue(ShaderParser.Parse(shader, out var type, out var blend, out var parameters, out var variants, out var instanceParameters,
            out var vertex, out var fragment, out var compute));

        Assert.That(type, Is.EqualTo(ShaderType.VertexFragment));

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
        Assert.That(instanceParameters[1].type, Is.EqualTo("vec4"));

        Assert.That(parameters.Count, Is.EqualTo(3));

        Assert.That(parameters[0].type, Is.EqualTo("varying"));
        Assert.That(parameters[0].dataType, Is.EqualTo("vec2"));
        Assert.That(parameters[0].name, Is.EqualTo("v_texcoord0"));
        Assert.That(parameters[0].attribute, Is.EqualTo("TEXCOORD0"));
        Assert.That(parameters[0].initializer, Is.EqualTo("vec2(0.0, 0.0)"));

        Assert.That(parameters[1].type, Is.EqualTo("varying"));
        Assert.That(parameters[1].dataType, Is.EqualTo("vec2"));
        Assert.That(parameters[1].name, Is.EqualTo("v_texcoord1"));
        Assert.That(parameters[1].attribute, Is.EqualTo("TEXCOORD1"));
        Assert.That(parameters[1].initializer, Is.Null);

        Assert.That(parameters[2].type, Is.EqualTo("uniform"));
        Assert.That(parameters[2].dataType, Is.EqualTo("vec2"));
        Assert.That(parameters[2].name, Is.EqualTo("v_texcoord2"));
        Assert.That(parameters[2].attribute, Is.Null);
        Assert.That(parameters[2].initializer, Is.EqualTo("vec2(1.0, 1.0)"));

        Assert.That(vertex, Is.Not.Null);
        Assert.That(vertex.content, Is.EqualTo("vertex A\nvertex B"));

        Assert.That(vertex.inputs.Count, Is.EqualTo(1));
        Assert.That(vertex.inputs[0], Is.EqualTo("v_texcoord0"));

        Assert.That(vertex.outputs.Count, Is.EqualTo(2));
        Assert.That(vertex.outputs[0], Is.EqualTo("v_texcoord1"));
        Assert.That(vertex.outputs[1], Is.EqualTo("v_texcoord2"));

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
varying vec2 v_texcoord0 : TEXCOORD0 = vec2(0.0, 0.0)
varying vec2 v_texcoord1 : TEXCOORD1
uniform vec2 v_texcoord2
ROBuffer<vec4> myBuffer:0
End Parameters

Begin Instancing
float rotation
vec4 color
End Instancing

Begin Vertex
$input v_texcoord0
$output v_texcoord1, v_texcoord2
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

        Assert.IsTrue(ShaderParser.Parse(shader, out var type, out var blend, out var parameters, out var variants, out var instanceParameters,
            out var vertex, out var fragment, out var compute));

        Assert.That(type, Is.EqualTo(ShaderType.Compute));

        Assert.That(variants.Count, Is.EqualTo(0));

        Assert.That(blend, Is.Null);

        Assert.That(instanceParameters, Is.Null);

        Assert.That(parameters.Count, Is.EqualTo(4));

        Assert.That(parameters[0].type, Is.EqualTo("varying"));
        Assert.That(parameters[0].dataType, Is.EqualTo("vec2"));
        Assert.That(parameters[0].name, Is.EqualTo("v_texcoord0"));
        Assert.That(parameters[0].attribute, Is.EqualTo("TEXCOORD0"));
        Assert.That(parameters[0].initializer, Is.EqualTo("vec2(0.0, 0.0)"));

        Assert.That(parameters[1].type, Is.EqualTo("varying"));
        Assert.That(parameters[1].dataType, Is.EqualTo("vec2"));
        Assert.That(parameters[1].name, Is.EqualTo("v_texcoord1"));
        Assert.That(parameters[1].attribute, Is.EqualTo("TEXCOORD1"));
        Assert.That(parameters[1].initializer, Is.Null);

        Assert.That(parameters[2].type, Is.EqualTo("uniform"));
        Assert.That(parameters[2].dataType, Is.EqualTo("vec2"));
        Assert.That(parameters[2].name, Is.EqualTo("v_texcoord2"));
        Assert.That(parameters[2].attribute, Is.Null);
        Assert.That(parameters[2].initializer, Is.Null);

        Assert.That(parameters[3].type, Is.EqualTo("ROBuffer"));
        Assert.That(parameters[3].dataType, Is.EqualTo("vec4"));
        Assert.That(parameters[3].name, Is.EqualTo("myBuffer"));
        Assert.That(parameters[3].attribute, Is.Null);
        Assert.That(parameters[3].initializer, Is.EqualTo("0"));

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

        Assert.IsTrue(ShaderParser.Parse(shader, out var type, out var blend, out var parameters, out var variants, out var instanceParameters,
            out var vertex, out var fragment, out var compute));

        Assert.That(type, Is.EqualTo(ShaderType.VertexFragment));

        Assert.That(blend, Is.Null);

        Assert.That(instanceParameters, Is.Not.Null);

        Assert.That(instanceParameters.Count, Is.EqualTo(0));
    }
}
