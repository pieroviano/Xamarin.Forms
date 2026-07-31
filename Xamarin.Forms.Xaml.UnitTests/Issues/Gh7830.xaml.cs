// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh7830 : ContentPage
	{
		public static string StaticText = "Foo";
		public Gh7830() => InitializeComponent();
		public Gh7830(bool useCompiledXaml)
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
			public void CanResolvexStaticWithShortName(bool useCompiledXaml)
			{
				var layout = new Gh7830(useCompiledXaml);
				var cell = layout.listView.ItemTemplate.CreateContent() as ViewCell;
				Assert.Equal(StaticText, (cell.View as Label).Text);
			}
		}
	}
}
