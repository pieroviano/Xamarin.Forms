// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class DefinitionCollectionTests : ContentPage
	{
		public DefinitionCollectionTests() => InitializeComponent();
		public DefinitionCollectionTests(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{
			[SetUp] public void Setup() => Device.PlatformServices = new MockPlatformServices();
			[TearDown] public void TearDown() => Device.PlatformServices = null;

			[Fact]
			public void DefinitionCollectionsParsedFromMarkup([Values(false, true)] bool useCompiledXaml)
			{
				var layout = new DefinitionCollectionTests(useCompiledXaml);
				var coldef = layout.grid.ColumnDefinitions;
				var rowdef = layout.grid.RowDefinitions;

				Assert.Equal(5, coldef.Count);

				Assert.Equal(new GridLength(1, GridUnitType.Star), coldef[0].Width);
				Assert.Equal(new GridLength(2, GridUnitType.Star), coldef[1].Width);
				Assert.Equal(new GridLength(1, GridUnitType.Auto), coldef[2].Width);
				Assert.Equal(new GridLength(1, GridUnitType.Star), coldef[3].Width);
				Assert.Equal(new GridLength(300, GridUnitType.Absolute), coldef[4].Width);

				Assert.Equal(5, rowdef.Count);
				Assert.Equal(new GridLength(1, GridUnitType.Star), rowdef[0].Height);
				Assert.Equal(new GridLength(1, GridUnitType.Auto), rowdef[1].Height);
				Assert.Equal(new GridLength(25, GridUnitType.Absolute), rowdef[2].Height);
				Assert.Equal(new GridLength(14, GridUnitType.Absolute), rowdef[3].Height);
				Assert.Equal(new GridLength(20, GridUnitType.Absolute), rowdef[4].Height);

			}
		}
	}
}