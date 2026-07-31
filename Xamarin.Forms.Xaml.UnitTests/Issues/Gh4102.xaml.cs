using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh4102VM
	{
		public Gh4102VM SomeNullValue { get; set; }
		public string SomeProperty { get; set; } = "Foo";
	}

	public partial class Gh4102 : ContentPage
	{
		public Gh4102() => InitializeComponent();

		public Gh4102(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{
			[SetUp] public void Setup() => Device.PlatformServices = new MockPlatformServices();
			[TearDown] public void TearDown() => Device.PlatformServices = null;

			[InlineData(true), TestCase(false)]
			public void CompiledBindingsNullInPath(bool useCompiledXaml)
			{
				var layout = new Gh4102(useCompiledXaml) { BindingContext = new Gh4102VM() };
				Assert.Equal(null, layout.label.Text);
			}
		}
	}
}
