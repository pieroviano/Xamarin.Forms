using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz51567 : ContentPage
	{
		public Bz51567()
		{
			InitializeComponent();
		}

		public Bz51567(bool useCompiledXaml)
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
			[InlineData(true)]
			[InlineData(false)]
			public void SetterWithElementValue(bool useCompiledXaml)
			{
				var page = new Bz51567(useCompiledXaml);
				var style = page.Resources["ListText"] as Style;
				var setter = style.Setters[1];
				Assert.NotNull(setter);
			}
		}
	}
}
