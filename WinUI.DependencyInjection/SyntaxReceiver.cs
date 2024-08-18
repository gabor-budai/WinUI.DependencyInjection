using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace WinUI.DependencyInjection;

internal class SyntaxReceiver : ISyntaxReceiver
{
    public IList<ClassDeclarationSyntax> CandidateClasses { get; } = [];

    private static string AttributeName { get; } = Templates.XamlMetadataServiceProviderAttribute.Name.Replace("Attribute", "");

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not ClassDeclarationSyntax classDeclarationSyntax ||
            !classDeclarationSyntax.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString().Contains(AttributeName))) return;

        CandidateClasses.Add(classDeclarationSyntax);
    }
}
