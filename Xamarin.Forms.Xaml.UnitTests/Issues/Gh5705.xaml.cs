using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh5705 : Shell
	{
		public Gh5705() => InitializeComponent();
		public Gh5705(bool useCompiledXaml)
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
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void SearchHandlerIneritBC(bool useCompiledXaml)
			{
				var vm = new object();
				var shell = new Gh5705(useCompiledXaml) { BindingContext = vm };
				Assert.Equal(vm, shell.searchHandler.BindingContext);
			}
		}
	}
}
