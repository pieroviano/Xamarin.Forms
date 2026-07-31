using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh3280 : ContentPage
	{
		public Gh3280()
		{
			InitializeComponent();
		}

		public Size Foo { get; set; }

		public Gh3280(bool useCompiledXaml)
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

			[InlineData(false), TestCase(true)]
			public void SizeHasConverter(bool useCompiledXaml)
			{
				Gh3280 layout = null;
				Assert.DoesNotThrow(() => layout = new Gh3280(useCompiledXaml));
				Assert.Equal(new Size(15, 25), layout.Foo);
			}
		}
	}
}
