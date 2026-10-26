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
    private static readonly Regex blendRegex = BlendRegex();
    private static readonly Regex variantsRegex = VariantsRegex();
    private static readonly Regex variantDependencyRegex = VariantDependencyRegex();
    private static readonly Regex shaderDefinesRegex = ShaderDefinesRegex();
    private static readonly Regex bufferRegex = BufferRegex();
    private static readonly Regex instancingRegex = InstancingRegex();
    private static readonly Regex instancingParameterRegex = InstancingParameterRegex();
    private static readonly Regex renderQueueRegex = RenderQueueRegex();
    private static readonly Regex enableZWriteRegex = ZWriteRegex();
    private static readonly Regex enableDepthRegex = DepthRegex();
    private static readonly Regex depthTestRegex = DepthTestRegex();

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

    [GeneratedRegex("(variant\\: \\w+ )?Blend (.*) (.*)")]
    private static partial Regex BlendRegex();

    [GeneratedRegex("Variants (.*)")]
    private static partial Regex VariantsRegex();

    [GeneratedRegex("ShaderDefines (.*)")]
    private static partial Regex ShaderDefinesRegex();

    [GeneratedRegex("VariantDependency (\\w+) (\\-*\\w+)")]
    private static partial Regex VariantDependencyRegex();

    [GeneratedRegex("(variant\\: \\w+ )?RenderQueue (.*) (.*)")]
    private static partial Regex RenderQueueRegex();

    [GeneratedRegex("(variant\\: \\w+ )?ZWrite (.*)")]
    private static partial Regex ZWriteRegex();

    [GeneratedRegex("(variant\\: \\w+ )?Depth (.*)")]
    private static partial Regex DepthRegex();

    [GeneratedRegex("(variant\\: \\w+ )?DepthTest (.*)")]
    private static partial Regex DepthTestRegex();

    public class ShaderData
    {
        public readonly SortedDictionary<string, (BlendMode, BlendMode)> blendMode = [with(StringComparer.InvariantCulture)];
        public readonly SortedDictionary<string, bool> enableDepth = [with(StringComparer.InvariantCulture)];
        public readonly SortedDictionary<string, bool> enableZWrite = [with(StringComparer.InvariantCulture)];
        public readonly SortedDictionary<string, DepthTest> depthTest = [with(StringComparer.InvariantCulture)];
        public readonly SortedDictionary<string, (MaterialRenderQueue, int)> renderQueue = [with(StringComparer.InvariantCulture)];
        public Parameter[] parameters;
        public string[] variants;
        public KeyValuePair<string, string>[] variantDependencies;
        public InstanceParameter[] instanceParameters;
        public string[] shaderDefines;
        public ShaderPiece vertex;
        public ShaderPiece fragment;
        public ShaderPiece compute;
    }

    public static bool Parse(string source, ShaderType type, out ShaderData parsedData)
    {
        parsedData = new();

        var defaultBlendFrom = BlendMode.Off;
        var defaultBlendTo = BlendMode.Off;
        var defaultEnableDepth = true;
        var defaultEnableZWrite = true;
        var defaultDepthTest = DepthTest.GreaterEqual;
        var defaultRenderQueue = MaterialRenderQueue.Opaque;
        var defaultRenderOffset = 0;

        var shaderDefinesMatch = shaderDefinesRegex.Match(source);

        if(shaderDefinesMatch.Success && shaderDefinesMatch.Length > 0)
        {
            parsedData.shaderDefines = [.. shaderDefinesMatch.Groups[1].Value.Split(' ').Select(x => x.Trim())];
        }
        else
        {
            parsedData.shaderDefines = [];
        }

        if (type == ShaderType.VertexFragment)
        {
            var variantsMatch = variantsRegex.Match(source);

            var variants = new List<string>();

            if (variantsMatch.Success && variantsMatch.Length > 0)
            {
                variants.AddRange([.. variantsMatch.Groups[1].Value.Split(",").Select(x => x.Trim())]);

                parsedData.variants = [.. variants];
                parsedData.variantDependencies = [];

                var variantDependencyMatches = variantDependencyRegex.Matches(source);
                var variantDependencies = new List<KeyValuePair<string, string>>();

                foreach(Match dependency in variantDependencyMatches)
                {
                    if(dependency.Groups.Count == 3)
                    {
                        variantDependencies.Add(new(dependency.Groups[1].Value.Trim(), dependency.Groups[2].Value.Trim()));
                    }
                }

                parsedData.variantDependencies = [.. variantDependencies];
            }
            else
            {
                parsedData.variants = [];
                parsedData.variantDependencies = [];
            }

            bool HasVariant(string variant)
            {
                foreach(var v in variants)
                {
                    if (!string.Equals(v, variant, StringComparison.InvariantCulture))
                    {
                        continue;
                    }

                    return true;
                }

                return false;
            }

            foreach(Match blendMatch in blendRegex.Matches(source))
            {
                if (blendMatch.Success &&
                    Enum.TryParse<BlendMode>(blendMatch.Groups[2].Value, true, out var from) &&
                    Enum.TryParse<BlendMode>(blendMatch.Groups[3].Value, true, out var to))
                {
                    var variant = blendMatch.Groups[1].Value.Trim();

                    if (!string.IsNullOrEmpty(variant))
                    {
                        variant = variant["variant: ".Length..];
                    }

                    if (string.IsNullOrEmpty(variant))
                    {
                        defaultBlendFrom = from;
                        defaultBlendTo = to;
                    }
                    else if(HasVariant(variant))
                    {
                        parsedData.blendMode.AddOrSetKey(variant, (from, to));
                    }
                }
            }

            foreach (Match renderQueueMatch in renderQueueRegex.Matches(source))
            {
                if (renderQueueMatch.Success &&
                    Enum.TryParse<MaterialRenderQueue>(renderQueueMatch.Groups[2].Value, true, out var renderQueue))
                {
                    var renderOffset = 0;

                    if(renderQueueMatch.Groups.Count > 3 &&
                        int.TryParse(renderQueueMatch.Groups[3].Value, CultureInfo.InvariantCulture, out var offset))
                    {
                        renderOffset = offset;
                    }

                    if (renderOffset < 0)
                    {
                        renderOffset = 0;
                    }

                    var variant = renderQueueMatch.Groups[1].Value.Trim();

                    if (!string.IsNullOrEmpty(variant))
                    {
                        variant = variant["variant: ".Length..];
                    }

                    if (string.IsNullOrEmpty(variant))
                    {
                        defaultRenderQueue = renderQueue;
                        defaultRenderOffset = renderOffset;
                    }
                    else if (HasVariant(variant))
                    {
                        parsedData.renderQueue.Add(variant, (renderQueue, renderOffset));
                    }
                }
            }

            foreach (Match depthTestMatch in depthTestRegex.Matches(source))
            {
                if (depthTestMatch.Success &&
                    Enum.TryParse<DepthTest>(depthTestMatch.Groups[2].Value, true, out var depthTest))
                {
                    var variant = depthTestMatch.Groups[1].Value.Trim();

                    if (!string.IsNullOrEmpty(variant))
                    {
                        variant = variant["variant: ".Length..];
                    }

                    if (string.IsNullOrEmpty(variant))
                    {
                        defaultDepthTest = depthTest;
                    }
                    else if (HasVariant(variant))
                    {
                        parsedData.depthTest.Add(variant, depthTest);
                    }
                }
            }

            foreach(Match enableZWriteMatch in enableZWriteRegex.Matches(source))
            {
                if (!enableZWriteMatch.Success)
                {
                    continue;
                }

                var enableZWrite = !enableZWriteMatch.Groups[2].Value.Trim().Equals("Off", StringComparison.InvariantCultureIgnoreCase);

                var variant = enableZWriteMatch.Groups[1].Value.Trim();

                if (!string.IsNullOrEmpty(variant))
                {
                    variant = variant["variant: ".Length..];
                }

                if (string.IsNullOrEmpty(variant))
                {
                    defaultEnableZWrite = enableZWrite;
                }
                else if (HasVariant(variant))
                {
                    parsedData.enableZWrite.Add(variant, enableZWrite);
                }
            }

            foreach (Match enableDepthMatch in enableDepthRegex.Matches(source))
            {
                if (!enableDepthMatch.Success)
                {
                    continue;
                }

                var enableDepth = !enableDepthMatch.Groups[2].Value.Trim().Equals("Off", StringComparison.InvariantCultureIgnoreCase);

                var variant = enableDepthMatch.Groups[1].Value.Trim();

                if (!string.IsNullOrEmpty(variant))
                {
                    variant = variant["variant: ".Length..];
                }

                if (string.IsNullOrEmpty(variant))
                {
                    defaultEnableDepth = enableDepth;
                }
                else if (HasVariant(variant))
                {
                    parsedData.enableDepth.Add(variant, enableDepth);
                }
            }
        }
        else
        {
            parsedData.variants = [];
            parsedData.variantDependencies = [];
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

            parsedData.parameters = [.. parameterList];
        }
        else
        {
            parsedData.parameters = default;
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
            HandleContent(vertexRegex, out parsedData.vertex);
            HandleContent(fragmentRegex, out parsedData.fragment);

            if(common != null)
            {
                parsedData.vertex?.content = $"{common.content}\n\n{parsedData.vertex.content}";
                parsedData.fragment?.content = $"{common.content}\n\n{parsedData.fragment.content}";
            }

            parsedData.compute = default;

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

                parsedData.instanceParameters = [.. parameterList];
            }
            else
            {
                parsedData.instanceParameters = default;
            }

            void EnsureDefault<T>(SortedDictionary<string, T> container, T value)
            {
                if(container.ContainsKey(""))
                {
                    return;
                }

                container.Add("", value);
            }

            EnsureDefault(parsedData.blendMode, (defaultBlendFrom, defaultBlendTo));
            EnsureDefault(parsedData.depthTest, defaultDepthTest);
            EnsureDefault(parsedData.enableDepth, defaultEnableDepth);
            EnsureDefault(parsedData.enableZWrite, defaultEnableZWrite);
            EnsureDefault(parsedData.renderQueue, (defaultRenderQueue, defaultRenderOffset));
        }
        else
        {
            HandleContent(commonRegex, out var common);
            HandleContent(computeRegex, out parsedData.compute);

            if (common != null)
            {
                parsedData.compute?.content = $"{common.content}\n\n{parsedData.compute.content}";
            }

            parsedData.vertex = default;
            parsedData.fragment = default;
            parsedData.instanceParameters = default;
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
                if(combination.Contains(dependency.Key))
                {
                    if(dependency.Value.StartsWith("-", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (combination.Contains(dependency.Value[1..]))
                        {
                            found = true;

                            break;
                        }
                    }
                    else if(!combination.Contains(dependency.Value))
                    {
                        found = true;

                        break;
                    }
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
