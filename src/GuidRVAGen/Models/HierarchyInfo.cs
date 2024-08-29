﻿using GuidRVAGen.Extensions;
using GuidRVAGen.Helpers;
using Microsoft.CodeAnalysis;
using System;

namespace GuidRVAGen.Models;

/// <summary>
/// A model describing the hierarchy info for a specific type.
/// </summary>
/// <param name="FullyQualifiedMetadataName">The fully qualified metadata name for the current type.</param>
/// <param name="Namespace">Gets the namespace for the current type.</param>
/// <param name="Hierarchy">Gets the sequence of type definitions containing the current type.</param>
internal sealed partial record HierarchyInfo(string FullyQualifiedMetadataName, string Namespace, EquatableArray<TypeInfo> Hierarchy, bool AllowsUnsafe)
{
    /// <summary>
    /// Creates a new <see cref="HierarchyInfo"/> instance from a given <see cref="INamedTypeSymbol"/>.
    /// </summary>
    /// <param name="typeSymbol">The input <see cref="INamedTypeSymbol"/> instance to gather info for.</param>
    /// <returns>A <see cref="HierarchyInfo"/> instance describing <paramref name="typeSymbol"/>.</returns>
    public static HierarchyInfo From(INamedTypeSymbol typeSymbol, bool AllowsUnsafe)
    {
        using ImmutableArrayBuilder<TypeInfo> hierarchy = new();

        for (INamedTypeSymbol? parent = typeSymbol;
             parent is not null;
             parent = parent.ContainingType)
        {
            hierarchy.Add(new TypeInfo(
                parent.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                parent.TypeKind,
                parent.IsRecord));
        }

        return new(
            typeSymbol.GetFullyQualifiedMetadataName(),
            typeSymbol.ContainingNamespace.ToDisplayString(new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)),
            hierarchy.ToImmutable(),
            AllowsUnsafe);
    }

    /// <summary>
    /// Writes syntax for the current hierarchy into a target writer.
    /// </summary>
    /// <typeparam name="T">The type of state to pass to callbacks.</typeparam>
    /// <param name="state">The input state to pass to callbacks.</param>
    /// <param name="writer">The target <see cref="IndentedTextWriter"/> instance to write text to.</param>
    /// <param name="baseTypes">A list of base types to add to the generated type, if any.</param>
    /// <param name="memberCallbacks">The callbacks to use to write members into the declared type.</param>
    public void WriteSyntax<T>(
        T state,
        IndentedTextWriter writer,
        ReadOnlySpan<string> baseTypes,
        ReadOnlySpan<IndentedTextWriter.Callback<T>> memberCallbacks)
    {
        // Write the generated file header
        writer.WriteLine("// <auto-generated/>");
        writer.WriteLine("#pragma warning disable");
        writer.WriteLine();

        // Declare the namespace, if needed
        if (Namespace.Length > 0)
        {
            writer.WriteLine($"namespace {Namespace}");
            writer.WriteLine("{");
            writer.IncreaseIndent();


            // Declare all the opening types until the inner-most one
            for (int i = Hierarchy.Length - 1; i >= 0; i--)
            {
                writer.WriteLine($$"""/// <inheritdoc cref="{{Hierarchy[i].QualifiedName}}"/>""");

                if (AllowsUnsafe)
                    writer.Write("unsafe ");

                writer.Write($$"""partial {{Hierarchy[i].GetTypeKeyword()}} {{Hierarchy[i].QualifiedName}}""");

                // Add any base types, if needed
                if (i == 0 && !baseTypes.IsEmpty)
                {
                    writer.Write(" : ");
                    writer.WriteInitializationExpressions(baseTypes, static (item, writer) => writer.Write(item));
                    writer.WriteLine();
                }
                else
                {
                    writer.WriteLine();
                }

                writer.WriteLine($$"""{""");
                writer.IncreaseIndent();
            }

            // Generate all nested members
            writer.WriteLineSeparatedMembers(memberCallbacks, (callback, writer) => callback(state, writer));

            // Close all scopes and reduce the indentation
            for (int i = 0; i < Hierarchy.Length; i++)
            {
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }

            // Close the namespace scope as well, if needed
            if (Namespace.Length > 0)
            {
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
        }
    }

    /// <summary>
    /// Gets the fully qualified type name for the current instance.
    /// </summary>
    /// <returns>The fully qualified type name for the current instance.</returns>
    public string GetFullyQualifiedTypeName()
    {
        using ImmutableArrayBuilder<char> fullyQualifiedTypeName = new();

        fullyQualifiedTypeName.AddRange("global::".AsSpan());

        if (Namespace.Length > 0)
        {
            fullyQualifiedTypeName.AddRange(Namespace.AsSpan());
            fullyQualifiedTypeName.Add('.');
        }

        fullyQualifiedTypeName.AddRange(Hierarchy[^1].QualifiedName.AsSpan());

        for (int i = Hierarchy.Length - 2; i >= 0; i--)
        {
            fullyQualifiedTypeName.Add('.');
            fullyQualifiedTypeName.AddRange(Hierarchy[i].QualifiedName.AsSpan());
        }

        return fullyQualifiedTypeName.ToString();
    }
}
