// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh12763 : ContentPage
	{
		public Gh12763() => InitializeComponent();
		public Gh12763(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{
			[SetUp] public void Setup() => Device.PlatformServices = new MockPlatformServices();
			[TearDown] public void TearDown() => Device.PlatformServices = null;

			[Fact]
			public void QuotesInStringFormat([Values(false, true)] bool useCompiledXaml)
			{
				var layout = new Gh12763(useCompiledXaml);
				Assert.Equal("\"Foo\"", layout.label.Text);
			}
		}
	}
}
