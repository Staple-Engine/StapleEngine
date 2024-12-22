using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Staple.Tooling;

public static partial class ShaderParser
{
    public class Parameter
    {
        public string type;
        public string dataType;
        public string name;
        public string attribute;
        public string initializer;
    }

    public class InstanceParameter
    {
        public string name;
        public string type;
    }

    public class ShaderPiece
    {
        public string content;
        public List<string> inputs = [];
        public List<string> outputs = [];
    }

    private static Regex typeRegex = TypeRegex();
    private static Regex parametersRegex = ParametersRegex();
    private static Regex vertexRegex = VertexRegex();
    private static Regex fragmentRegex = FragmentRegex();
    private static Regex computeRegex = ComputeRegex();
    private static Regex parameterRegex = ParameterRegex();
    private static Regex inputRegex = InputRegex();
    private static Regex outputRegex = OutputRegex();
    private static Regex blendRegex = BlendRegex();
    private static Regex variantsRegex = VariantsRegex();
    private static Regex bufferRegex = BufferRegex();
    private static Regex instancingRegex = InstancingRegex();
    private static Regex instancingParameterRegex = InstancingParameterRegex();

    [GeneratedRegex("(varying|uniform) (\\w+) (\\w+)(([ ]*)\\:([ ]*)(\\w+))?(([ ]*)\\=([ ]*)(.*))?")]
    private static partial Regex ParameterRegex();

    [GeneratedRegex("(ROBuffer|RWBuffer|WOBuffer)\\<(\\w+)\\>([ ]*)(\\w+)([ ]*)\\:([ ]*)([0-9]+)")]
    private static partial Regex BufferRegex();

    [GeneratedRegex("Begin Vertex((.|\\n)*)End Vertex")]
    private static partial Regex VertexRegex();

    [GeneratedRegex("Begin Fragment((.|\\n)*)End Fragment")]
    private static partial Regex FragmentRegex();

    [GeneratedRegex("Begin Compute((.|\\n)*)End Compute")]
    private static partial Regex ComputeRegex();

    [GeneratedRegex("Begin Parameters((.|\\n)*)End Parameters")]
    private static partial Regex ParametersRegex();

    [GeneratedRegex("Begin Instancing((.|\\n)*)End Instancing")]
    private static partial Regex InstancingRegex();

    [GeneratedRegex("(\\w+) (\\w+)")]
    private static partial Regex InstancingParameterRegex();

    [GeneratedRegex("Type (\\w+)")]
    private static partial Regex TypeRegex();

    [GeneratedRegex("\\$input (.*)")]
    private static partial Regex InputRegex();

    [GeneratedRegex("\\$output (.*)")]
    private static partial Regex OutputRegex();

    [GeneratedRegex("Blend (.*) (.*)")]
    private static partial Regex BlendRegex();

    [GeneratedRegex("Variants (.*)")]
    private static partial Regex VariantsRegex();

