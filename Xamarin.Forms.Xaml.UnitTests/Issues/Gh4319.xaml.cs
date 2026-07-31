using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh4319 : ContentPage
	{
		public Gh4319() => InitializeComponent();
		public Gh4319(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(true), InlineData(false)]
			public void OnPlatformMarkupAndNamedSizes(bool useCompiledXaml)
			{
				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.iOS;
				var layout = new Gh4319(useCompiledXaml);
				Assert.Equal(4d, layout.label.FontSize);

				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.Android;
				layout = new Gh4319(useCompiledXaml);
				Assert.Equal(8d, layout.label.FontSize);
			}
		}
	}
}