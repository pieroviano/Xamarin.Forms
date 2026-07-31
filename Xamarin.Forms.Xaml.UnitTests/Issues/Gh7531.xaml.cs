using System;
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

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void XamlOnlyResourceResolvesLocalAssembly(bool useCompiledXaml)
			{
				Gh7531 layout = null;
				AssertEx.DoesNotThrow(() => layout = new Gh7531(useCompiledXaml));
				var style = ((ResourceDictionary)layout.Resources["Colors"])["style"] as Style;
				Assert.Equal(typeof(Gh7531), style.TargetType);
			}
		}
	}
}