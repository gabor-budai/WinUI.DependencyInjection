namespace WinUI.DependencyInjection;

public partial class Templates
{
	public const string Namespace = $"{nameof(WinUI)}.{nameof(DependencyInjection)}";
	public const string DebuggerNonUserCodeAttribute = "[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]";

	private static string? _generatedCodeAttribute;
	public static string GeneratedCodeAttribute
	{
		get
		{
			if (_generatedCodeAttribute is not null) return _generatedCodeAttribute;
			var type = typeof(XamlMetadataServiceProviderGenerator);
			var tool = type.FullName!;
			var toolVersion = type.Assembly.GetName().Version!.ToString();
			return _generatedCodeAttribute = $"[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"{tool}\", \"{toolVersion}\")]";
		}
	}
}
