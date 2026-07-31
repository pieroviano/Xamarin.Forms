using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class ResourceDictionaryWithSource : ContentPage
	{
		public ResourceDictionaryWithSource()
		{
			InitializeComponent();
		}

		public ResourceDictionaryWithSource(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(false), InlineData(true)]
			public void RDWithSourceAreFound(bool useCompiledXaml)
			{
				var layout = new ResourceDictionaryWithSource(useCompiledXaml);
				Assert.Equal(Color.Pink, layout.label.TextColor);
			}

			[Theory]
			[InlineData(false), InlineData(true)]
			public void RelativeAndAbsoluteURI(bool useCompiledXaml)
			{
				var layout = new ResourceDictionaryWithSource(useCompiledXaml);
				Assert.Equal(new Uri("./SharedResourceDictionary.xaml;assembly=Xamarin.Forms.Xaml.UnitTests", UriKind.Relative), ((ResourceDictionary)layout.Resources["relURI"]).Source);
				Assert.IsType<Style>(((ResourceDictionary)layout.Resources["relURI"])["sharedfoo"]);
				Assert.Equal(new Uri("/SharedResourceDictionary.xaml;assembly=Xamarin.Forms.Xaml.UnitTests", UriKind.Relative), ((ResourceDictionary)layout.Resources["absURI"]).Source);
				Assert.IsType<Style>(((ResourceDictionary)layout.Resources["absURI"])["sharedfoo"]);
				Assert.Equal(new Uri("SharedResourceDictionary.xaml;assembly=Xamarin.Forms.Xaml.UnitTests", UriKind.Relative), ((ResourceDictionary)layout.Resources["shortURI"]).Source);
				Assert.IsType<Style>(((ResourceDictionary)layout.Resources["shortURI"])["sharedfoo"]);
				Assert.IsType<Color>(((ResourceDictionary)layout.Resources["Colors"])["MediumGrayTextColor"]);
				Assert.IsType<Color>(((ResourceDictionary)layout.Resources["CompiledColors"])["MediumGrayTextColor"]);
			}

			[Fact]
			public void XRIDIsGeneratedForRDWithoutCodeBehind()
			{
				var asm = typeof(ResourceDictionaryWithSource).Assembly;
				var resourceId = XamlResourceIdAttribute.GetResourceIdForPath(asm, "AppResources/Colors.xaml");
				Assert.NotNull(resourceId);
				var type = XamlResourceIdAttribute.GetTypeForResourceId(asm, resourceId);
				Assert.Null(type);
			}

			[Fact]
			public void CodeBehindIsGeneratedForRDWithXamlComp()
			{
				var asm = typeof(ResourceDictionaryWithSource).Assembly;
				var resourceId = XamlResourceIdAttribute.GetResourceIdForPath(asm, "AppResources/CompiledColors.xaml");
				Assert.NotNull(resourceId);
				var type = XamlResourceIdAttribute.GetTypeForResourceId(asm, resourceId);
				Assert.NotNull(type);
				var rd = Activator.CreateInstance(type);
				Assert.NotNull(rd as ResourceDictionary);
			}

			[Theory]
			[InlineData(false), InlineData(true)]
			public void LoadResourcesWithAssembly(bool useCompiledXaml)
			{
				var layout = new ResourceDictionaryWithSource(useCompiledXaml);
				Assert.Equal(new Uri("/AppResources/Colors.xaml;assembly=Xamarin.Forms.Xaml.UnitTests", UriKind.Relative), ((ResourceDictionary)layout.Resources["inCurrentAssembly"]).Source);
				Assert.IsType<Color>(((ResourceDictionary)layout.Resources["inCurrentAssembly"])["MediumGrayTextColor"]);
				Assert.Equal(new Uri("/AppResources.xaml;assembly=Xamarin.Forms.Controls", UriKind.Relative), ((ResourceDictionary)layout.Resources["inOtherAssembly"]).Source);
				Assert.IsType<Color>(((ResourceDictionary)layout.Resources["inOtherAssembly"])["notBlue"]);
			}

		}
	}
}