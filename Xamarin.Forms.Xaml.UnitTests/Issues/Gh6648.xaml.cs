using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh6648 : ContentPage
	{
		public Gh6648() => InitializeComponent();
		public Gh6648(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(true)]
			public void DoesntFailOnNullDataType(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					AssertEx.DoesNotThrow(() => MockCompiler.Compile(typeof(Gh6648)));
			}

			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			public void BindingsOnxNullDataTypeWorks(bool useCompiledXaml)
			{
				var layout = new Gh6648(useCompiledXaml);
				layout.stack.BindingContext = new { foo = "Foo" };
				Assert.Equal("Foo", layout.label.Text);
			}
		}
	}
}
