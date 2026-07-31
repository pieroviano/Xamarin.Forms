using System;
using Xunit;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz58922 : ContentPage
	{
		public Bz58922()
		{
			InitializeComponent();
		}

		public Bz58922(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			TargetIdiom defaultIdiom;
			public Tests()
{
				defaultIdiom = Device.Idiom;
			}

			public void Dispose()
{
				Device.Idiom = defaultIdiom;
				Device.PlatformServices = null;
			}

			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			public void OnIdiomXDouble(bool useCompiledXaml)
			{
				Device.Idiom = TargetIdiom.Phone;
				var layout = new Bz58922(useCompiledXaml);
				Assert.Equal(320, layout.grid.HeightRequest);
				Device.Idiom = TargetIdiom.Tablet;
				layout = new Bz58922(useCompiledXaml);
				Assert.Equal(480, layout.grid.HeightRequest);
			}
		}
	}
}