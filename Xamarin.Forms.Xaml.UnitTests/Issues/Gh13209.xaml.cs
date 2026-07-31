using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh13209 : ContentPage
	{
		public Gh13209() => InitializeComponent();
		public Gh13209(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{

			[SetUp] public void Setup() => Device.PlatformServices = new MockPlatformServices();

			[TearDown] public void TearDown() => Device.PlatformServices = null;

			[InlineData(true), TestCase(false)]
			public void RdWithSource(bool useCompiledXaml)
			{
				var layout = new Gh13209(useCompiledXaml);
				Assert.Equal(Color.Chartreuse, layout.MyRect.BackgroundColor);
				Assert.Equal(1, layout.Root.Resources.Count);
				Assert.Equal(0, layout.Root.Resources.MergedDictionaries.Count);

				Assert.NotNull(layout.Root.Resources["Color1"]);
				Assert.True(layout.Root.Resources.Remove("Color1"));
				Assert.Throws<KeyNotFoundException>(() =>
				{
					var _ = layout.Root.Resources["Color1"];
				});

			}
		}
	}
}
