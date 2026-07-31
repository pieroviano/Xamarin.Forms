using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz55347 : ContentPage
	{
		public Bz55347()
		{
		}

		public Bz55347(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			public void Dispose()
{
				Device.PlatformServices = null;
				Application.Current = null;
			}

			[InlineData(true)]
			[InlineData(false)]
			public void PaddingThicknessResource(bool useCompiledXaml)
			{
				Application.Current = new MockApplication
				{
					Resources = new ResourceDictionary {
						{"Padding", new Thickness(8)}
					}
				};
				var layout = new Bz55347(useCompiledXaml);
			}
		}
	}
}