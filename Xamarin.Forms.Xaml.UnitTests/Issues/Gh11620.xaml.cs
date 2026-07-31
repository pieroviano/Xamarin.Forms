using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh11620 : ContentPage
	{
		public Gh11620() => InitializeComponent();
		public Gh11620(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{
			[SetUp] public void Setup() => Device.PlatformServices = new MockPlatformServices();
			[TearDown] public void TearDown() => Device.PlatformServices = null;

			[Fact]
			public void BoxOnAdd([Values(false, true)] bool useCompiledXaml)
			{
				var layout = new Gh11620(useCompiledXaml);
				var arr = layout.Resources["myArray"];
				Assert.IsType<object[]>(arr);
				Assert.Equal(3, ((object[])arr).Length);
				Assert.Equal(32, ((object[])arr)[2]);
			}
		}
	}
}