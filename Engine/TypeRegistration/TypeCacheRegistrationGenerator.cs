using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Staple
{
    [Generator]
    public class TypeCacheRegistrationGenerator : IIncrementalGenerator
    {
        private const bool verbose = false;

        private readonly string[] blacklistedNamespaces =
        [
            "MessagePack",
            "NAudio",
            "JoltPhysicsSharp",
            "NVorbis",
            "SDL2",
            "SDL3",
            "Bgfx",
            "DrLibs",
            "OpenAL",
            "Newtonsoft",
            "NUnit",
            "NuGet",
            "Hexa",
            "ImGui",
            "Android",
            "Java",
            "Javax",
            "Org",
            "Xamarin",
            "_Microsoft",
            "Dalvik",
            "Evergine",
            "SDL",
            "System",
            "Standart.Hash.xxHash",
            "Staple.PerformanceProfiler",
            "Staple.EntityQuery",
            "Staple.SceneQuery",
            "Staple.VertexBuffer",
            "Staple.IndexBuffer",
            "Staple.ExpandableContainer",
            "Staple.World",
            "StbImageSharp",
            "StbImageWriteSharp",
            "Microsoft",
            "FastNoiseLite",
            "Nerdbank",
        ];

        private readonly string[] whitelistedNamespaces =
        [
            "System.Collections",
            "System.Numerics",
        ];

        private readonly string[] blacklistedUnmanagedTypes =
        [
            "MessagePack",
            "NAudio",
            "JoltPhysicsSharp",
            "NVorbis",
            "SDL2",
            "SDL3",
            "Bgfx",
            "DrLibs",
            "OpenAL",
            "Newtonsoft",
            "NUnit",
            "NuGet",
            "Hexa",
            "ImGui",
            "Android",
            "Java",
            "Javax",
            "Org",
            "Xamarin",
            "_Microsoft",
            "Dalvik",
            "NfdSharp",
            "Evergine",
            "SDL",
            "System",
            "Standart.Hash.xxHash",
            "Staple.PerformanceProfiler",
            "Staple.EntityQuery",
            "Staple.SceneQuery",
            "Staple.VertexBuffer",
            "Staple.IndexBuffer",
            "Staple.ExpandableContainer",
            "Staple.World",
            "StbImageSharp",
            "StbImageWriteSharp",
            "Microsoft",
            "FastNoiseLite",
            "Staple.EntityID",
            "Staple.InputActionContext",
            "Staple.Internal.AppEvent",
            "Staple.Internal.Gamepad",
            "Staple.Internal.Keyboard",
            "Staple.Internal.MaximizeWindow",
            "Staple.Internal.Mouse",
            "Staple.Internal.Touch",
            "Staple.Internal.MeshAsset",
            "Staple.Internal.RenderSystem",
            "Staple.Internal.ResetEvent",
            "Staple.Internal.ResourcePak",
            "Staple.Internal.SDL",
            "Staple.Internal.StapleShader",
            "Staple.Internal.TextRenderer",
            "Staple.Internal.Texture",
            "Staple.Input",
            "Staple.Jobs",
            "Staple.Mesh",
            "Staple.Ray",
            "Staple.RenderEntry",
            "Staple.TextParameters",
            "Staple.Internal",
        ];

        private readonly string[] whitelistedUnmanagedTypes =
        [
            typeof(char).FullName,
            typeof(byte).FullName,
            typeof(sbyte).FullName,
            typeof(ushort).FullName,
            typeof(short).FullName,
            typeof(uint).FullName,
            typeof(int).FullName,
            typeof(ulong).FullName,
            typeof(long).FullName,
            typeof(float).FullName,
            typeof(double).FullName,
            "System.Numerics",
        ];

        private readonly string[] whitelistedTypes =
        [
            typeof(char).FullName,
            typeof(byte).FullName,
            typeof(sbyte).FullName,
            typeof(ushort).FullName,
            typeof(short).FullName,
            typeof(uint).FullName,
            typeof(int).FullName,
            typeof(ulong).FullName,
            typeof(long).FullName,
            typeof(float).FullName,
            typeof(double).FullName,
            typeof(string).FullName,
            typeof(bool).FullName,
        ];

        private bool IsBlacklisted(string typeName)
        {
            foreach (var item in whitelistedTypes)
            {
                if (typeName.StartsWith(item))
                {
                    return false;
                }
            }

            foreach (var item in whitelistedNamespaces)
            {
                if(typeName.StartsWith(item))
                {
                    return false;
                }
            }

            foreach(var item in whitelistedUnmanagedTypes)
            {
                if(typeName.StartsWith(item))
                {
                    return false;
                }
            }

            foreach (var item in blacklistedNamespaces)
            {
                if (typeName.StartsWith(item))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsBlacklistedUnmanagedType(string typeName)
        {
            foreach (var item in whitelistedUnmanagedTypes)
            {
                if (typeName.StartsWith(item))
                {
                    return false;
                }
            }

            foreach (var item in blacklistedUnmanagedTypes)
            {
                if (typeName.StartsWith(item))
                {
                    return true;
                }
            }

            return false;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
#if DEBUG && false
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif

            var excludedTypes = new List<string>();

            var configFile = context.AdditionalTextsProvider.Where(x => x.Path.EndsWith("TypeExclusions.txt"))
                .Select((file, token) => file.GetText(token)?.ToString())
                .Select((text, _) =>
                {
                    if(string.IsNullOrEmpty(text))
                    {
                        return [];
                    }

                    return text
                        .Split('\n')
                        .Select(x => x.Trim())
                        .ToList();
                })
                .Collect();

            var source = $@"// <auto-generated />
using System;
using System.Runtime.InteropServices;

namespace StapleCodeGeneration
{{
    public static class TypeCacheRegistration
    {{
        public static void RegisterAll()
        {{
";

            var types = new HashSet<string>();
            var constructibleTypes = new HashSet<string>();
            var unmanagedConstructibleTypes = new HashSet<string>();
            var sizableTypes = new HashSet<string>();
            var componentTypes = new HashSet<string>();
            var typeSizes = new Dictionary<string, int>();
            var componentFieldsAndProperties = new Dictionary<string, List<KeyValuePair<string, string>>>();

            void Perform(IAssemblySymbol symbol, bool isSelf)
            {
                bool IsValidNamedTypeSymbol(INamedTypeSymbol t)
                {
                    return !(t.IsAbstract ||
                        t.IsGenericType ||
                        t.IsStatic ||
                        t.IsRefLikeType ||
                        t.TypeKind == TypeKind.Delegate ||
                        t.Name.EndsWith("Attribute") ||
                        t.Name.EndsWith("Exception") ||
                        t.GetAttributes().Any(x => x.AttributeClass.Name == typeof(RequiredAttributeAttribute).Name ||
                            x.AttributeClass.Name == typeof(ObsoleteAttribute).Name ||
                            x.AttributeClass.Name == "ExperimentalAttribute") ||
                        (t.DeclaredAccessibility != Accessibility.Public &&
                        (t.DeclaredAccessibility != Accessibility.Internal || !isSelf)));
                }

                bool IsValidSymbol(ISymbol t)
                {
                    return !(t.IsAbstract ||
                        t.IsStatic ||
                        t.Name.EndsWith("Attribute") ||
                        t.Name.EndsWith("Exception") ||
                        t.GetAttributes().Any(x => x.AttributeClass.Name == typeof(RequiredAttributeAttribute).Name ||
                            x.AttributeClass.Name == typeof(ObsoleteAttribute).Name ||
                            x.AttributeClass.Name == "ExperimentalAttribute") ||
                        (t.DeclaredAccessibility != Accessibility.Public &&
                            (t.DeclaredAccessibility != Accessibility.Internal || !isSelf)) ||
                        (t.ContainingType != null &&
                            (t.ContainingType.TypeKind == TypeKind.Delegate || t.ContainingType.IsRefLikeType))
                        );
                }

                bool IsPrimitive(ITypeSymbol t)
                {
                    return t.SpecialType switch
                    {
                        SpecialType.System_Boolean => true,
                        SpecialType.System_Byte => true,
                        SpecialType.System_SByte => true,
                        SpecialType.System_UInt16 => true,
                        SpecialType.System_Int16 => true,
                        SpecialType.System_UInt32 => true,
                        SpecialType.System_Int32 => true,
                        SpecialType.System_UInt64 => true,
                        SpecialType.System_Int64 => true,
                        SpecialType.System_Single => true,
                        SpecialType.System_Double => true,
                        SpecialType.System_Char => true,
                        SpecialType.System_Enum => true,
                        _ => false,
                    };
                }

                bool IsValidUnmanagedSerializerSymbol(ITypeSymbol t, HashSet<string> checkedTypes)
                {
                    if (!IsValidSymbol(t) ||
                        (!t.IsValueType && !t.IsUnmanagedType) ||
                        !TryGetConstructibleTypeSymbolName(t, out var typeName) ||
                        IsBlacklistedUnmanagedType(typeName))
                    {
                        return false;
                    }

                    if (checkedTypes.Contains(typeName))
                    {
                        return true;
                    }

                    if (IsPrimitive(t))
                    {
                        checkedTypes.Add(typeName);

                        return true;
                    }

                    if(t.TypeKind == TypeKind.Struct)
                    {
                        var members = t.GetMembers()
                            .Where(x => x.Kind == SymbolKind.Field || x.Kind == SymbolKind.Property)
                            .ToArray();

                        foreach (var member in members)
                        {
                            if (member is IFieldSymbol field && !SymbolEqualityComparer.Default.Equals(field.Type, t))
                            {
                                if (!IsValidUnmanagedSerializerSymbol(field.Type, checkedTypes))
                                {
                                    return false;
                                }

                                if (TryGetConstructibleTypeSymbolName(field.Type, out var subtypeName))
                                {
                                    checkedTypes.Add(subtypeName);
                                }
                            }
                            else if (member is IPropertySymbol property && !SymbolEqualityComparer.Default.Equals(property.Type, t))
                            {
                                if (!IsValidUnmanagedSerializerSymbol(property.Type, checkedTypes))
                                {
                                    return false;
                                }

                                if (TryGetConstructibleTypeSymbolName(property.Type, out var subtypeName))
                                {
                                    checkedTypes.Add(subtypeName);
                                }
                            }
                        }
                    }

                    checkedTypes.Add(typeName);

                    return true;
                }

                bool TryGetConstructibleTypeSymbolName(ITypeSymbol symbol, out string typeName)
                {
                    var displayFormat = new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Omitted,
                        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                        SymbolDisplayGenericsOptions.IncludeTypeParameters);

                    typeName = symbol.ToDisplayString(NullableFlowState.None, displayFormat);

                    if (excludedTypes.Contains(typeName))
                    {
                        return false;
                    }

                    if (typeName.EndsWith("*"))
                    {
                        return false;
                    }

                    if (typeName.StartsWith("MessagePack"))
                    {
                        return false;
                    }

                    if (typeName.Contains("e__FixedBuffer"))
                    {
                        return false;
                    }

                    if (typeName.ToUpperInvariant().Contains("DELEGATE"))
                    {
                        return false;
                    }

                    return !symbol.IsStatic &&
                        new List<TypeKind>([TypeKind.Enum, TypeKind.Class, TypeKind.Struct, TypeKind.Interface])
                            .Contains(symbol.TypeKind);
                }

                void HandleNamedSymbol(INamedTypeSymbol t, string baseTypeName)
                {
                    if (!IsValidNamedTypeSymbol(t))
                    {
                        if (verbose)
                        {
                            source += $"            //Ignoring type {t.Name}: {t.DeclaredAccessibility}\r\n";
                        }

                        return;
                    }

                    var typeName = baseTypeName.Length == 0 ? t.Name : $"{baseTypeName}.{t.Name}";

                    Debug.WriteLine($"Adding type {typeName}");

                    if (excludedTypes.Contains(typeName))
                    {
                        source += $"            //Found {typeName}. Skipped (excluded)\r\n";

                        return;
                    }

                    if (typeName.Contains("e__FixedBuffer"))
                    {
                        if (verbose)
                        {
                            source += $"            //Processing to {typeName}. Skipped (FixedBuffer)\r\n";
                        }

                        return;
                    }

                    if (typeName.ToUpperInvariant().Contains("DELEGATE"))
                    {
                        if (verbose)
                        {
                            source += $"            //Processing to {typeName}. Skipped (Delegate)\r\n";
                        }

                        return;
                    }

                    types.Add(typeName);

                    if (!t.IsStatic &&
                        (t.Constructors.Any(x => x.Parameters.Length == 0) ||
                        t.TypeKind == TypeKind.Struct ||
                        t.TypeKind == TypeKind.Interface))
                    {
                        constructibleTypes.Add(typeName);
                    }

                    var cache = new HashSet<string>();

                    if (IsValidUnmanagedSerializerSymbol(t, cache))
                    {
                        unmanagedConstructibleTypes.Add(typeName);
                    }

                    if (!t.IsStatic &&
                        (t.Constructors.Any(x => x.Parameters.Length == 0) ||
                        t.TypeKind == TypeKind.Struct))
                    {
                        sizableTypes.Add(typeName);
                    }

                    bool BaseTypeIsStapleComponent(INamedTypeSymbol symbol)
                    {
                        if (symbol.ContainingNamespace?.Name == "Staple" && symbol.Name == "Component")
                        {
                            return true;
                        }

                        if (symbol.BaseType != null)
                        {
                            return BaseTypeIsStapleComponent(symbol.BaseType);
                        }

                        return false;
                    }

                    var fieldsAndProperties = t.GetMembers().Where(x => x.Kind == SymbolKind.Field || x.Kind == SymbolKind.Property);

                    if (BaseTypeIsStapleComponent(t))
                    {
                        componentTypes.Add(typeName);

                        if(!componentFieldsAndProperties.ContainsKey(typeName))
                        {
                            var container = new List<KeyValuePair<string, string>>();

                            foreach (var member in fieldsAndProperties)
                            {
                                if(!IsValidSymbol(member))
                                {
                                    continue;
                                }

                                var memberType = member switch
                                {
                                    IFieldSymbol field => field.Type,
                                    IPropertySymbol property => property.Type,
                                    _ => null,
                                };

                                if(memberType == null ||
                                    !TryGetConstructibleTypeSymbolName(memberType, out var memberTypeName))
                                {
                                    continue;
                                }

                                container.Add(new(member.Name, memberTypeName));
                            }

                            componentFieldsAndProperties.Add(typeName, container);
                        }
                    }

                    foreach (var member in fieldsAndProperties)
                    {
                        HandleSymbol(member);
                    }

                    foreach (var type in t.GetTypeMembers())
                    {
                        HandleNamedSymbol(type, baseTypeName.Length == 0 ? t.Name : $"{baseTypeName}.{t.Name}");
                    }
                }

                void HandleSymbol(ISymbol t)
                {
                    if (!IsValidSymbol(t))
                    {
                        if (verbose)
                        {
                            source += $"            //Ignoring type {t.Name}: {t.DeclaredAccessibility}\r\n";
                        }

                        return;
                    }

                    Debug.WriteLine($"Entering type {t.Name}");

                    if (verbose)
                    {
                        source += $"            //Entering type: {t.Name} ({t.GetType().FullName})\r\n";
                    }

                    void HandleTypeSymbol(ITypeSymbol symbol)
                    {
                        var displayFormat = new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Omitted,
                            SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                            SymbolDisplayGenericsOptions.IncludeTypeParameters);

                        var typeName = symbol.ToDisplayString(NullableFlowState.None, displayFormat);

                        if (excludedTypes.Contains(typeName))
                        {
                            source += $"            //Found {typeName}. Skipped (excluded)\r\n";

                            return;
                        }

                        if (typeName.EndsWith("*"))
                        {
                            if (verbose)
                            {
                                source += $"            //Resolved to {typeName}. Skipped (pointer)\r\n";
                            }

                            return;
                        }

                        if (typeName.StartsWith("MessagePack"))
                        {
                            if (verbose)
                            {
                                source += $"            //Resolved to {typeName}. Skipped (MessagePack)\r\n";
                            }

                            return;
                        }

                        if (typeName.Contains("e__FixedBuffer"))
                        {
                            if (verbose)
                            {
                                source += $"            //Resolved to {typeName}. Skipped (FixedBuffer)\r\n";
                            }

                            return;
                        }

                        if (typeName.ToUpperInvariant().Contains("DELEGATE"))
                        {
                            if (verbose)
                            {
                                source += $"            //Processing to {typeName}. Skipped (Delegate)\r\n";
                            }

                            return;
                        }

                        Debug.WriteLine($"Resolved to {typeName}");

                        if (verbose)
                        {
                            source += $"            //Resolved to {typeName}\r\n";
                        }

                        types.Add(typeName);

                        if (!symbol.IsStatic &&
                            new List<TypeKind>([TypeKind.Enum, TypeKind.Class, TypeKind.Struct])
                                .Contains(symbol.TypeKind))
                        {
                            sizableTypes.Add(typeName);
                        }

                        if (!symbol.IsStatic &&
                            new List<TypeKind>([TypeKind.Enum, TypeKind.Class, TypeKind.Struct, TypeKind.Interface])
                                .Contains(symbol.TypeKind))
                        {
                            constructibleTypes.Add(typeName);
                        }
                    }

                    if (t is IFieldSymbol field)
                    {
                        HandleTypeSymbol(field.Type);
                    }
                    else if (t is IPropertySymbol property)
                    {
                        HandleTypeSymbol(property.Type);
                    }
                }

                void HandleNamespace(INamespaceSymbol n)
                {
                    var namespaceName = n.Name;

                    void Name(INamespaceSymbol s)
                    {
                        if (s == null || s.Name.Length == 0)
                        {
                            return;
                        }

                        namespaceName = $"{s.Name}.{namespaceName}";

                        if (s.ContainingNamespace != null)
                        {
                            Name(s.ContainingNamespace);
                        }
                    }

                    Name(n.ContainingNamespace);

                    foreach (var ns in n.GetNamespaceMembers())
                    {
                        HandleNamespace(ns);
                    }

                    if (IsBlacklisted(namespaceName))
                    {
                        return;
                    }

                    foreach (var t in n.GetTypeMembers())
                    {
                        HandleNamedSymbol(t, namespaceName);
                    }
                }

                foreach (var n in symbol.GlobalNamespace.GetNamespaceMembers())
                {
                    HandleNamespace(n);
                }

                HandleNamespace(symbol.GlobalNamespace);
            }

            context.RegisterSourceOutput(context.CompilationProvider.Combine(configFile),
                (sourceContext, compilation) =>
            {
                excludedTypes = [.. compilation.Right.ToString()
                    .Split('\n')
                    .Select(x => x.Trim())
                    ];

                var module = compilation.Left.SourceModule;

                foreach (var symbol in module.ReferencedAssemblySymbols)
                {
                    Perform(symbol, false);
                }

                Perform(module.ContainingAssembly, true);

                unmanagedConstructibleTypes.Add(typeof(char).FullName);
                unmanagedConstructibleTypes.Add(typeof(byte).FullName);
                unmanagedConstructibleTypes.Add(typeof(sbyte).FullName);
                unmanagedConstructibleTypes.Add(typeof(ushort).FullName);
                unmanagedConstructibleTypes.Add(typeof(short).FullName);
                unmanagedConstructibleTypes.Add(typeof(uint).FullName);
                unmanagedConstructibleTypes.Add(typeof(int).FullName);
                unmanagedConstructibleTypes.Add(typeof(ulong).FullName);
                unmanagedConstructibleTypes.Add(typeof(long).FullName);
                unmanagedConstructibleTypes.Add(typeof(float).FullName);
                unmanagedConstructibleTypes.Add(typeof(double).FullName);

                types.Add(typeof(char).FullName);
                types.Add(typeof(byte).FullName);
                types.Add(typeof(sbyte).FullName);
                types.Add(typeof(ushort).FullName);
                types.Add(typeof(short).FullName);
                types.Add(typeof(uint).FullName);
                types.Add(typeof(int).FullName);
                types.Add(typeof(ulong).FullName);
                types.Add(typeof(long).FullName);
                types.Add(typeof(float).FullName);
                types.Add(typeof(double).FullName);
                types.Add(typeof(string).FullName);

                sizableTypes.Add(typeof(char).FullName);
                sizableTypes.Add(typeof(byte).FullName);
                sizableTypes.Add(typeof(sbyte).FullName);
                sizableTypes.Add(typeof(ushort).FullName);
                sizableTypes.Add(typeof(short).FullName);
                sizableTypes.Add(typeof(uint).FullName);
                sizableTypes.Add(typeof(int).FullName);
                sizableTypes.Add(typeof(ulong).FullName);
                sizableTypes.Add(typeof(long).FullName);
                sizableTypes.Add(typeof(float).FullName);
                sizableTypes.Add(typeof(double).FullName);

                constructibleTypes.Add(typeof(char).FullName);
                constructibleTypes.Add(typeof(byte).FullName);
                constructibleTypes.Add(typeof(sbyte).FullName);
                constructibleTypes.Add(typeof(ushort).FullName);
                constructibleTypes.Add(typeof(short).FullName);
                constructibleTypes.Add(typeof(uint).FullName);
                constructibleTypes.Add(typeof(int).FullName);
                constructibleTypes.Add(typeof(ulong).FullName);
                constructibleTypes.Add(typeof(long).FullName);
                constructibleTypes.Add(typeof(float).FullName);
                constructibleTypes.Add(typeof(double).FullName);
                constructibleTypes.Add(typeof(string).FullName);

                foreach (var type in unmanagedConstructibleTypes)
                {
                    var serializerName = $"{type.Replace('.', '_')}UnmanagedSerializer";

                    source += $@"
            Staple.Internal.TypeCache.RegisterUnmanagedArraySerializer<{type}>(new {serializerName}());
";


                    var serializerSource = $@"// <auto-generated />
using Staple;
using Staple.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace StapleCodeGeneration
{{
    public class {serializerName} : IStapleUnmanagedArraySerializer
    {{
        public byte[] SerializeArray(Array array)
        {{
            if(array is not {type}[] localArray)
            {{
                return null;
            }}

            var arraySpan = new Span<{type}>(localArray);

            return MemoryUtils.ToByteArray(arraySpan);
        }}

        public byte[] SerializeList(IList list)
        {{
            if(list is not List<{type}> localList)
            {{
                return null;
            }}

            var byteSize = TypeCache.SizeOf(typeof({type}).ToString()) * localList.Count;

            var buffer = new byte[byteSize];

            var listSpan = CollectionsMarshal.AsSpan(localList);

            return MemoryUtils.ToByteArray(listSpan);
        }}

        public void DeserializeArray(byte[] data, Array target)
        {{
            MemoryUtils.FillArrayWithBytes<{type}>(data, target);
        }}
    }}
}}
";

                    sourceContext.AddSource($"{serializerName}.Generated.cs", SourceText.From(serializerSource, Encoding.UTF8));
                }

                foreach (var type in types)
                {
                    /*
                    if(type.StartsWith("System.") && !type.StartsWith("System.Collections"))
                    {
                        continue;
                    }
                    */

                    if (IsBlacklisted(type))
                    {
                        continue;
                    }

                    //source += $"            Console.WriteLine(\"Registering {type}\");\r\n\r\n";

                    var constructorSnippet = "";

                    var sizeofSnippet = sizableTypes.Contains(type) ? $@"
                () =>
                {{
                    return Marshal.SizeOf<{type}>();
                }},
" : "null,\n";

                    if (constructibleTypes.Contains(type))
                    {
                        constructorSnippet = $@"
                (length) =>
                {{
                    return new {type}[length];
                }},
";
                    }
                    else
                    {
                        constructorSnippet = "null, ";
                    }

                    if (componentTypes.Contains(type) && constructibleTypes.Contains(type))
                    {
                        source += $@"
            Staple.Internal.TypeCache.RegisterType(typeof({type}),
                {sizeofSnippet}
                {constructorSnippet}
                new() 
                {{
                    add = (entity) => entity.AddComponent<{type}>(),
                    remove = (entity) => entity.RemoveComponent<{type}>(),
                    get = (entity) => entity.GetComponent<{type}>(),
                }});

";
                    }
                    else
                    {
                        source += $@"
            Staple.Internal.TypeCache.RegisterType(typeof({type}),
                {sizeofSnippet}
                {constructorSnippet}
                null);
";
                    }
                }

                source += $@"
        }}
    }}
}}";

                sourceContext.AddSource("TypeCacheRegistration.Generated.cs", SourceText.From(source, Encoding.UTF8));
            });
        }
    }
}
