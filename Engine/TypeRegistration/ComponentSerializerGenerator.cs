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
    public class ComponentSerializerGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            /*
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
            */

            var componentFieldsAndProperties = new Dictionary<string, List<KeyValuePair<string, string>>>();

            void Perform(IAssemblySymbol symbol, bool isSelf)
            {
                bool IsValidNamedTypeSymbol(INamedTypeSymbol t)
                {
                    return !(t.IsAbstract || t.IsGenericType || t.IsStatic || t.Name.EndsWith("Attribute") ||
                        t.GetAttributes().Any(x => x.AttributeClass.Name == typeof(RequiredAttributeAttribute).Name ||
                        x.AttributeClass.Name == typeof(ObsoleteAttribute).Name ||
                        x.AttributeClass.Name == "ExperimentalAttribute") ||
                        (t.DeclaredAccessibility != Accessibility.Public &&
                        (t.DeclaredAccessibility != Accessibility.Internal || !isSelf)) ||
                        t.TypeKind == TypeKind.Delegate ||
                        t.IsRefLikeType);
                }

                bool IsValidSymbol(ISymbol t)
                {
                    return !(t.IsAbstract || t.Name.EndsWith("Attribute") || t.IsStatic ||
                        t.GetAttributes().Any(x => x.AttributeClass.Name == typeof(RequiredAttributeAttribute).Name ||
                        x.AttributeClass.Name == typeof(ObsoleteAttribute).Name ||
                        x.AttributeClass.Name == "ExperimentalAttribute") ||
                        (t.DeclaredAccessibility != Accessibility.Public &&
                        (t.DeclaredAccessibility != Accessibility.Internal || !isSelf)) ||
                        t.ContainingType.TypeKind == TypeKind.Delegate ||
                        t.ContainingType.IsRefLikeType);
                }

                bool TryGetConstructibleTypeSymbolName(ITypeSymbol symbol, out string typeName)
                {
                    var displayFormat = new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Omitted,
                        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                        SymbolDisplayGenericsOptions.IncludeTypeParameters);

                    typeName = symbol.ToDisplayString(NullableFlowState.None, displayFormat);

                    return !symbol.IsStatic &&
                        new List<TypeKind>([TypeKind.Enum, TypeKind.Class, TypeKind.Struct, TypeKind.Interface])
                            .Contains(symbol.TypeKind);
                }

                void HandleNamedSymbol(INamedTypeSymbol t, string baseTypeName)
                {
                    if (!IsValidNamedTypeSymbol(t))
                    {
                        return;
                    }

                    var typeName = baseTypeName.Length == 0 ? t.Name : $"{baseTypeName}.{t.Name}";

                    bool BaseTypeIsStapleComponent(INamedTypeSymbol symbol)
                    {
                        if (symbol.GetAttributes().Any(x => x.AttributeClass.Name == "AbstractComponent"))
                        {
                            return false;
                        }

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

                    if (!BaseTypeIsStapleComponent(t))
                    {
                        return;
                    }

                    var fieldsAndProperties = t.GetMembers().Where(x => x.Kind == SymbolKind.Field || x.Kind == SymbolKind.Property).ToArray();

                    if (!componentFieldsAndProperties.ContainsKey(typeName) && fieldsAndProperties.Length > 0)
                    {
                        var container = new List<KeyValuePair<string, string>>();

                        foreach (var member in fieldsAndProperties)
                        {
                            if (!IsValidSymbol(member))
                            {
                                continue;
                            }

                            var memberType = member switch
                            {
                                IFieldSymbol field => field.Type,
                                IPropertySymbol property => property.Type,
                                _ => null,
                            };

                            if (memberType == null ||
                                !TryGetConstructibleTypeSymbolName(memberType, out var memberTypeName))
                            {
                                continue;
                            }

                            container.Add(new(member.Name, memberTypeName));
                        }

                        componentFieldsAndProperties.Add(typeName, container);
                    }
                }

                void HandleNamespace(INamespaceSymbol n)
                {
                    if (n.Name == "System" || n.Name == "Microsoft")
                    {
                        return;
                    }

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

                    foreach (var t in n.GetTypeMembers())
                    {
                        HandleNamedSymbol(t, namespaceName);
                    }

                    foreach (var ns in n.GetNamespaceMembers())
                    {
                        HandleNamespace(ns);
                    }
                }

                foreach (var n in symbol.GlobalNamespace.GetNamespaceMembers())
                {
                    HandleNamespace(n);
                }

                HandleNamespace(symbol.GlobalNamespace);
            }

            context.RegisterSourceOutput(context.CompilationProvider,
                (sourceContext, compilation) =>
                {
                    var module = compilation.SourceModule;

                    foreach (var symbol in module.ReferencedAssemblySymbols)
                    {
                        Perform(symbol, false);
                    }

                    Perform(module.ContainingAssembly, true);

                    var serializerNames = new Dictionary<string, KeyValuePair<string, string>>();

                    foreach(var pair in componentFieldsAndProperties)
                    {
                        var serializerName = $"{pair.Key.Replace('.', '_')}Serializer";

                        serializerNames.Add(pair.Key, new(serializerName, pair.Key));

                        var source = $@"
// <auto-generated />
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Staple.Internal;

namespace StapleCodeGeneration
{{
    public class {serializerName} : IComponentSerializer<{pair.Key}>
    {{
        public void Deserialize({pair.Key} component, Dictionary<string, object> fields)
        {{
";

                        foreach(var field in pair.Value)
                        {
                            source += $@"
            //component.{field.Key} = {field.Value};
";
                        }

                        source += $@"
        }}
    }}
}}
";

                        sourceContext.AddSource($"{pair.Key}.Component.Generated.cs", SourceText.From(source, Encoding.UTF8));
                    }

                    var lookupSource = $@"
        private static readonly Dictionary<Type, int> resolverLookup = new()
        {{
";

                    var counter = 0;

                    foreach (var pair in serializerNames)
                    {
                        lookupSource += $@"
            {{ typeof({pair.Key}), {counter++} }},";
                    }

                    lookupSource += $@"
        }};
";

                    var arrayLookupSource = $@"
        private static readonly IComponentSerializer[] resolverArray = 
            [
";

                    foreach (var pair in serializerNames)
                    {
                        arrayLookupSource += $@"
                new {pair.Value.Key}(),";
                    }

                    arrayLookupSource += $@"
            ];
";

                    var resolverSource = $@"
// <auto-generated />
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Staple.Internal;

namespace StapleCodeGeneration
{{
    public class ComponentSerializationResolver : IComponentSerializerResolver
    {{
{lookupSource}
{arrayLookupSource}
        public IComponentSerializer<T> GetSerializer<T>()
        {{
            if(!resolverLookup.TryGetValue(typeof(T), out var index))
            {{
                return default;
            }}

            return (IComponentSerializer<T>)resolverArray[index];
        }}
    }}
}}
";
                    sourceContext.AddSource($"ComponentSerializerResolver.Generated.cs", SourceText.From(resolverSource, Encoding.UTF8));
                });
        }
    }
}
