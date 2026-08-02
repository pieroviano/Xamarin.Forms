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

		public class Tests: IDisposable
		{
			bool _debuggerinitialstate;

			public Tests()
			{
				Device.PlatformServices = new MockPlatformServices();

				// VisualDiagnostics.RegisterSourceInfo only records anything when a debugger is
				// attached, and DebuggerHelper fakes that automatically ONLY under #if DEBUG. So
				// without this the XamlSourceInfo assertion below dereferences null in a Release
				// run - the product is behaving correctly, the test was just relying on the build
				// configuration. Same save/set/restore the Gh10803, Gh11334 and Gh11335 fixtures
				// already use.
				_debuggerinitialstate = DebuggerHelper._mockDebuggerIsAttached;
				DebuggerHelper._mockDebuggerIsAttached = true;
			}

			public void Dispose()
			{
				DebuggerHelper._mockDebuggerIsAttached = _debuggerinitialstate;
				Device.PlatformServices = null;
			}

			[Theory]
			[InlineData(false)]
			//[InlineData(true)]
			public void Test(bool useCompiledXaml)
			{
				List<BindingBaseErrorEventArgs> failures = new List<BindingBaseErrorEventArgs>();
				BindingDiagnostics.BindingFailed += (o, e) => failures.Add(e);
				var layout = new BindingDiagnosticsTests(useCompiledXaml) { BindingContext = new { foo = "bar" } };
				Assert.True(failures.Count > 0);
				var failure = failures[0] as BindingErrorEventArgs;
				Assert.Equal("foobar", ((Binding)failure.Binding).Path);
				Assert.Equal(7, failure.XamlSourceInfo.LineNumber);
				Assert.IsType<Label>(failure.Target);
				Assert.Equal(Label.TextProperty, failure.TargetProperty);
			}
		}
	}
}