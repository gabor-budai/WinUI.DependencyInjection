# Why do we need this?
**The codes are representative not the real implementation**.\
The [`Template Studio`](https://github.com/microsoft/TemplateStudio)'s source generators when an empty project is created, it generates some code in the App class. 

```App.xaml.cs```:
```csharp
public partial class App : Application
{
    public IHost Host { get; } 

    public T GetService<T>() where T : class => (App)App.Current.Host.Services.GetRequiredService<T>(); 

    public App()
    {
        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((context, services) =>
            {
                // View and ViewModels registration is done by the Template Studio.
                services.AddTransient<MainViewModel>();
                services.AddTransient<MainPage>()
            })
            .Build();
    }
}
```
```ShellPage.xaml```
```xaml
<Page>
    <NavigationView x:Name="NavigationViewControl">
        <NavigationView.MenuItems>
            <NavigationViewItem x:Uid="Shell_Main1" helpers:NavigationHelper.NavigateTo="ViewModel1"/>
            <NavigationViewItem x:Uid="Shell_Main2" helpers:NavigationHelper.NavigateTo="ViewModel2"/>           
        </NavigationView.MenuItems>
    </NavigationView>
</Page>
```
WinUI, ```NavigationService.cs```:
```csharp
// For more information on navigation between pages see
// https://github.com/microsoft/TemplateStudio/blob/main/docs/WinUI/navigation.md
public class NavigationService : INavigationService
{
    public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
    {
        // The page service translates a View Model type to a View type.
        var pageType = _pageService.GetPageType(pageKey);
        var navigated = _frame.Navigate(pageType, parameter); // Type is passed!
        return navigated;
    }
}
```
WPF, ```NavigationService.cs```:
```csharp
public class NavigationService : INavigationService
{
    public bool NavigateTo(string pageKey, object parameter = null, bool clearNavigation = false)
    {
        var page = _pageService.GetPage(pageKey);
        var navigated = _frame.Navigate(page, parameter); // Instance is passed!

        return navigated;
    }
}
```
What do we see?\
Views and View Models are registered as we know them. WPF doesn't have a NavigationView; it's a custom implementation. But there's a significant difference: WinUI navigates to a type, whereas WPF navigates to an instance.
WinUI does not provide an option to pass a Page instance. Instead, WinUI creates Pages via type descriptors, which have an ActivateInstance() method responsible for creating the instance.
Here is the issue: pages are registered but they are never been activated by the Host's service provider. Views in most cases doesn't have external dependencies, but if they do we can use the App.GetService to inject any dependencies. So we don't need to register them, let WinUI create the instances, right?
```csharp
public class MyPage : Page
{
    public MyViewModel { get; } = App.GetService<MyViewModel>();
}
```
What are the benefits of activating a page via a service provider?
- The Template Studio’s generated code isn’t wrong but can be misleading, as it suggests pages are activated by the Host.
- We don’t need to break our dependency injection pattern; views and view models can be registered.
- If the view has an external dependency, we can inject it via the constructor, which makes the view more reusable.
- A view can be registered as a singleton. While view models can store view states and the view can be transient, we might still want to register a view as a singleton.
- WinUI can use typed property binding (x:Bind), but we may prefer to use DataContext instead, as it allows for automation (AddViewsHostBuilderExtensions).

```AddViewsHostBuilderExtensions.cs```:
```csharp
public static class AddViewsHostBuilderExtensions
{
    public static IHostBuilder AddViews(this IHostBuilder host) => host.ConfigureServices(services =>
    {
        services.AddView<MainPage, MainViewModel>(ServiceLifetime.Singleton);
    });
    
    private static IServiceCollection AddView<TView, TViewModel>(this IServiceCollection serviceCollection, ServiceLifetime lifetime)
        where TView : Page, new()
        where TViewModel : class
    {
        var descriptor = new ServiceDescriptor(typeof(TView), s =>
        {
            var view = s.GetRequiredService<TView>();
            view.DataContext = s.GetRequiredService<TViewModel>();
            return view;
        }, lifetime);

        serviceCollection.Add(descriptor);
       
        return serviceCollection;
    }
}
```

# How WinUI works
To determine what we should do, we first need to understand what WinUI does. WinUI's xaml compiler generates code, and we are specifically interested in the XamlTypeInfo generator ([`CSharpTypeInfoPass2.tt`](https://github.com/microsoft/microsoft-ui-xaml/blob/winui3/release/1.6-stable/src/src/XamlCompiler/BuildTasks/Microsoft/Xaml/XamlCompiler/CodeGenerators/CSharpTypeInfoPass2.tt)).
Since WinUI is written in C++, the IXamlMetadataProvider provides an interface to bridge between managed and native code. When WinUI activates a type, it queries the type's IXamlType and activates it by invoking the IXamlType.ActivateInstance method.
```XamlTypeInfo.g.cs```:
```csharp
public partial class App : IXamlMetadataProvider
{
    private XamlMetaDataProvider _AppProvider { get; }
    // XamlMetaDataProvider invokes the XamlTypeInfoProvider.GetXamlTypeByType
    public IXamlType /*IXamlMetadataProvider.*/GetXamlType(System.Type type) => _AppProvider.GetXamlType(type);
}

internal partial class XamlTypeInfoProvider
{
    // Does caching and if the type doesn't exist invokes CreateXamlType.
    public IXamlType GetXamlTypeByType(Type type);

    private IXamlType CreateXamlType(int typeIndex)
    {
        switch (typeIndex)
        {
            case x: 
                userType = new XamlUserType(this, typeName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
                // If there is no parameterless constructor, the Activator won't be assigned and later it will trhow an excpetion.
                userType.Activator = Activate_x_MainPage;
                break;
        }
    }

    private object Activate_x_MainPage() { return new MainPage(); } 
}

internal class XamlUserType : IXamlType
{
    // The Activator's type is a delegate (delegate void Activator()).
    public Activator Activator { get; set; }

    public object /*IXamlType.*/ActivateInstance() // Hooray, here we are! Here's the constructor.
    {
        return Activator(); 
    }
}

```

# How the source generator works
Fortunately, the IXamlMetadataProvider interface is implemented implicitly in the App class, which allows us to explicitly implement the interface's methods. We we can add custom code, but we need to access the generated implementation.
In the generated App class, we can see that the interface methods simply call the provider's methods. We can do the same get IXamlType via the _AppProvider and create custom activators, right?\
A WinUI project compilation is divided into multiple parts (I'm not entirely sure, but I think so). The compiler first compiles the C# code, then the xaml compiler compiles the xaml code and generates some code, and finally, the C# compiler compiles the generated code (at least, that’s my understanding). As a result, the generated code is not accessible by any user code because the user code is compiled before the generation.\
This isn't a big problem, though, because we can access the _AppProvider via reflection.\
The App must have an Host property (Template Studio generates it), and this can be used to activate types. If you have a custom service provider or if WinUI team changes the text template, you can implement the ```WinUI.DependencyInjection.IXamlMetadataServiceProvider``` interface in the App. 
```csharp
internal interface IXamlMetadataServiceProvider
{
    public object GetRequiredService(Type type); 
    public IXamlMetadataProvider GetAppProvider();  
}
```
These methods have default implementation, so you don't need to implement both; the source generator will decide which is the suitable solution.\
The generated code redirects the GetXamlType methods that way we create the ServiceProviderXamlUserType which activates any page via the service provider.

