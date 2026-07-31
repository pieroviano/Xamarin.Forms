using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class OnPlatform : ContentPage
	{
		public OnPlatform()
		{
			InitializeComponent();
		}

		public OnPlatform(bool useCompiledXaml)
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

			[InlineData(false)]
			[InlineData(true)]
			public void BoolToVisibility(bool useCompiledXaml)
			{
				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.iOS;
				var layout = new OnPlatform(useCompiledXaml);
				Assert.Equal(true, layout.label0.IsVisible);

				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.Android;
				layout = new OnPlatform(useCompiledXaml);
				Assert.Equal(false, layout.label0.IsVisible);
			}

			[InlineData(false)]
			[InlineData(true)]
			public void DoubleToWidth(bool useCompiledXaml)
			{
				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.iOS;
				var layout = new OnPlatform(useCompiledXaml);
				Assert.Equal(20, layout.label0.WidthRequest);

				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.Android;
				layout = new OnPlatform(useCompiledXaml);
				Assert.Equal(30, layout.label0.WidthRequest);
			}

			[InlineData(false)]
			[InlineData(true)]
			public void StringToText(bool useCompiledXaml)
			{
				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.iOS;
				var layout = new OnPlatform(useCompiledXaml);
				Assert.Equal("Foo", layout.label0.Text);

				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.Android;
				layout = new OnPlatform(useCompiledXaml);
				Assert.Equal("Bar", layout.label0.Text);
			}

			[InlineData(false)]
			[InlineData(true)]
			public void OnPlatformAsResource(bool useCompiledXaml)
			{
				var layout = new OnPlatform(useCompiledXaml);
				var onplat = layout.Resources["fontAttributes"] as OnPlatform<FontAttributes>;
				Assert.NotNull(onplat);
#pragma warning disable 612
				Assert.Equal(FontAttributes.Bold, onplat.iOS);
				Assert.Equal(FontAttributes.Italic, onplat.Android);
#pragma warning restore 612

			}

			[InlineData(false)]
			[InlineData(true)]
			public void OnPlatformAsResourceAreApplied(bool useCompiledXaml)
			{
				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.iOS;
				var layout = new OnPlatform(useCompiledXaml);
				var onidiom = layout.Resources["fontSize"] as OnIdiom<double>;
				Assert.NotNull(onidiom);
				Assert.IsType<double>(onidiom.Phone);
				Assert.Equal(20, onidiom.Phone);
				Assert.Equal(FontAttributes.Bold, layout.label0.FontAttributes);

				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.Android;
				layout = new OnPlatform(useCompiledXaml);
				Assert.Equal(FontAttributes.Italic, layout.label0.FontAttributes);
			}

			[InlineData(false)]
			[InlineData(true)]
			public void OnPlatform2Syntax(bool useCompiledXaml)
			{
				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.Android;
				var layout = new OnPlatform(useCompiledXaml);
				Assert.Equal(42, layout.label0.HeightRequest);

				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.iOS;
				layout = new OnPlatform(useCompiledXaml);
				Assert.Equal(21, layout.label0.HeightRequest);


				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = "FooBar";
				layout = new OnPlatform(useCompiledXaml);
				Assert.Equal(42, layout.label0.HeightRequest);
			}

			[InlineData(false)]
			[InlineData(true)]
			public void OnPlatformDefault(bool useCompiledXaml)
			{
				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = "\ud83d\ude80";
				var layout = new OnPlatform(useCompiledXaml);
				Assert.Equal(63, layout.label0.HeightRequest);
			}
		}
	}
}