// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh7531 : ContentPage
	{
		public Gh7531() => InitializeComponent();
		public Gh7531(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{
			[SetUp] public void Setup() => Device.PlatformServices = new MockPlatformServices();
			[TearDown] public void TearDown() => Device.PlatformServices = null;

			[Fact]
			public void XamlOnlyResourceResolvesLocalAssembly([Values(false, true)] bool useCompiledXaml)
			{
				Gh7531 layout = null;
				Assert.DoesNotThrow(() => layout = new Gh7531(useCompiledXaml));
				var style = ((ResourceDictionary)layout.Resources["Colors"])["style"] as Style;
				Assert.Equal(typeof(Gh7531), style.TargetType);
			}
		}
	}
}