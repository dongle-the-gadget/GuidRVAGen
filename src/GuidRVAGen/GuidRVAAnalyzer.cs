using GuidRVAGen.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace GuidRVAGen;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class GuidRVAAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
    [
        DiagnosticDescriptors.UnsupportedCSharpVersionDescriptor,
        DiagnosticDescriptors.UnknownReturnTypeDescriptor,
        DiagnosticDescriptors.InvalidGuidDescriptor,
        DiagnosticDescriptors.PropertyHasSetterDescriptor,
        DiagnosticDescriptors.PropertyNotPartialDescriptor
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            INamedTypeSymbol guidSymbol = context.Compilation.GetTypeByMetadataName("System.Guid")!;
            IPointerTypeSymbol guidPointerSymbol = context.Compilation.CreatePointerTypeSymbol(guidSymbol);

            context.RegisterSymbolAction((ctx) =>
            {
                if (ctx.Symbol is not IPropertySymbol propertySymbol)
                {
                    return;
                }

                var attribute = propertySymbol.GetAttributes().FirstOrDefault(f => f.AttributeClass?.GetFullyQualifiedMetadataName() == "GuidRVAGen.GuidAttribute");

                if (attribute is null)
                {
                    return;
                }

                if (((CSharpCompilation)ctx.Compilation).LanguageVersion < LanguageVersion.CSharp13)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnsupportedCSharpVersionDescriptor, attribute.GetLocation()));
                }

                // Check if the property is has a valid return type.
                // The return type is valid if:
                //    1. The property is not a ref return type and the return type is a pointer to a Guid.
                //    2. The property is a ref return type and the return type is a Guid.
                bool isValidReturnType =
                    SymbolEqualityComparer.Default.Equals(propertySymbol.Type, guidSymbol) &&
                    (propertySymbol.ReturnsByRef || propertySymbol.ReturnsByRefReadonly) ||
                    SymbolEqualityComparer.Default.Equals(propertySymbol.Type, guidPointerSymbol) &&
                    !propertySymbol.ReturnsByRef &&
                    !propertySymbol.ReturnsByRefReadonly;

                if (!isValidReturnType)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnknownReturnTypeDescriptor, attribute.GetLocation()));
                }

                string? guid = attribute.ConstructorArguments.FirstOrDefault().Value?.ToString();

                if (string.IsNullOrEmpty(guid) || !Guid.TryParse(guid, out _))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidGuidDescriptor, attribute.GetLocation()));
                }

                if (propertySymbol.SetMethod is not null)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.PropertyHasSetterDescriptor, attribute.GetLocation()));
                }

                if (propertySymbol.IsDefinition && !propertySymbol.IsPartialDefinition)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.PropertyNotPartialDescriptor, attribute.GetLocation()));
                }
            }, SymbolKind.Property);
        });
    }
}