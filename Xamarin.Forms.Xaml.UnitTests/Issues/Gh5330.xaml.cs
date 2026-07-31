using System;
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

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(true)]
			public void DoesntFailOnxType(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					AssertEx.DoesNotThrow(() => MockCompiler.Compile(typeof(Gh5330)));
			}

			[Theory]
			[InlineData(true)]
			public void CompiledBindingWithxType(bool useCompiledXaml)
			{
				var layout = new Gh5330(useCompiledXaml) { BindingContext = new Button { Text = "Foo" } };
				Assert.Equal("Foo", layout.label.Text);
			}
		}
	}
}
