using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public string vertexAttribute;
        public string initializer;
        public string attribute;
        public string variant;
    }

    public class InstanceParameter
    {
        public string name;
        public string type;
    }

    public class ShaderPiece
    {
        public string content;
    }

    private static readonly Regex parametersRegex = ParametersRegex();
    private static readonly Regex vertexRegex = VertexRegex();
    private static readonly Regex fragmentRegex = FragmentRegex();
    private static readonly Regex computeRegex = ComputeRegex();
    private static readonly Regex commonRegex = CommonRegex();
    private static readonly Regex parameterRegex = ParameterRegex();
    private static readonly Regex vertexInputRegex = VertexInputRegex();
    private static readonly Regex vertexInputElementRegex = VertexInputElementRegex();
    private static readonly Regex blendRegex = BlendRegex();
    private static readonly Regex variantsRegex = VariantsRegex();
    private static readonly Regex variantDependencyRegex = VariantDependencyRegex();
    private static readonly Regex bufferRegex = BufferRegex();
    private static readonly Regex instancingRegex = InstancingRegex();
    private static readonly Regex instancingParameterRegex = InstancingParameterRegex();
    private static readonly Regex renderQueueRegex = RenderQueueRegex();

    [GeneratedRegex("(\\[\\w+\\] )?(variant\\: \\w+ )?([ ]*)(\\w+) (\\w+)(([ ]*)\\:([ ]*)(\\w+))?(([ ]*)\\=([ ]*)(.*))?")]
    private static partial Regex ParameterRegex();

    [GeneratedRegex("(ROBuffer|RWBuffer|WOBuffer)\\<(\\w+)\\>([ ]*)(\\w+)([ ]*)")]
    private static partial Regex BufferRegex();

    [GeneratedRegex("Begin Vertex((.|\\n)*)End Vertex")]
    private static partial Regex VertexRegex();

    [GeneratedRegex("Begin Fragment((.|\\n)*)End Fragment")]
    private static partial Regex FragmentRegex();

    [GeneratedRegex("Begin Compute((.|\\n)*)End Compute")]
    private static partial Regex ComputeRegex();

    [GeneratedRegex("Begin Common((.|\\n)*)End Common")]
    private static partial Regex CommonRegex();

    [GeneratedRegex("Begin Parameters((.|\\n)*)End Parameters")]
    private static partial Regex ParametersRegex();

    [GeneratedRegex("Begin Instancing((.|\\n)*)End Instancing")]
    private static partial Regex InstancingRegex();

    [GeneratedRegex("(\\w+) (\\w+)")]
    private static partial Regex InstancingParameterRegex();

    [GeneratedRegex("Begin Input((.|\\n)*)End Input")]
    private static partial Regex VertexInputRegex();

    [GeneratedRegex("(variant\\: (?:[^|\\s]+\\|)*[^|\\s]+)?([ ]*)(\\w+)")]
    private static partial Regex VertexInputElementRegex();

    [GeneratedRegex("Blend (.*) (.*)")]
    private static partial Regex BlendRegex();

    [GeneratedRegex("Variants (.*)")]
    private static partial Regex VariantsRegex();

    [GeneratedRegex("VariantDependency (\\w+) (\\w+)")]
    private static partial Regex VariantDependencyRegex();

    [GeneratedRegex("RenderQueue (.*) (.*)")]
    private static partial Regex RenderQueueRegex();

    public static bool Parse(string source, ShaderType type, out (BlendMode, BlendMode)? blendMode, out Parameter[] parameters,
        out List<string> variants, out List<KeyValuePair<string, string>> variantDependencies, out List<InstanceParameter> instanceParameters,
        out Dictionary<string, List<VertexAttribute>> vertexInputs, out MaterialRenderQueue renderQueue,
        out int renderQueueOffset, out ShaderPiece vertex, out ShaderPiece fragment, out ShaderPiece compute)
    {
        vertexInputs = [];

        if (type == ShaderType.VertexFragment)
        {
            var variantsMatch = variantsRegex.Match(source);

            if (variantsMatch.Success && variantsMatch.Length > 0)
            {
                variants = variantsMatch.Groups[1].Value.Split(",").Select(x => x.Trim()).ToList();

                variantDependencies = [];

                var variantDependencyMatches = variantDependencyRegex.Matches(source);

                foreach(Match dependency in variantDependencyMatches)
                {
                    if(dependency.Groups.Count == 3)
                    {
                        variantDependencies.Add(new(dependency.Groups[1].Value.Trim(), dependency.Groups[2].Value.Trim()));
                    }
                }
            }
            else
            {
                variants = [];
                variantDependencies = [];
            }

            var blendMatch = blendRegex.Match(source);

            if (!blendMatch.Success ||
                !Enum.TryParse<BlendMode>(blendMatch.Groups[1].Value, true, out var from) ||
                !Enum.TryParse<BlendMode>(blendMatch.Groups[2].Value, true, out var to))
            {
                blendMode = default;
            }
            else
            {
                blendMode = (from, to);
            }

            var renderQueueMatch = renderQueueRegex.Match(source);

            if (!renderQueueMatch.Success ||
                !Enum.TryParse(renderQueueMatch.Groups[1].Value, true, out renderQueue) ||
                !int.TryParse(renderQueueMatch.Groups[2].Value, CultureInfo.InvariantCulture, out renderQueueOffset))
            {
                renderQueue = default;
                renderQueueOffset = default;
            }
        }
        else
        {
            variants = [];
            variantDependencies = [];
            blendMode = default;
            renderQueue = default;
            renderQueueOffset = default;
        }

        if (renderQueueOffset < 0)
        {
            renderQueueOffset = 0;
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
                    dataType = match.Groups[4].Value.Trim(),
                    name = match.Groups[5].Value.Trim(),
                    vertexAttribute = match.Groups[9].Value.Trim(),
                    initializer = match.Groups[13].Value.Trim()
                };

                var attribute = match.Groups[1].Value.Trim();
                var variant = match.Groups[2].Value.Trim();

                if(!string.IsNullOrEmpty(attribute))
                {
                    parameter.attribute = attribute[1..^1];
                }

                if(!string.IsNullOrEmpty(variant))
                {
                    parameter.variant = variant["variant: ".Length..];
                }

                if (parameter.vertexAttribute.Length == 0)
                {
                    parameter.vertexAttribute = default;
                }

                if (parameter.initializer.Length == 0)
                {
                    parameter.initializer = default;
                }

                parameterList.Add(parameter);
            }

            foreach (Match match in bufferMatches)
            {
                var parameter = new Parameter
                {
                    type = match.Groups[1].Value.Trim(),
                    dataType = match.Groups[2].Value.Trim(),
                    name = match.Groups[4].Value.Trim(),
                };

                parameterList.Add(parameter);
            }

            parameters = parameterList.ToArray();
        }
        else
        {
            parameters = default;
        }

        var inputMatch = vertexInputRegex.Match(source);

        if (inputMatch.Success)
        {
            var inputs = inputMatch.Groups[1].Value.Trim().Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();

            for(var i = 0; i < inputs.Length; i++)
            {
                var match = vertexInputElementRegex.Match(inputs[i]);

                if(match.Success)
                {
                    if (!Enum.TryParse<VertexAttribute>(match.Groups[3].Value, true, out var attribute))
                    {
                        type = default;
                        parameters = default;
                        variants = [];
                        vertex = default;
                        fragment = default;
                        compute = default;
                        blendMode = default;
                        instanceParameters = default;
                        vertexInputs = default;

                        return false;
                    }

                    var keys = match.Groups[1].Value.Trim().Split('|');

                    foreach(var key in keys)
                    {
                        var target = key;

                        if(key.Length > 0)
                        {
                            if(key.StartsWith("variant: "))
                            {
                                target = target["variant: ".Length..];
                            }
                        }

                        vertexInputs ??= [];

                        if (!vertexInputs.TryGetValue(target, out var list))
                        {
                            list = [];

                            vertexInputs.Add(target, list);
                        }

                        list.Add(attribute);
                    }
                }
            }
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

                piece.content = piece.content.Trim();
            }
            else
            {
                piece = default;
            }
        }

        if(type == ShaderType.VertexFragment)
        {
            HandleContent(commonRegex, out var common);
            HandleContent(vertexRegex, out vertex);
            HandleContent(fragmentRegex, out fragment);

            if(common != null)
            {
                vertex?.content = $"{common.content}\n\n{vertex.content}";
                fragment?.content = $"{common.content}\n\n{fragment.content}";
            }

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
            HandleContent(commonRegex, out var common);
            HandleContent(computeRegex, out compute);

            if (common != null)
            {
                compute?.content = $"{common.content}\n\n{compute.content}";
            }

            vertex = default;
            fragment = default;
            instanceParameters = default;
        }

        return true;
    }

    public static List<List<string>> ProcessVariants(List<string> variants, List<KeyValuePair<string, string>> variantDependencies)
    {
        var combinations = Utilities.Combinations(variants);

        if(variantDependencies.Count == 0 || combinations.Count == 0)
        {
            return combinations;
        }

        for(var i = combinations.Count - 1; i >= 0; i--)
        {
            var combination = combinations[i];

            var found = false;

            foreach (var dependency in variantDependencies)
            {
                if(combination.Contains(dependency.Key) && !combination.Contains(dependency.Value))
                {
                    found = true;

                    break;
                }
            }

            if(found)
            {
                combinations.RemoveAt(i);
            }
        }

        return combinations;
    }
}
