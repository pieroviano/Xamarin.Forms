using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh6192 : ContentPage
	{
		public Gh6192() => InitializeComponent();
		public Gh6192(bool useCompiledXaml)
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
			public void XamlCDoesntFail(bool useCompiledXaml)
			{
				var layout = new Gh6192(useCompiledXaml);
				layout.BindingContext = new
				{
					Items = new[] {
						new {
							Options = new [] { "Foo", "Bar" },
						}
					}
				};
				var lv = (layout.bindableStackLayout.Children[0] as ContentView).Content as ListView;
				lv.ItemTemplate.CreateContent();
			}
		}
	}
}