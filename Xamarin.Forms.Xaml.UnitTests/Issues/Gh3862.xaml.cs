using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh3862 : ContentPage
	{
		public Gh3862()
		{
			InitializeComponent();
		}

		public Gh3862(bool useCompiledXaml)
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
			[InlineData(false), InlineData(true)]
			public void OnPlatformMarkupInStyle(bool useCompiledXaml)
			{
				Device.PlatformServices = new MockPlatformServices { RuntimePlatform = Device.iOS };
				var layout = new Gh3862(useCompiledXaml);
				Assert.Equal(Color.Pink, layout.label.TextColor);
				Assert.False(layout.label.IsVisible);

				Device.PlatformServices = new MockPlatformServices { RuntimePlatform = Device.Android };

				layout = new Gh3862(useCompiledXaml);
				Assert.True(layout.label.IsVisible);

			}
		}
	}
}
