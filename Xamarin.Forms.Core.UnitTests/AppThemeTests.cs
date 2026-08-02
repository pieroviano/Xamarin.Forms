using System;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class AppThemeTests : BaseTestFixture
	{
		public AppThemeTests()
		{
			Application.Current = new MockApplication();
		}

		[Fact]
		public void ThemeChangeUsingSetAppThemeColor()
		{
			var label = new Label
			{
				Text = "Green on Light, Red on Dark"
			};

			label.SetAppThemeColor(Label.TextColorProperty, Color.Green, Color.Red);
			Assert.Equal(Color.Green, label.TextColor);

			SetAppTheme(OSAppTheme.Dark);

			Assert.Equal(Color.Red, label.TextColor);
		}

		[Fact]
		public void ThemeChangeUsingSetAppTheme()
		{
			var label = new Label
			{
				Text = "Green on Light, Red on Dark"
			};

			label.SetOnAppTheme(Label.TextColorProperty, Color.Green, Color.Red);
			Assert.Equal(Color.Green, label.TextColor);

			SetAppTheme(OSAppTheme.Dark);

			Assert.Equal(Color.Red, label.TextColor);
		}

		[Fact]
		public void ThemeChangeUsingSetBinding()
		{
			var label = new Label
			{
				Text = "Green on Light, Red on Dark"
			};

			label.SetBinding(Label.TextColorProperty, new AppThemeBinding { Light = Color.Green, Dark = Color.Red });
			Assert.Equal(Color.Green, label.TextColor);

			SetAppTheme(OSAppTheme.Dark);

			Assert.Equal(Color.Red, label.TextColor);
		}

		void SetAppTheme(OSAppTheme theme)
		{
			((MockPlatformServices)Device.PlatformServices).RequestedTheme = theme;
			Application.Current.TriggerThemeChanged(new AppThemeChangedEventArgs(theme));
		}
	}
}