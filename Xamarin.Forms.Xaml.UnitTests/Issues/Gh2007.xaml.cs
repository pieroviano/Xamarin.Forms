using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh2007 : ContentPage
	{
		public Gh2007()
		{
			InitializeComponent();
		}

		public Gh2007(bool useCompiledXaml)
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
			public void UsefullxResourceErrorMessages(bool useCompiledXaml)
			{
				Assert.Throws<XamlParseException>(() => new Gh2007(useCompiledXaml));
			}
		}
	}
}
