using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh11541 : ContentPage
	{
		public Gh11541() => InitializeComponent();
		public Gh11541(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{
			[SetUp] public void Setup() => Device.PlatformServices = new MockPlatformServices();

			[TearDown] public void TearDown() => Device.PlatformServices = null;

			[Fact]
			public void RectangleGeometryDoesntThrow([Values(false, true)] bool useCompiledXaml)
			{
				Assert.DoesNotThrow(() => new Gh11541(useCompiledXaml));
			}
		}
	}
}
