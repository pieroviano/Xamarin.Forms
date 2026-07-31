// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;
using Xamarin.Forms.Xaml.Diagnostics;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class BindingDiagnosticsTests : ContentPage
	{
		public BindingDiagnosticsTests() => InitializeComponent();

		public BindingDiagnosticsTests(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[SetUp] public void Setup() => Device.PlatformServices = new MockPlatformServices();

			[TearDown] public void TearDown() => Device.PlatformServices = null;

			[InlineData(false)]
			//[InlineData(true)]
			public void Test(bool useCompiledXaml)
			{
				List<BindingBaseErrorEventArgs> failures = new List<BindingBaseErrorEventArgs>();
				BindingDiagnostics.BindingFailed += (o, e) => failures.Add(e);
				var layout = new BindingDiagnosticsTests(useCompiledXaml) { BindingContext = new { foo = "bar" } };
				Assert.That(failures.Count, Is.GreaterThan(0));
				var failure = failures[0] as BindingErrorEventArgs;
				Assert.Equal("foobar", ((Binding)failure.Binding).Path);
				Assert.Equal(7, failure.XamlSourceInfo.LineNumber);
				Assert.IsType<Label>(failure.Target);
				Assert.Equal(Label.TextProperty, failure.TargetProperty);
			}
		}
	}
}