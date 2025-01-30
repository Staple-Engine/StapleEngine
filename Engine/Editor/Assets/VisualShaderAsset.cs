using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Staple.Editor;

public class VisualShaderAsset : IGenerator
{
    private enum PairType
    {
        Type,
        Parameters,
        Instancing,
        VertexNodes,
        FragmentNodes,
        ComputeNodes,
    }

    private static readonly string Header = "//VisualShaderAsset ";

    public class Parameter
    {
        public bool varying;
        public ShaderUniformType uniformType;
        public string name;
        public string attribute;
    }

    public ShaderType shaderType;

    public readonly List<Parameter> parameters = [];

    public readonly List<Parameter> instancing = [];

    public string Extension => "shader";

    public bool IsText => true;

    public static bool Load(string guid, out IGenerator asset)
    {
        var content = ResourceManager.instance.LoadFileString(guid);

        if(content == null || content.StartsWith(Header) == false)
        {
            asset = default;

            return false;
        }

        var pieces = content.Split('\n').FirstOrDefault()?.Substring(Header.Length)?.Split('|');

        if(pieces == null)
        {
            asset = default;

            return false;
        }

        var instance = new VisualShaderAsset();

        foreach(var item in pieces)
        {
            PairType pairType;

            switch(item)
            {
                case string str when str.StartsWith("T:"):

                    pairType = PairType.Type;

                    break;

                case string str when str.StartsWith("P:"):

                    pairType = PairType.Parameters;

                    break;

                case string str when str.StartsWith("I:"):

                    pairType = PairType.Instancing;

                    break;

                case string str when str.StartsWith("VN:"):

                    pairType = PairType.VertexNodes;

                    break;

                case string str when str.StartsWith("FN:"):

                    pairType = PairType.FragmentNodes;

                    break;

                case string str when str.StartsWith("CN:"):

                    pairType = PairType.ComputeNodes;

                    break;

                default:

                    continue;
            }

            var itemContent = item[2..];

            switch(pairType)
            {
                case PairType.Type:

                    if(Enum.TryParse(itemContent, true, out instance.shaderType) == false)
                    {
                        asset = default;

                        return false;
                    }

                    break;

                case PairType.Parameters:

                    {
                        var parameters = itemContent.Split(';');

                        foreach (var p in parameters)
                        {
                            var parts = p.Split(':');

                            if (parts.Length < 3 ||
                                Staple.Tooling.Utilities.TryGetShaderUniformType(parts[1], out var uniformType) == false)
                            {
                                asset = default;

                                return false;
                            }

                            var attribute = parts.Length == 4 ? parts[3] : null;
                            var name = parts[2];
                            var varying = parts[0].Equals("varying", StringComparison.InvariantCultureIgnoreCase);

                            var param = new Parameter()
                            {
                                attribute = attribute,
                                name = name,
                                uniformType = uniformType,
                                varying = varying
                            };

                            instance.parameters.Add(param);
                        }
                    }

                    break;

                case PairType.Instancing:

                    if(itemContent.Length > 0)
                    {
                        var parameters = itemContent.Split(';');

                        foreach (var p in parameters)
                        {
                            var parts = p.Split(':');

                            if (parts.Length != 2 ||
                                Staple.Tooling.Utilities.TryGetShaderUniformType(parts[0], out var uniformType) == false)
                            {
                                asset = default;

                                return false;
                            }

                            var name = parts[1];

                            var param = new Parameter()
                            {
                                name = name,
                                uniformType = uniformType,
                                varying = false,
                            };

                            instance.instancing.Add(param);
                        }
                    }

                    break;

                case PairType.VertexNodes:

                    //TODO

                    break;

                case PairType.FragmentNodes:

                    //TODO

                    break;

                case PairType.ComputeNodes:

                    //TODO

                    break;
            }
        }

        asset = instance;

        return true;
    }

    public byte[] CreateNew()
    {
        return Encoding.UTF8.GetBytes($$"""
//VisualShaderAsset T:VertexFragment|P:varying:Vector3:a_position:POSITION;uniform:Color:mainColor|I:|VN:|FN:|CN:|
Type VertexFragment

Begin Parameters

varying vec3 a_position : POSITION
uniform color mainColor

End Parameters

Begin Instancing
End Instancing

Begin Vertex

$input a_position

void main()
{
	mat4 model = StapleModelMatrix;

	#ifdef SKINNING
	model = StapleGetSkinningMatrix(model, a_indices, a_weight);
	#endif

	mat4 projViewWorld = mul(mul(u_proj, u_view), model);

	vec4 v_pos = mul(projViewWorld, vec4(a_position, 1.0));

	gl_Position = v_pos;
}

End Vertex

Begin Fragment

void main()
{
	gl_FragColor = mainColor;
}

End Fragment

""");
    }

    public byte[] Generate()
    {
        var builder = new StringBuilder();

        builder.Append(Header);

        builder.Append("T:");

        builder.Append(shaderType.ToString());

        builder.Append('|');

        builder.Append("P:");

        for(var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            builder.Append(parameter.varying ? "varying" : "uniform");

            builder.Append(':');

            builder.Append(parameter.uniformType.ToString());

            builder.Append(':');

            builder.Append(parameter.name);

            if(parameter.attribute != null)
            {
                builder.Append(':');

                builder.Append(parameter.attribute);
            }

            if (i + 1 < parameters.Count)
            {
                builder.Append(';');
            }
        }

        builder.Append('|');

        builder.Append("I:");

        for (var i = 0; i < instancing.Count; i++)
        {
            var parameter = instancing[i];

            builder.Append(parameter.uniformType.ToString());

            builder.Append(':');

            builder.Append(parameter.name);

            if (i + 1 < instancing.Count)
            {
                builder.Append(';');
            }
        }

        if(shaderType == ShaderType.VertexFragment)
        {
            builder.Append('|');

            builder.Append("VN:");

            builder.Append('|');

            builder.Append("FN:");
        }
        else
        {
            builder.Append('|');

            builder.Append("CN:");
        }

        builder.AppendLine();

        return Encoding.UTF8.GetBytes(builder.ToString());
    }
}
