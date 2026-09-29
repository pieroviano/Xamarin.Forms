using System;
using System.Runtime.CompilerServices;

namespace Xamarin.Forms.Wpf
{
	/// <summary>
	/// The <c>pack://</c> URI scheme, which WPF code and XAML name resources with. WPF registers it (in
	/// System.IO.Packaging) before any of its code runs; without the registration <c>new Uri("pack://...")</c>
	/// throws, so it is made when this assembly loads.
	/// </summary>
	internal static class PackUri
	{
		[ModuleInitializer]
		internal static void Register()
		{
			if (!UriParser.IsKnownScheme("pack"))
				UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), "pack", -1);
		}
	}
}

namespace System.Runtime.CompilerServices
{
	/// <summary>The C# 9 module initializer marker, which netstandard2.0 does not define; the compiler finds it by name.</summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	internal sealed class ModuleInitializerAttribute : Attribute
	{
	}
}
