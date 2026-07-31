using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;
namespace Xamarin.Forms.Xaml.UnitTests
{
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class ResourceDictionaryWithInvalidSource : ContentPage
	{
		public ResourceDictionaryWithInvalidSource()
		{
			InitializeComponent();
		}

		public ResourceDictionaryWithInvalidSource(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			public void Dispose()
{
				Device.PlatformServices = null;
			}

			[Theory]
			[InlineData(false), InlineData(true)]
			public void InvalidSourceThrows(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					new BuildExceptionConstraint(8, 33).Verify(() => MockCompiler.Compile(typeof(ResourceDictionaryWithInvalidSource)));
				else
					new XamlParseExceptionConstraint(8, 33).Verify(() => new ResourceDictionaryWithInvalidSource(useCompiledXaml));
			}
		}
	}
}