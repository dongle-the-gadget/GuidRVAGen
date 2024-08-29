using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace GuidRVAGen;

internal static class DiagnosticDescriptors
{
    public static DiagnosticDescriptor UnsupportedCSharpVersionDescriptor = new(
        id: "GUIDGEN0001",
        title: "Unsupported C# version",
        messageFormat: "The GuidGen source generator requires consuming projects to use C# version 13 or higher",
        category: typeof(GuidRVAGenerator).FullName,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnknownReturnTypeDescriptor = new(
        id: "GUIDGEN0002",
        title: "Unknown return type",
        messageFormat: "The [Guid] attribute can only be applied to properties with a return type of \"ref System.Guid\", \"ref readonly System.Guid\" or \"Guid*\"",
        category: typeof(GuidRVAGenerator).FullName,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidGuidDescriptor = new(
        id: "GUIDGEN0003",
        title: "Invalid Guid",
        messageFormat: "The provided Guid value is not a valid Guid",
        category: typeof(GuidRVAGenerator).FullName,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PropertyHasSetterDescriptor = new(
        id: "GUIDGEN0004",
        title: "Property has setter",
        messageFormat: "The property with the [Guid] attribute must not have a setter",
        category: typeof(GuidRVAGenerator).FullName,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}