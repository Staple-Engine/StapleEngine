using Staple;
using Staple.Internal;
using Staple.Tooling;

namespace StapleToolingTests;

public class MeshImporterTests
{
    [Test]
    public void TestDefaultValues()
    {
        var shader = $$"""
Begin Parameters
color diffuseColor = #FFFFFFFF
float alphaThreshold = 0.25
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
            out var instanceParameters, out var vertexAttributes, out var vertex, out var fragment, out var compute), Is.True);

        Assert.That(blend, Is.Null);
        Assert.That(instanceParameters, Is.Null);

        Assert.That(parameters.Count, Is.EqualTo(2));

        Assert.That(parameters[0].type, Is.Null);
        Assert.That(parameters[0].dataType, Is.EqualTo("color"));
        Assert.That(parameters[0].name, Is.EqualTo("diffuseColor"));
        Assert.That(parameters[0].initializer, Is.EqualTo("#FFFFFFFF"));

        Assert.That(parameters[1].type, Is.Null);
        Assert.That(parameters[1].dataType, Is.EqualTo("float"));
        Assert.That(parameters[1].name, Is.EqualTo("alphaThreshold"));
        Assert.That(parameters[1].initializer, Is.EqualTo("0.25"));

        var serializedShader = new SerializableShader()
        {
            metadata = new()
            {
                uniforms =
                    [
                        new()
                        {
                            name = parameters[0].name,
                            defaultValue = parameters[0].initializer,
                            type = ShaderUniformType.Color,
                        },
                        new()
                        {
                            name = parameters[1].name,
                            defaultValue = parameters[1].initializer,
                            type = ShaderUniformType.Float,
                        }
                    ],
            }
        };

        var materialMetadata = new MaterialMetadata();

        MeshImporterContext.FillMaterialParameters(materialMetadata, serializedShader);

        Assert.That(materialMetadata.parameters.Count, Is.EqualTo(2));

        Assert.That(materialMetadata.parameters["diffuseColor"].type, Is.EqualTo(MaterialParameterType.Color));
        Assert.That(materialMetadata.parameters["diffuseColor"].colorValue, Is.EqualTo(Color32.White));

        Assert.That(materialMetadata.parameters["alphaThreshold"].type, Is.EqualTo(MaterialParameterType.Float));
        Assert.That(materialMetadata.parameters["alphaThreshold"].floatValue, Is.EqualTo(0.25f));
    }
}
