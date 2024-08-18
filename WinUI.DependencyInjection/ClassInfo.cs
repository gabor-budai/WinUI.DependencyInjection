using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;

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

    public static ClassInfo? CreateInstance(INamedTypeSymbol symbol)
    {
		// Global classes are not supported, in most cases, WinUI projects don't have global classes.
		var @namespace = symbol.ContainingNamespace.ToDisplayString();
		if (string.IsNullOrWhiteSpace(@namespace)) return null;

		return new ClassInfo()
		{
			AppMetadataProviderNamespace = @namespace,
			ClassName = symbol.Name,
			ClassFullName = symbol.ToDisplayString(),
			// WinUI's source generator uses a different namespace format ("{rootNamespace}.{projectName}_XamlTypeInfo"). 
			// If the project name is not provided, it will not be accessible from the source generator.
			// This will result in a namespace like 'MyApp.MyApp_XamlTypeInfo' or 'MyApp.OtherNamespace.OtherNamespace_XamlTypeInfo'.
			XamlTypeInfoNamespace = $"{symbol.ContainingNamespace.ToDisplayString()}.{symbol.ContainingNamespace.Name}_XamlTypeInfo",
			HasHost = symbol.GetMembers()
				.OfType<IPropertySymbol>()
				.FirstOrDefault(CheckHasHost)?.DeclaredAccessibility >= Accessibility.Internal,
			InterfaceImplementedMembers = GetInterfaceImplementations(symbol)
		};
    }

    private static bool CheckHasHost(IPropertySymbol property) => 
        property.Name == HostPropertyName && property.Type.ToDisplayString() == HostType;

	private static IReadOnlyDictionary<string, bool>? GetInterfaceImplementations(INamedTypeSymbol symbol)
	{
		var iface = symbol.AllInterfaces.FirstOrDefault(i => i.ToDisplayString() == Templates.IXamlMetadataServiceProvider.FullName);
		if (iface is null) return null;

		var hash = new Dictionary<string, IMethodSymbol>();

		foreach (var member in symbol.GetMembers().OfType<IMethodSymbol>())
		{
			if (hash.ContainsKey(member.Name)) continue;
			hash[member.Name] = member;
		}

		var dictionary = new Dictionary<string, bool>();
		foreach (var interfaceMember in iface.GetMembers().OfType<IMethodSymbol>())
		{
			if (symbol.FindImplementationForInterfaceMember(interfaceMember) is not IMethodSymbol implementedMethod) continue;
			if (implementedMethod.MethodKind == MethodKind.ExplicitInterfaceImplementation)
			{
				dictionary[interfaceMember.Name] = true;
				continue;
			}

			dictionary[interfaceMember.Name] = 
				hash.TryGetValue(interfaceMember.Name, out var classMember) && classMember.DeclaredAccessibility == interfaceMember.DeclaredAccessibility;
		}

		return new ReadOnlyDictionary<string, bool>(dictionary);
	}

    public string AppMetadataProviderNamespace { get; private set; }

    public string AppProvider =>  "_AppProvider";
 
    public string ClassName { get; private set; }

    public string ClassFullName { get; private set; }

    public string XamlTypeInfoNamespace { get; private set; }

    public bool HasHost { get; private set; }

    private IReadOnlyDictionary<string, bool>? InterfaceImplementedMembers { get; set; }

    public bool IsGetAppProviderImplemented => InterfaceImplementedMembers?.TryGetValue(Templates.IXamlMetadataServiceProvider.GetAppProvider, out var value) == true && value;

    public bool IsGetRequiredServiceImplemented => InterfaceImplementedMembers?.TryGetValue(Templates.IXamlMetadataServiceProvider.GetRequiredService, out var value) == true && value;

    public bool HasServiceProviderInterface => IsGetAppProviderImplemented || IsGetRequiredServiceImplemented;
}