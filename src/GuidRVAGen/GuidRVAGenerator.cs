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
        context.RegisterPostInitializationOutput((context) =>
        {
            const string attribute = """
            using System;

            namespace GuidRVAGen;

            [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
            internal class GuidAttribute : Attribute
            {
                public GuidAttribute(string guid)
                { }
            }
            """;

            context.AddSource("GuidAttribute.cs", attribute);
        });

        IncrementalValuesProvider<(HierarchyInfo Hierarchy, string PropertyName, EquatableArray<ushort> Modifiers, GuidReturnType ReturnType, Guid ParsedGuid, EquatableArray<DiagnosticInfo> Diagnostics)> propertiesProvider = context.SyntaxProvider.ForAttributeWithMetadataName("GuidRVAGen.GuidAttribute",
            static (node, _) => node is PropertyDeclarationSyntax,
            static (context, token) =>
            {
                using ImmutableArrayBuilder<DiagnosticInfo> diagnostics = new();

                // Check if the consuming project is using C# 13 or higher (which supports partial properties).
                if (((CSharpCompilation)context.SemanticModel.Compilation).LanguageVersion < LanguageVersion.CSharp13)
                {
                    diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.UnsupportedCSharpVersionDescriptor, context.Attributes[0].GetLocation()));
                }

                token.ThrowIfCancellationRequested();

                IPropertySymbol propertySymbol = (IPropertySymbol)context.TargetSymbol;

                token.ThrowIfCancellationRequested();

                if (propertySymbol.SetMethod is not null)
                {
                    diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.PropertyHasSetterDescriptor, context.Attributes[0].GetLocation()));
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
                    diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.UnknownReturnTypeDescriptor, context.Attributes[0].GetLocation()));
                }

                token.ThrowIfCancellationRequested();

                GuidReturnType returnType = propertySymbol.ReturnsByRefReadonly ? GuidReturnType.RefReadonly : propertySymbol.ReturnsByRef ? GuidReturnType.Ref : GuidReturnType.Pointer;

                token.ThrowIfCancellationRequested();

                string? guid = (string?)(context.Attributes[0].ConstructorArguments[0].Value);
                Guid parsedGuid;
                if (string.IsNullOrEmpty(guid))
                {
                    diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.InvalidGuidDescriptor, context.Attributes[0].GetLocation()));
                }
                else if (!Guid.TryParse(guid, out parsedGuid))
                {
                    diagnostics.Add(DiagnosticInfo.Create(DiagnosticDescriptors.InvalidGuidDescriptor, context.Attributes[0].GetLocation()));
                }

                token.ThrowIfCancellationRequested();

                return (HierarchyInfo.From(propertySymbol.ContainingType, context.SemanticModel.Compilation is CSharpCompilation { Options.AllowUnsafe: true }), propertySymbol.Name, modifiers, returnType, parsedGuid, new EquatableArray<DiagnosticInfo>(diagnostics.ToImmutable()));
            })
            .WithTrackingName("GuidRVAGenerator.CollectProperties");

        context.RegisterSourceOutput(propertiesProvider, static (context, property) =>
        {
            if (!property.Diagnostics.IsDefaultOrEmpty)
            {
                foreach (DiagnosticInfo diagnostic in property.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic.ToDiagnostic());
                }
                return;
            }

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
                writer.WriteLine("ref global::System.Guid reference = ref global::System.Runtime.CompilerServices.Unsafe.As<byte, global::System.Guid>(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(guidBytes));");
                if (value.returnType == GuidReturnType.Pointer)
                {
                    writer.WriteLine("return (global::System.Guid*)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref reference);");
                }
                else
                {
                    writer.WriteLine("return ref reference;");
                }
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