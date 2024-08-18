using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using TestApp.Activation;
using TestApp.Contracts.Services;
using TestApp.Services;
using TestApp.ViewModels;
using TestApp.Views;

namespace TestApp;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.

[WinUI.DependencyInjection.XamlMetadataServiceProvider]
public partial class App : Application, WinUI.DependencyInjection.IXamlMetadataServiceProvider
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

	// See how the generated code changes when you uncomment the methods (Analyzers->WinUI.DependencyInjection->App.xaml.XamlMetadataServiceProvider.g.cs).
	// Explicit declaration is fine, rename the host and use this method the code will work. (GetRequiredService changes)
	// object WinUI.DependencyInjection.IXamlMetadataServiceProvider.GetRequiredService(Type type) => Host.Services.GetRequiredService(type);

	// Implicit declaration is fine, an example implementation is provided below. (ctor changes)
	// public Microsoft.UI.Xaml.Markup.IXamlMetadataProvider GetAppProvider() => (Microsoft.UI.Xaml.Markup.IXamlMetadataProvider)typeof(App).GetProperty("_AppProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(this)!;

	// It's not the interface implementation; it's never invoked by the generated code.
	// private object GetRequiredService(Type type) => Host.Services.GetRequiredService(type);

	public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public static UIElement? AppTitlebar { get; set; }

    public App()
    {
        InitializeComponent();
		Host.Services.GetRequiredService(null!);
        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers

            // Services
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Views and ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
        }).
        Build();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        await App.GetService<IActivationService>().ActivateAsync(args);
    }
}
