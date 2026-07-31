using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh4103VM
	{
		public string SomeNullableValue { get; set; } = "initial";
	}

	public partial class Gh4103 : ContentPage
	{
		public Gh4103() => InitializeComponent();

		public Gh4103(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(true), InlineData(false)]
			public void CompiledBindingsTargetNullValue(bool useCompiledXaml)
			{
				var layout = new Gh4103(useCompiledXaml) { BindingContext = new Gh4103VM() };
				Assert.Equal("initial", layout.label.Text);

				layout.BindingContext = new Gh4103VM { SomeNullableValue = null };
				Assert.Equal("target null", layout.label.Text);
			}
		}
	}
}