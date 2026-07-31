// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh7837VMBase //using a base class to test #2131
	{
		public string this[int index] => "";
		public string this[string index] => "";
	}

	public class Gh7837VM : Gh7837VMBase
	{
		public new string this[int index] => index == 42 ? "forty-two" : "dull number";
		public new string this[string index] => index.ToUpper();
	}

	public partial class Gh7837 : ContentPage
	{
		public Gh7837() => InitializeComponent();
		public Gh7837(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{
			[SetUp] public void Setup() => Device.PlatformServices = new MockPlatformServices();
			[TearDown] public void TearDown() => Device.PlatformServices = null;

			[Fact]
			public void BindingWithMultipleIndexers([Values(false, true)] bool useCompiledXaml)
			{
				if (useCompiledXaml)
					MockCompiler.Compile(typeof(Gh7837));
				var layout = new Gh7837(useCompiledXaml);
				Assert.Equal("forty-two", layout.label0.Text);
				Assert.Equal("FOO", layout.label1.Text);
				Assert.Equal("forty-two", layout.label2.Text);
				Assert.Equal("FOO", layout.label3.Text);
			}
		}
	}
}
