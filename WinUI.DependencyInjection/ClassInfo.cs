using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace WinUI.DependencyInjection;

public class ClassInfo
{
    private const string HostPropertyName = "Host";
    private const string HostType = "Microsoft.Extensions.Hosting.IHost";

#nullable disable
    private ClassInfo()
    {

    }
#nullable enable

    public static ClassInfo? CreateInstance(ClassDeclarationSyntax classDeclaration, INamedTypeSymbol symbol)
    {
        var info = new ClassInfo();
        info.AppMetadataProviderNamespace = symbol.ContainingNamespace.ToDisplayString();
        // Global classes are not supported, in most cases WinUI projects are don't have global classes.
        if (string.IsNullOrWhiteSpace(info.AppMetadataProviderNamespace)) return null;
        info.ClassName = symbol.Name;
        info.ClassFullName = symbol.ToDisplayString();

        // WinUI's source generator uses a different namespace format ("{rootNamespace}.{projectName}_XamlTypeInfo"). 
        // If the project name is not provided, it will not be accessible from the source generator.
        // This will result in a namespace like 'MyApp.MyApp_XamlTypeInfo' or 'MyApp.OtherNamespace.OtherNamespace_XamlTypeInfo'.
        info.XamlTypeInfoNamespace = $"{symbol.ContainingNamespace.ToDisplayString()}.{symbol.ContainingNamespace.Name}_XamlTypeInfo";
        info.HasHost = symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(CheckHasHost)?.DeclaredAccessibility >= Accessibility.Internal;

        var iface = symbol.AllInterfaces.FirstOrDefault(i => i.ToDisplayString() == Templates.IXamlMetadataServiceProvider.FullName);
        if (iface is null) return info;

        var hash = symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Select(m => m.Name)
            .ToImmutableHashSet();

        info.InterfaceImplementedMembers = new Dictionary<string, bool>();

        foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
        {
            if (symbol.FindImplementationForInterfaceMember(member) is not IMethodSymbol implementedMethod) continue;
            if (implementedMethod.MethodKind == MethodKind.ExplicitInterfaceImplementation)
            {
                info.InterfaceImplementedMembers[member.Name] = true;
                continue;
            }
            info.InterfaceImplementedMembers[member.Name] = hash.Contains(member.Name);
        }

        return info;
    }

    private static bool CheckHasHost(IPropertySymbol property) => 
        property.Name == HostPropertyName && property.Type.ToDisplayString() == HostType;

    public string AppMetadataProviderNamespace { get; private set; }

    public string AppProvider =>  "_AppProvider";
 
    public string ClassName { get; private set; }

    public string ClassFullName { get; private set; }

    public string XamlTypeInfoNamespace { get; private set; }

    public bool HasHost { get; private set; }

    private IDictionary<string, bool>? InterfaceImplementedMembers { get; set; }

    public bool IsGetAppProviderImplemented => InterfaceImplementedMembers?.TryGetValue(Templates.IXamlMetadataServiceProvider.GetAppProvider, out var value) == true && value;

    public bool IsGetRequiredServiceImplemented => InterfaceImplementedMembers?.TryGetValue(Templates.IXamlMetadataServiceProvider.GetRequiredService, out var value) == true && value;

    public bool HasServiceProviderInterface => IsGetAppProviderImplemented || IsGetRequiredServiceImplemented;
}