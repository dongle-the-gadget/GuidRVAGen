using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GuidRVAGen.Extensions;

internal static class AttributeDataExtensions
{
    /// <summary>
    /// Tries to get the name location of the input <see cref="AttributeData"/> instance.
    /// </summary>
    /// <param name="attributeData">The input <see cref="AttributeData"/> instance to get the name location for.</param>
    /// <returns>The resulting name location for <paramref name="attributeData"/>, if a syntax reference is available.</returns>
    public static Location? GetAttributeNameLocation(this AttributeData attributeData)
    {
        if (attributeData.ApplicationSyntaxReference is { } syntaxReference)
        {
            return syntaxReference.SyntaxTree.GetLocation(((AttributeSyntax)syntaxReference.GetSyntax()).Name.Span);
        }

        return null;
    }

    /// <summary>
    /// Tries to get the location of the constructor argument with specified index of the input <see cref="AttributeData"/> instance.
    /// </summary>
    /// <param name="attributeData">The input <see cref="AttributeData"/> instance to get the constructor argument location for.</param>
    /// <param name="index">The index of the constructor argument to get the location for.</param>
    /// <returns>The resulting constructor argument location for <paramref name="attributeData"/> at index <paramref name="index"/>, if it's available.</returns>
    public static Location? GetConstructorArgumentLocation(this AttributeData attributeData, int index)
    {
        if (attributeData.ApplicationSyntaxReference is { } syntaxReference)
        {
            AttributeSyntax attributeSyntax = (AttributeSyntax)syntaxReference.GetSyntax();
            if (attributeSyntax.ArgumentList is { } argList && argList.Arguments.Count > index)
            {
                return syntaxReference.SyntaxTree.GetLocation(argList.Arguments[index].Span);
            }
        }
        return null;
    }
}
