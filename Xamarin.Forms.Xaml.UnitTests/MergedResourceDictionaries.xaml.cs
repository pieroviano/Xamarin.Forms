using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class MergedResourceDictionaries : ContentPage
	{
		public MergedResourceDictionaries()
		{
			InitializeComponent();
		}

		public MergedResourceDictionaries(bool useCompiledXaml)
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
			[InlineData(false)]
			[InlineData(true)]
			public void MergedResourcesAreFound(bool useCompiledXaml)
			{
				MockCompiler.Compile(typeof(MergedResourceDictionaries));
				var layout = new MergedResourceDictionaries(useCompiledXaml);
				Assert.Equal("Foo", layout.label0.Text);
				Assert.Equal(Color.Pink, layout.label0.TextColor);
				Assert.Equal(Color.FromHex("#111"), layout.label0.BackgroundColor);
			}
		}
	}
}