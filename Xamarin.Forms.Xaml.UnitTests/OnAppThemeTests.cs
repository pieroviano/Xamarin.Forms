using Xunit;

using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class OnAppThemeTests : BaseTestFixture
	{
		public OnAppThemeTests()
{
			Application.Current = new MockApplication();
		}

		public override void Dispose()
{
			Application.Current = null;
			base.Dispose();
		}

		[Fact]
		public void OnAppThemeExtensionLightDarkColor()
		{
			var xaml = @"
			<Label 
			xmlns=""http://xamarin.com/schemas/2014/forms""
			xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" TextColor=""{AppThemeBinding Light = Green, Dark = Red}
			"">This text is green or red depending on Light (or default) or Dark</Label>";

			((MockPlatformServices)Device.PlatformServices).RequestedTheme = OSAppTheme.Light;
			var label = new Label().LoadFromXaml(xaml);
			Assert.Equal(Color.Green, label.TextColor);

			((MockPlatformServices)Device.PlatformServices).RequestedTheme = OSAppTheme.Dark;
			label = new Label().LoadFromXaml(xaml);
			Assert.Equal(Color.Red, label.TextColor);
		}

		[Fact]
		public void OnAppThemeLightDarkColor()
		{
			var xaml = @"
			<Label
			xmlns=""http://xamarin.com/schemas/2014/forms""
			xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
			Text=""This text is green or red depending on Light(or default) or Dark"">
                <Label.TextColor>
                    <AppThemeBinding Light=""Green"" Dark=""Red"" />
				</Label.TextColor>
			</Label> ";

			((MockPlatformServices)Device.PlatformServices).RequestedTheme = OSAppTheme.Light;
			var label = new Label().LoadFromXaml(xaml);
			Assert.Equal(Color.Green, label.TextColor);

			((MockPlatformServices)Device.PlatformServices).RequestedTheme = OSAppTheme.Dark;
			label = new Label().LoadFromXaml(xaml);
			Assert.Equal(Color.Red, label.TextColor);
		}

		[Fact]
		public void OnAppThemeUnspecifiedThemeDefaultsToLightColor()
		{
			var xaml = @"
			<Label
			xmlns=""http://xamarin.com/schemas/2014/forms""
			xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
			Text=""This text is green or red depending on Light(or default) or Dark"">
                <Label.TextColor>
                    <AppThemeBinding Light=""Green"" Dark=""Red"" />
				</Label.TextColor>
			</Label> ";

			((MockPlatformServices)Device.PlatformServices).RequestedTheme = OSAppTheme.Unspecified;
			var label = new Label().LoadFromXaml(xaml);
			Assert.Equal(Color.Green, label.TextColor);
		}

		[Fact]
		public void OnAppThemeUnspecifiedLightColorDefaultsToDefault()
		{
			var xaml = @"
			<Label
			xmlns=""http://xamarin.com/schemas/2014/forms""
			xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
			Text=""This text is green or red depending on Light(or default) or Dark"">
                <Label.TextColor>
                    <AppThemeBinding Default=""Green"" Dark=""Red"" />
				</Label.TextColor>
			</Label> ";

			((MockPlatformServices)Device.PlatformServices).RequestedTheme = OSAppTheme.Light;
			var label = new Label().LoadFromXaml(xaml);
			Assert.Equal(Color.Green, label.TextColor);
		}

		[Fact]
		public void AppThemeColorLightDark()
		{
			var xaml = @"
			<Label
			xmlns=""http://xamarin.com/schemas/2014/forms""
			xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
			Text=""This text is green or red depending on Light(or default) or Dark"">
                <Label.TextColor>
                    <AppThemeBinding Light=""Green"" Dark=""Red"" />
				</Label.TextColor>
			</Label> ";

			((MockPlatformServices)Device.PlatformServices).RequestedTheme = OSAppTheme.Light;
			var label = new Label().LoadFromXaml(xaml);
			Assert.Equal(Color.Green, label.TextColor);

			((MockPlatformServices)Device.PlatformServices).RequestedTheme = OSAppTheme.Dark;
			label = new Label().LoadFromXaml(xaml);
			Assert.Equal(Color.Red, label.TextColor);
		}

		[Fact]
		public void AppThemeColorUnspecifiedThemeDefaultsToLightColor()
		{
			var xaml = @"
			<Label
			xmlns=""http://xamarin.com/schemas/2014/forms""
			xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
			Text=""This text is green or red depending on Light(or default) or Dark"">
                <Label.TextColor>
                    <AppThemeBinding Light=""Green"" Dark=""Red"" />
				</Label.TextColor>
			</Label> ";

			((MockPlatformServices)Device.PlatformServices).RequestedTheme = OSAppTheme.Unspecified;
			var label = new Label().LoadFromXaml(xaml);
			Assert.Equal(Color.Green, label.TextColor);
		}

		[Fact]
		public void AppThemeColorUnspecifiedLightColorDefaultsToDefault()
		{
			var xaml = @"
			<Label
			xmlns=""http://xamarin.com/schemas/2014/forms""
			xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
			Text=""This text is green or red depending on Light(or default) or Dark"">
                <Label.TextColor>
                    <AppThemeBinding Default=""Green"" Dark=""Red"" />
				</Label.TextColor>
			</Label> ";

			((MockPlatformServices)Device.PlatformServices).RequestedTheme = OSAppTheme.Unspecified;
			var label = new Label().LoadFromXaml(xaml);
			Assert.Equal(Color.Green, label.TextColor);
		}
	}
}