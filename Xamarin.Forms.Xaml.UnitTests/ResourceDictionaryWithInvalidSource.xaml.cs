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

		[TestFixtureAttribute]
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

			[InlineData(false), TestCase(true)]
			public void InvalidSourceThrows(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					Assert.Throws(new BuildExceptionConstraint(8, 33), () => MockCompiler.Compile(typeof(ResourceDictionaryWithInvalidSource)));
				else
					Assert.Throws(new XamlParseExceptionConstraint(8, 33), () => new ResourceDictionaryWithInvalidSource(useCompiledXaml));
			}
		}
	}
}