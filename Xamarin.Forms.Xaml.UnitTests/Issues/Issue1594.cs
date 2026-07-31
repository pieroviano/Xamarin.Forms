using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Issue1594
	: IDisposable{
		public Issue1594()
{
			Device.PlatformServices = new MockPlatformServices();
		}

		public void Dispose()
{
			Device.PlatformServices = null;
		}

		[Fact]
		public void OnPlatformForButtonHeight()
		{
			var xaml = @"
				<Button 
					xmlns=""http://xamarin.com/schemas/2014/forms"" 
					xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" 
					xmlns:sys=""clr-namespace:System;assembly=mscorlib""
					x:Name=""activateButton"" Text=""ACTIVATE NOW"" TextColor=""White"" BackgroundColor=""#00A0FF"">
				        <Button.HeightRequest>
				           <OnPlatform x:TypeArguments=""sys:Double""
				                   iOS=""33""
				                   Android=""44""
				                   WinPhone=""44"" />
				         </Button.HeightRequest>
				 </Button>";

			((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.iOS;
			var button = new Button().LoadFromXaml(xaml);
			Assert.Equal(33, button.HeightRequest);

			((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.Android;
			button = new Button().LoadFromXaml(xaml);
			Assert.Equal(44, button.HeightRequest);

			((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.UWP;
			button = new Button().LoadFromXaml(xaml);
			Assert.Equal(44, button.HeightRequest);
		}
	}
}