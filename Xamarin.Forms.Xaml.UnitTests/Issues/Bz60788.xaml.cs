using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz60788 : ContentPage
	{
		public Bz60788()
		{
			InitializeComponent();
		}

		public Bz60788(bool useCompiledXaml)
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
			}

			[InlineData(true), TestCase(false)]
			public void KeyedRDWithImplicitStyles(bool useCompiledXaml)
			{
				var layout = new Bz60788(useCompiledXaml);
				Assert.Equal(2, layout.Resources.Count);
				Assert.Equal(3, ((ResourceDictionary)layout.Resources["RedTextBlueBackground"]).Count);
				Assert.Equal(3, ((ResourceDictionary)layout.Resources["BlueTextRedBackground"]).Count);
			}
		}
	}
}