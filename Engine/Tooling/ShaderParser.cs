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

    public class ShaderPiece
    {
        public string content;
        public List<string> inputs = new();
        public List<string> outputs = new();
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

    [GeneratedRegex("(varying|uniform) (\\w+) (\\w+)( \\: (\\w+))?( \\= (.*))?")]
    private static partial Regex ParameterRegex();

    [GeneratedRegex("Begin Vertex((.|\\n)*)End Vertex")]
    private static partial Regex VertexRegex();

    [GeneratedRegex("Begin Fragment((.|\\n)*)End Fragment")]
    private static partial Regex FragmentRegex();

    [GeneratedRegex("Begin Compute((.|\\n)*)End Compute")]
    private static partial Regex ComputeRegex();

    [GeneratedRegex("Begin Parameters((.|\\n)*)End Parameters")]
    private static partial Regex ParametersRegex();

    [GeneratedRegex("Type (\\w+)")]
    private static partial Regex TypeRegex();

    [GeneratedRegex("\\$input (.*)")]
    private static partial Regex InputRegex();

    [GeneratedRegex("\\$output (.*)")]
    private static partial Regex OutputRegex();

    [GeneratedRegex("Blend (.*) (.*)")]
    private static partial Regex BlendRegex();

    public static bool Parse(string source, out ShaderType type, out (BlendMode, BlendMode)? blendMode, out Parameter[] parameters,
        out ShaderPiece vertex, out ShaderPiece fragment, out ShaderPiece compute)
    {
        var typeMatch = typeRegex.Match(source);

        if(typeMatch == null ||
            Enum.TryParse(typeMatch.Groups[1].Value.Trim(), true, out type) == false)
        {
            type = default;
            parameters = default;
            vertex = default;
            fragment = default;
            compute = default;
            blendMode = default;

            return false;
        }

        var blendMatch = blendRegex.Match(source);

        if(blendMatch == null ||
            Enum.TryParse<BlendMode>(blendMatch.Groups[1].Value, true, out var from) == false ||
            Enum.TryParse<BlendMode>(blendMatch.Groups[2].Value, true, out var to) == false)
        {
            blendMode = default;
        }
        else
        {
            blendMode = (from, to);
        }

        var parametersMatch = parametersRegex.Match(source);

        if (parametersMatch != null)
        {
            var content = parametersMatch.Groups[1].Value;

            var parameterMatches = parameterRegex.Matches(content);

            var parameterList = new List<Parameter>();

            foreach (Match match in parameterMatches)
            {
                var parameter = new Parameter
                {
                    type = match.Groups[1].Value.Trim(),
                    dataType = match.Groups[2].Value.Trim(),
                    name = match.Groups[3].Value.Trim(),
                    attribute = match.Groups[5].Value.Trim(),
                    initializer = match.Groups[7].Value.Trim()
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

            parameters = parameterList.ToArray();
        }
        else
        {
            parameters = default;
        }

        void HandleContent(Regex regex, out ShaderPiece piece)
        {
            var match = regex.Match(source);

            if (match != null)
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

                if(inputMatch != null && inputMatch.Length > 0)
                {
                    piece.inputs = inputMatch.Groups[1].Value.Split(",").Select(x => x.Trim()).ToList();

                    piece.content = piece.content.Replace($"{inputMatch.Value}\n", "");
                }

                var outputMatch = outputRegex.Match(piece.content);

                if (outputMatch != null && outputMatch.Length > 0)
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

        HandleContent(vertexRegex, out vertex);
        HandleContent(fragmentRegex, out fragment);
        HandleContent(computeRegex, out compute);

        return true;
    }
}
