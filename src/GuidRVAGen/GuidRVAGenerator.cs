using GuidRVAGen.Extensions;
using GuidRVAGen.Helpers;
using GuidRVAGen.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace GuidRVAGen;

[Generator(LanguageNames.CSharp)]
public class GuidRVAGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(HierarchyInfo Hierarchy, string PropertyName, EquatableArray<ushort> Modifiers, GuidReturnType ReturnType, Guid ParsedGuid)> propertiesProvider = context.SyntaxProvider.ForAttributeWithMetadataName("GuidRVAGen.GuidAttribute",
            static (node, _) => node is PropertyDeclarationSyntax syntax && syntax.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)),
            static (context, token) =>
            {
                // Check if the consuming project is using C# 13 or higher (which supports partial properties).
                if (((CSharpCompilation)context.SemanticModel.Compilation).LanguageVersion < LanguageVersion.CSharp13)
                {
                    return default;
                }

                token.ThrowIfCancellationRequested();

                IPropertySymbol propertySymbol = (IPropertySymbol)context.TargetSymbol;

                token.ThrowIfCancellationRequested();

                if (propertySymbol.SetMethod is not null)
                {
                    return default;
                }

                token.ThrowIfCancellationRequested();

                EquatableArray<ushort> modifiers = ((PropertyDeclarationSyntax)context.TargetNode).Modifiers.Select(modifier => (ushort)modifier.Kind()).ToImmutableArray();

                token.ThrowIfCancellationRequested();

                INamedTypeSymbol guidSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Guid")!;
                IPointerTypeSymbol guidPointerSymbol = context.SemanticModel.Compilation.CreatePointerTypeSymbol(guidSymbol);

                token.ThrowIfCancellationRequested();

                // Check if the property is has a valid return type.
                // The return type is valid if:
                //    1. The property is not a ref return type and the return type is a pointer to a Guid.
                //    2. The property is a ref return type and the return type is a Guid.
                bool isValidReturnType = 
                    (SymbolEqualityComparer.Default.Equals(propertySymbol.Type, guidSymbol) &&
                    (propertySymbol.ReturnsByRef || propertySymbol.ReturnsByRefReadonly)) ||
                    (SymbolEqualityComparer.Default.Equals(propertySymbol.Type, guidPointerSymbol) && 
                    !propertySymbol.ReturnsByRef && 
                    !propertySymbol.ReturnsByRefReadonly);

                token.ThrowIfCancellationRequested();

                if (!isValidReturnType)
                {
                    return default;
                }

                token.ThrowIfCancellationRequested();

                GuidReturnType returnType = propertySymbol.ReturnsByRefReadonly ? GuidReturnType.RefReadonly : propertySymbol.ReturnsByRef ? GuidReturnType.Ref : GuidReturnType.Pointer;

                token.ThrowIfCancellationRequested();

                string? guid = (string?)(context.Attributes[0].ConstructorArguments[0].Value);
                Guid parsedGuid;
                if (string.IsNullOrEmpty(guid))
                {
                    return default;
                }
                else if (!Guid.TryParse(guid, out parsedGuid))
                {
                    return default;
                }

                token.ThrowIfCancellationRequested();

                return (HierarchyInfo.From(propertySymbol.ContainingType, context.SemanticModel.Compilation is CSharpCompilation { Options.AllowUnsafe: true }), propertySymbol.Name, modifiers, returnType, parsedGuid);
            })
            .WithTrackingName("GuidRVAGenerator.CollectProperties");

        IncrementalValuesProvider<(HierarchyInfo Hierarchy, string PropertyName, EquatableArray<ushort> Modifiers, GuidReturnType ReturnType, Guid ParsedGuid)> filteredProperties
            = propertiesProvider.Where(static (property) => property != default)
            .WithTrackingName("GuidRVAGenerator.CollectPropertiesFiltered");

        context.RegisterSourceOutput(filteredProperties, static (context, property) =>
        {
            using IndentedTextWriter writer = new();

            static void WriteProperty((string propertyName, EquatableArray<ushort> modifiers, GuidReturnType returnType, Guid guid) value, IndentedTextWriter writer)
            {
                string returnTypeString = value.returnType switch
                {
                    GuidReturnType.Pointer => "global::System.Guid*",
                    GuidReturnType.Ref => "ref global::System.Guid",
                    GuidReturnType.RefReadonly => "ref readonly global::System.Guid",
                    _ => throw new NotSupportedException()
                };

                writer.WriteLine("/// <inheritdoc/>");
                writer.WriteGeneratedAttributes(typeof(GuidRVAGenerator).FullName);

                foreach (ushort modifier in value.modifiers)
                {
                    writer.Write($"{SyntaxFacts.GetText((SyntaxKind)modifier)} ");
                }

                writer.WriteLine($"{returnTypeString} {value.propertyName}");
                writer.WriteLine("{");
                writer.IncreaseIndent();
                writer.WriteLine("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
                writer.WriteLine("get");
                writer.WriteLine("{");
                writer.IncreaseIndent();
                writer.Write("global::System.ReadOnlySpan<byte> guidBytes = [ ");

                Span<byte> guidBytes = stackalloc byte[16];
                value.guid.TryWriteBytes(guidBytes);

                for (int i = 0; i < guidBytes.Length; i++)
                {
                    writer.Write($"0x{guidBytes[i]:X2}");
                    if (i < guidBytes.Length - 1)
                    {
                        writer.Write(", ");
                    }
                }

                writer.WriteLine(" ];");
                writer.Write("return ");
                if (value.returnType == GuidReturnType.Pointer)
                {
                    writer.Write("(global::System.Guid*)global::System.Runtime.CompilerServices.Unsafe.AsPointer(");
                }
                writer.Write("ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(guidBytes))");
                if (value.returnType == GuidReturnType.Pointer)
                {
                    writer.Write(")");
                }
                writer.WriteLine(";");
                writer.DecreaseIndent();
                writer.WriteLine("}");
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }

            property.Hierarchy.WriteSyntax((property.PropertyName, property.Modifiers, property.ReturnType, property.ParsedGuid), writer, [], [WriteProperty]);

            context.AddSource($"{property.Hierarchy.FullyQualifiedMetadataName}.{property.PropertyName}.g.cs", writer.ToString());
        });
    }
}