```App.xaml.XamlMetadataServiceProvider.g.cs```:
```csharp
partial class App : IXamlMetadataProvider
{
    // ...
    private IXamlMetadataProvider _DiXamlMetadataProvider => __diXamlMetadataProvider ??= new XamlMetadataServiceProvider(this);
    // The interface meber are implemented explicitly, so we can 'override' the default implementation.
    IXamlType IXamlMetadataProvider.GetXamlType(Type type) => _DiXamlMetadataProvider.GetXamlType(type);   
    // ...
}

file sealed class XamlMetadataServiceProvider : IXamlMetadataProvider
{
    // ...
    private App App { get; }
    private IXamlMetadataProvider AppProvider { get; }
    // Initializes the App and the AppProvider.
    public XamlMetadataServiceProvider(WinUI.Extensions.SourceGenerators.DependencyInjection.TestApp.App app);
    public object GetRequiredService(Type type);
    private bool TryGetServiceProviderXamlUserType(IXamlType? xamlType, out IXamlType? serviceProviderXamlUserType)
    {
        serviceProviderXamlUserType = null;
        // Frame.Navigate instantiates pages only, so only Microsoft.UI.Xaml.Controls.Page or its subclasses are redirected.
        if (xamlType?.UnderlyingType.IsSubclassOf(typeof(Microsoft.UI.Xaml.Controls.Page)) is null or false) return false;
        serviceProviderXamlUserType = new ServiceProviderXamlUserType(this, xamlType);
        return true;
    }

    public IXamlType GetXamlType(Type type)
    {
        var xamlType = AppProvider.GetXamlType(type);
        if (TryGetServiceProviderXamlUserType(xamlType, out var serviceProviderXamlUserType)) return serviceProviderXamlUserType;
        return xamlType;
    }
    // ...
}

file sealed class ServiceProviderXamlUserType : IXamlType
{
    public ServiceProviderXamlUserType(XamlMetadataServiceProvider provider, IXamlType xamlUserType);

    public IXamlType XamlUserType { get; }
    public WinUI.Extensions.SourceGenerators.DependencyInjection.TestApp.TestApp_XamlTypeInfo.XamlMetadataServiceProvider Provider { get; }
    public object ActivateInstance() => Provider.GetRequiredService(UnderlyingType); // Hooray, we can use the service provider!

    // Other interface properties or methods does the same.
    public IXamlType BaseType => XamlUserType.BaseType;
    // ...
}
```
