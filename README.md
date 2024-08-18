# WinUI Dependency Injection Source Generator
This source generator makes possible to instantiate a ```Micrsoft.UI.Xaml.Controls.Page``` via a service provider.

# Why and how
You can find the detailed explanation in [here](https://github.com/gabor-budai/WinUI.DependencyInjection/blob/master/README_DETAIL.md).

# Usage
The source generator generates the ```XamlMetadataServiceProviderAttribute``` attribute and App class must have this attribute to override the default XamlMetadaProvider.
```App.xaml.cs```:
```csharp
[WinUI.DependencyInjection.XamlMetadataServiceProvider]
public partial class App : Application
{
    public IHost Host { get; }

    // If you want to make sure Pages activated by the provider then register them like this:
    // ...
    .ConfigureServices((hostContext, services) =>
    {
        services.AddTransient(() =>
        {
            Debugger.Break();
            return new MainPage();
        });
    });
}
```
And the xaml compiler generated code. 
```XamlTypeInfo.g.cs```:
```csharp
public partial class App : IXamlMetadataProvider
{
    private XamlMetaDataProvider _AppProvider { get; }
}
```
If you have a custom service provider or if the xaml compiler generated code changes, you can implement the ```WinUI.DependencyInjection.IXamlMetadataServiceProvider``` interface in the App.
You don't need to implement both (the interface has default imlementations) methods, the source generator will decide which is the suitable solution.
```App.xaml.cs```:
```csharp
[WinUI.DependencyInjection.XamlMetadataServiceProvider]
public partial class App : WinUI.DependencyInjection.IXamlMetadataServiceProvider
{

    public object GetRequiredService(Type type) 
    {
        // Your service provider.
    }

    // The generated provider has an _AppProvider property. If the WinUI team changes its name, you can update it with the new name here.
    public IXamlMetadataProvider GetAppProvider() => typeof(App).GetProperty("NewAppProviderName", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);
}
```