using System;
using Xunit;

using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh4572 : ContentPage
	{
		public Gh4572() => InitializeComponent();
		public Gh4572(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(true), InlineData(false)]
			public void BindingAsElement(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					AssertEx.DoesNotThrow(() => MockCompiler.Compile(typeof(Gh4572)));
				var layout = new Gh4572(useCompiledXaml) { BindingContext = new { labeltext = "Foo" } };
				Assert.Equal("Foo", layout.label.Text);
			}
		}
	}
}
