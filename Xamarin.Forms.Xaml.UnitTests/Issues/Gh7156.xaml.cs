using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh7156 : ContentPage
	{
		public Gh7156() => InitializeComponent();
		public Gh7156(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			public void OnPlatformDefaultToBPDefaultValue(bool useCompiledXaml)
			{
				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.Android;
				var layout = new Gh7156(useCompiledXaml);
				Assert.Equal(Label.TextProperty.DefaultValue, layout.l0.Text);
				Assert.Equal(VisualElement.WidthRequestProperty.DefaultValue, layout.l0.WidthRequest);
				Assert.Equal("bar", layout.l1.Text);
				Assert.Equal(20d, layout.l1.WidthRequest);
			}
		}
	}
}
