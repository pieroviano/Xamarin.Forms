using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Build.Tasks;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class Gh3512 : ContentPage
	{
		public Gh3512()
		{
			InitializeComponent();
		}

		public Gh3512(bool useCompiledXaml)
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
			public void ThrowsOnDuplicateXKey(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					Assert.Throws<BuildException>(() => MockCompiler.Compile(typeof(Gh3512)));
				else
					Assert.Throws<ArgumentException>(() => new Gh3512(useCompiledXaml));
			}
		}
	}
}
