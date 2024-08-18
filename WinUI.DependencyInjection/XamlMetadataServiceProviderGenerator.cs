using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;

namespace WinUI.DependencyInjection;

[Generator(LanguageNames.CSharp)]
public class XamlMetadataServiceProviderGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {

#if DEBUG
		//if (!System.Diagnostics.Debugger.IsAttached)
		//{
		//	System.Diagnostics.Debugger.Launch();
		//}
#endif

		context.RegisterForPostInitialization((i) =>
        {
            i.AddSource($"{Templates.XamlMetadataServiceProviderAttribute.Name}.g.cs", Templates.XamlMetadataServiceProviderAttribute.Text);
            i.AddSource($"{Templates.IXamlMetadataServiceProvider.Name}.g.cs", Templates.IXamlMetadataServiceProvider.Text);
        });

        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxReceiver receiver) return;

        var languageVersion = (context.Compilation as CSharpCompilation)?.LanguageVersion ?? LanguageVersion.CSharp1;
        
        foreach (var classDeclarationSyntax in receiver.CandidateClasses)
        {
            var classSymbol = context.Compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree).GetDeclaredSymbol(classDeclarationSyntax);
            if (classSymbol?.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == Templates.XamlMetadataServiceProviderAttribute.FullName) is null or false) continue;

            var classInfo = ClassInfo.CreateInstance(classSymbol);
            if (classInfo is null) continue;

			var syntaxTree = classDeclarationSyntax.SyntaxTree;
			context.AddSource(GetFileName(syntaxTree.FilePath), SourceText.From(GenerateCode(languageVersion, classInfo), syntaxTree.Encoding));
        }
    }

    private static string GetFileName(string path) => $"{Path.GetFileNameWithoutExtension(path)}.XamlMetadataServiceProvider.g.cs";

    private static string GenerateCode(LanguageVersion languageVersion, ClassInfo classInfo)
    {
        // Yes, it could have been an interpolated string, but T4 was easier for me.
        var text = new XamlMetadataServiceProviderCodeGenerator()
        {
            Model = classInfo,
            Visibility = languageVersion >= LanguageVersion.CSharp11 ? "file" : "internal",
        }.TransformText();

        return text;
    }
}
