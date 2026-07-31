using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh12025NavPage : NavigationPage
	{
		public static new readonly BindableProperty IconColorProperty = BindableProperty.CreateAttached("IconColor", typeof(Color), typeof(Page), Color.Default);
		public static void SetIconColor(Page page, Color barTintColor) => page.SetValue(IconColorProperty, barTintColor);
		public static Color GetIconColor(Page page) => (Color)page.GetValue(IconColorProperty);
	}

	public partial class Gh12025 : ContentPage
	{
		public Gh12025() => InitializeComponent();
		public Gh12025(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void FindMostDerivedABP(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					AssertEx.DoesNotThrow(() => MockCompiler.Compile(typeof(Gh12025)));
				var layout = new Gh12025(useCompiledXaml);
				Assert.Equal(NavigationPage.IconColorProperty.DefaultValue, NavigationPage.GetIconColor(layout));
				Assert.Equal(Color.HotPink, Gh12025NavPage.GetIconColor(layout));
			}
		}
	}
}