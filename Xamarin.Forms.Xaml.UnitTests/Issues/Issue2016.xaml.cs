using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Issue2016 : ContentPage
	{
		public Issue2016()
		{
			InitializeComponent();
		}

		public Issue2016(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			[InlineData(false)]
			[InlineData(true)]
			public void TestSwitches(bool useCompiledXaml)
			{
				var page = new Issue2016(useCompiledXaml);
				Assert.Equal(false, page.a0.IsToggled);
				Assert.Equal(false, page.b0.IsToggled);
				Assert.Equal(false, page.s0.IsToggled);
				Assert.Equal(false, page.t0.IsToggled);

				page.a0.IsToggled = true;
				page.b0.IsToggled = true;

				Assert.Equal(true, page.s0.IsToggled);
				Assert.Equal(true, page.t0.IsToggled);
			}
		}
	}
}