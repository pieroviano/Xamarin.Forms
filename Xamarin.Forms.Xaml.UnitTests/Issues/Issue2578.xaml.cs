using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Issue2578 : ContentPage
	{
		public Issue2578()
		{
			InitializeComponent();
		}

		public Issue2578(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			[InlineData(false)]
			[InlineData(true)]
			public void MultipleTriggers(bool useCompiledXaml)
			{
				Issue2578 layout = null;
				Assert.DoesNotThrow(() => layout = new Issue2578(useCompiledXaml));

				Assert.Equal(null, layout.label.Text);
				Assert.Equal(Color.Default, layout.label.BackgroundColor);
				Assert.Equal(Color.Olive, layout.label.TextColor);
				layout.label.Text = "Foo";
				Assert.Equal(Color.Red, layout.label.BackgroundColor);
			}
		}
	}
}