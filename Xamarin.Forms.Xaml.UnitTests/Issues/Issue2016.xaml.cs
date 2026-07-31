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

		public class Tests
		{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void TestSwitches(bool useCompiledXaml)
			{
				var page = new Issue2016(useCompiledXaml);
				Assert.False(page.a0.IsToggled);
				Assert.False(page.b0.IsToggled);
				Assert.False(page.s0.IsToggled);
				Assert.False(page.t0.IsToggled);

				page.a0.IsToggled = true;
				page.b0.IsToggled = true;

				Assert.True(page.s0.IsToggled);
				Assert.True(page.t0.IsToggled);
			}
		}
	}
}