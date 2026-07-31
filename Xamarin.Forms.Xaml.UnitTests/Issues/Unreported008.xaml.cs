using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Unreported008 : ContentPage
	{
		public Unreported008()
		{
			InitializeComponent();
		}

		public Unreported008(bool useCompiledXaml)
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
			[InlineData(true), InlineData(false)]
			public void PickerDateTimesAndXamlC(bool useCompiledXaml)
			{
				var page = new Unreported008(useCompiledXaml);
				var picker = page.picker0;
				Assert.Equal(DateTime.Today, picker.Date.Date);
				Assert.Equal(new DateTime(2000, 1, 1), picker.MinimumDate);
				Assert.Equal(new DateTime(2050, 12, 31), picker.MaximumDate);
			}
		}
	}
}