// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh2678 : ContentPage
	{
		public Gh2678() => InitializeComponent();
		public Gh2678(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
				Xamarin.Forms.Internals.Registrar.RegisterAll(new Type[0]);
			}

			[TearDown] public void TearDown() => Device.PlatformServices = null;

			[Fact]
			public void StyleClassCanBeChanged([Values(false, true)] bool useCompiledXaml)
			{
				var layout = new Gh2678(useCompiledXaml);
				var label = layout.label0;
				Assert.Equal(Color.Red, label.BackgroundColor);
				label.StyleClass = new List<string> { "two" };
				Assert.Equal(Color.Green, label.BackgroundColor);
			}
		}
	}
}
