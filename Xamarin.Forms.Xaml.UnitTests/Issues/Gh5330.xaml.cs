using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh5330 : ContentPage
	{
		public Gh5330() => InitializeComponent();
		public Gh5330(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{
			[SetUp] public void Setup() => Device.PlatformServices = new MockPlatformServices();
			[TearDown] public void TearDown() => Device.PlatformServices = null;

			[Fact]
			public void DoesntFailOnxType([Values(true)] bool useCompiledXaml)
			{
				if (useCompiledXaml)
					Assert.DoesNotThrow(() => MockCompiler.Compile(typeof(Gh5330)));
			}

			[Fact]
			public void CompiledBindingWithxType([Values(true)] bool useCompiledXaml)
			{
				var layout = new Gh5330(useCompiledXaml) { BindingContext = new Button { Text = "Foo" } };
				Assert.Equal("Foo", layout.label.Text);
			}
		}
	}
}