    public static bool Parse(string source, out ShaderType type, out (BlendMode, BlendMode)? blendMode, out Parameter[] parameters,
        out List<string> variants, out List<InstanceParameter> instanceParameters, out ShaderPiece vertex, out ShaderPiece fragment,
        out ShaderPiece compute)
    {
        var typeMatch = typeRegex.Match(source);

        if(typeMatch.Success == false ||
            Enum.TryParse(typeMatch.Groups[1].Value.Trim(), true, out type) == false)
        {
            type = default;
            parameters = default;
            variants = [];
            vertex = default;
            fragment = default;
            compute = default;
            blendMode = default;
            instanceParameters = default;

            return false;
        }

        if(type == ShaderType.VertexFragment)
        {
            var variantsMatch = variantsRegex.Match(source);

            if (variantsMatch.Success && variantsMatch.Length > 0)
            {
                variants = variantsMatch.Groups[1].Value.Split(",").Select(x => x.Trim()).ToList();
            }
            else
            {
                variants = [];
            }

            var blendMatch = blendRegex.Match(source);

            if (blendMatch.Success == false ||
                Enum.TryParse<BlendMode>(blendMatch.Groups[1].Value, true, out var from) == false ||
                Enum.TryParse<BlendMode>(blendMatch.Groups[2].Value, true, out var to) == false)
            {
                blendMode = default;
            }
            else
            {
                blendMode = (from, to);
            }
        }
        else
        {
            variants = [];
            blendMode = default;
        }

        var parametersMatch = parametersRegex.Match(source);

        if (parametersMatch.Success)
        {
            var content = parametersMatch.Groups[1].Value;

            var parameterMatches = parameterRegex.Matches(content);
            var bufferMatches = bufferRegex.Matches(content);

            var parameterList = new List<Parameter>();

            foreach (Match match in parameterMatches)
            {
                var parameter = new Parameter
                {
                    type = match.Groups[1].Value.Trim(),
                    dataType = match.Groups[2].Value.Trim(),
                    name = match.Groups[3].Value.Trim(),
                    attribute = match.Groups[7].Value.Trim(),
                    initializer = match.Groups[11].Value.Trim()
                };

                if (parameter.attribute.Length == 0)
                {
                    parameter.attribute = default;
                }

                if (parameter.initializer.Length == 0)
                {
                    parameter.initializer = default;
                }

                parameterList.Add(parameter);
            }

            foreach(Match match in bufferMatches)
            {
                if(int.TryParse(match.Groups[7].Value.Trim(), out var bufferIndex) == false)
                {
                    type = default;
                    parameters = default;
                    variants = [];
                    vertex = default;
                    fragment = default;
                    compute = default;
                    blendMode = default;
                    instanceParameters = default;

                    return false;
                }

                var parameter = new Parameter
                {
                    type = match.Groups[1].Value.Trim(),
                    dataType = match.Groups[2].Value.Trim(),
                    name = match.Groups[4].Value.Trim(),
                    initializer = bufferIndex.ToString(),
                };

                parameterList.Add(parameter);
            }

            parameters = parameterList.ToArray();
        }
        else
        {
            parameters = default;
        }

        void HandleContent(Regex regex, out ShaderPiece piece)
        {
            var match = regex.Match(source);

            if (match.Success)
            {
                var content = match.Groups[1].Value.Replace("\r", "").Trim();

                if (content.Length == 0)
                {
                    piece = default;

                    return;
                }

                piece = new()
                {
                    content = content,
                };

                var inputMatch = inputRegex.Match(piece.content);

                if(inputMatch.Success && inputMatch.Length > 0)
                {
                    piece.inputs = inputMatch.Groups[1].Value.Split(",").Select(x => x.Trim()).ToList();

                    piece.content = piece.content.Replace($"{inputMatch.Value}\n", "");
                }

                var outputMatch = outputRegex.Match(piece.content);

                if (outputMatch.Success && outputMatch.Length > 0)
                {
                    piece.outputs = outputMatch.Groups[1].Value.Split(",").Select(x => x.Trim()).ToList();

                    piece.content = piece.content.Replace($"{outputMatch.Value}\n", "");
                }

                piece.content = piece.content.Trim();
            }
            else
            {
                piece = default;
            }
        }

        if(type == ShaderType.VertexFragment)
        {
            HandleContent(vertexRegex, out vertex);
            HandleContent(fragmentRegex, out fragment);

            compute = default;

            var instanceParametersMatch = instancingRegex.Match(source);

            if (instanceParametersMatch.Success)
            {
                var content = instanceParametersMatch.Groups[1].Value;

                var parameterMatches = instancingParameterRegex.Matches(content);

                var parameterList = new List<InstanceParameter>();

                foreach (Match m in parameterMatches)
                {
                    var parameter = new InstanceParameter()
                    {
                        name = m.Groups[2].Value,
                        type = m.Groups[1].Value,
                    };

                    parameterList.Add(parameter);
                }

                instanceParameters = parameterList;
            }
            else
            {
                instanceParameters = default;
            }
        }
        else
        {
            HandleContent(computeRegex, out compute);

            vertex = default;
            fragment = default;
            instanceParameters = default;
        }

        return true;
    }
}
