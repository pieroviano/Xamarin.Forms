using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Bz27968Page : ContentPage
	{
	}

	public partial class Bz27968 : Bz27968Page
	{
		public Bz27968()
		{
			InitializeComponent();
		}

		public Bz27968(bool useCompiledXaml)
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

			[InlineData(true)]
			[InlineData(false)]
			public void BaseClassIdentifiersAreValidForResources(bool useCompiledXaml)
			{
				var layout = new Bz27968(useCompiledXaml);
				Assert.IsType<ListView>(layout.Resources["listView"]);
			}
		}
	}
}
