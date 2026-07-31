using System;
using System.Collections.Generic;

using Xunit;

using Xamarin.Forms;
using Xamarin.Forms.Build.Tasks;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class Gh4099 : ContentPage
	{
		public Gh4099()
		{
			InitializeComponent();
		}

		public Gh4099(bool useCompiledXaml)
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
			public void BetterExceptionReport(bool useCompiledXaml)
			{
				if (useCompiledXaml)
				{
					try
					{
						MockCompiler.Compile(typeof(Gh4099));
					}
					catch (BuildException xpe)
					{
						Assert.Equal(5, xpe.XmlInfo.LineNumber);
						Assert.Pass();
					}
					Assert.Fail();
				}
			}
		}
	}
}
