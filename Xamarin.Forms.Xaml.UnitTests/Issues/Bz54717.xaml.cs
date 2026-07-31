using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz54717 : ContentPage
	{
		public Bz54717()
		{
			InitializeComponent();
		}

		public Bz54717(bool useCompiledXaml)
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
				Application.Current = null;
			}

			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			public void Foo(bool useCompiledXaml)
			{
				Application.Current = new MockApplication
				{
					Resources = new ResourceDictionary {
						{"Color1", Color.Red},
						{"Color2", Color.Blue},
					}
				};
				var layout = new Bz54717(useCompiledXaml);
				Assert.Single(layout.Resources);
				var array = layout.Resources["SomeColors"] as Color[];
				Assert.Equal(Color.Red, array[0]);
				Assert.Equal(Color.Blue, array[1]);
			}
		}
	}
}
