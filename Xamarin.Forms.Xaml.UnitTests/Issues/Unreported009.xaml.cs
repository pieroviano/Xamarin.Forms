using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Unreported009 : ContentPage
	{
		public Unreported009()
		{
			InitializeComponent();
		}

		public Unreported009(bool useCompiledXaml)
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
			[InlineData(true), InlineData(false)]
			public void AllowSetterValueAsElementProperties(bool useCompiledXaml)
			{
				var p = new Unreported009(useCompiledXaml);
				var s = p.Resources["Default"] as Style;
				Assert.Equal("Bananas!", (s.Setters[0].Value as Label).Text);
			}
		}
	}
}
