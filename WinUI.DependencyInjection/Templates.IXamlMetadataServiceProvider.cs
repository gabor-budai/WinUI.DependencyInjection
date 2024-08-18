﻿namespace WinUI.DependencyInjection;

internal partial class Templates
{
    public class IXamlMetadataServiceProvider
    {
        public const string Name = nameof(IXamlMetadataServiceProvider);
        public const string FullName = $"{Namespace}.{Name}";
        public const string GetRequiredService = nameof(GetRequiredService);
        public const string GetAppProvider = nameof(GetAppProvider);
        public const string Text = $$"""
// <auto-generated>

using System;

namespace {{Namespace}}
{
    /// <summary>
    /// This makes it possible to provide a custom service provider that might differ from the MS implementation.
    /// </summary>
    /// <remarks>
    /// The source generator detects which method is implemented (implicitly or explicitly). If you have a custom service provider, you can
    /// implement the GetRequiredService method and return the service from your custom service provider. The _AppProvider field, which is 
    /// generated by CSharpTypeInfoPass2.tt, might change and it will break the functionality. If it changes, you can implement the GetAppProvider.
    /// Note, the GetAppProvider should use reflection because the code that generated by the CSharpTypeInfoPass2 is not accessible.
    /// The XamlCompiler runs after the compilation but before the build ((or something like that)), so user code cannot access the generated 
    /// code because during the compilation, the code is not generated yet.
    /// </remarks>
    internal interface {{Name}} 
    {
        object {{GetRequiredService}}(Type type) => throw new NotImplementedException();

        Microsoft.UI.Xaml.Markup.IXamlMetadataProvider {{GetAppProvider}}() => throw new NotImplementedException();
    }
}

""";
    }
}
