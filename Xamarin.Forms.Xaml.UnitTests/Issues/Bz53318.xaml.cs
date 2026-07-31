using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Bz53318ListView : ListView
	{
	}

	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class Bz53318 : ContentPage
	{
		public Bz53318()
		{
			InitializeComponent();
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

			[Fact]
			public void DoesCompilesArgsInsideDataTemplate()
			{
				Assert.DoesNotThrow(() => MockCompiler.Compile(typeof(Bz53318)));
			}
		}
	}
}