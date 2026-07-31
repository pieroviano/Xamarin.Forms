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

		public class Tests: IDisposable
		{

			public Tests() => Device.PlatformServices = new MockPlatformServices();

			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(true), InlineData(false)]
			public void RdWithSource(bool useCompiledXaml)
			{
				var layout = new Gh13209(useCompiledXaml);
				Assert.Equal(Color.Chartreuse, layout.MyRect.BackgroundColor);
				// .Count, not Assert.Single/Empty: ResourceDictionary enumeration and Count
				// differ once merged dictionaries are involved, and the NUnit original
				// asserted Count. Assert.Single() enumerates and reports the collection empty.
#pragma warning disable xUnit2013
				Assert.Equal(1, layout.Root.Resources.Count);
				Assert.Equal(0, layout.Root.Resources.MergedDictionaries.Count);
#pragma warning restore xUnit2013

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
