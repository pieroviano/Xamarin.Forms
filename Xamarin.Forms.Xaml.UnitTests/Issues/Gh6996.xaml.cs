// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh6996 : ContentPage
	{
		public Gh6996() => InitializeComponent();
		public Gh6996(bool useCompiledXaml)
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
			public void FontImageSourceColorWithDynamicResource(bool useCompiledXaml)
			{
				var layout = new Gh6996(useCompiledXaml);
				Image image = layout.image;
				var fis = image.Source as FontImageSource;
				Assert.Equal(Color.Orange, fis.Color);

				layout.Resources["imcolor"] = layout.Resources["notBlue"];
				Assert.Equal(Color.Lime, fis.Color);
			}
		}
	}
}
