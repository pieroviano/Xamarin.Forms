using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;
using Xamarin.Forms.Xaml.Diagnostics;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh10803 : ContentPage
	{
		public Gh10803() => InitializeComponent();
		public Gh10803(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			bool debuggerinitialstate;
			int failures = 0;

			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
				VisualDiagnostics.VisualTreeChanged += VTChanged;
				debuggerinitialstate = Xamarin.Forms.Xaml.Diagnostics.DebuggerHelper._mockDebuggerIsAttached;
				DebuggerHelper._mockDebuggerIsAttached = true;
			}

			public void Dispose()
{
				DebuggerHelper._mockDebuggerIsAttached = debuggerinitialstate;
				Device.PlatformServices = null;
				VisualDiagnostics.VisualTreeChanged -= VTChanged;
				failures = 0;
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void SourceInfoForElementsInDT(bool useCompiledXaml)
			{
				var layout = new Gh10803(useCompiledXaml);
				var listview = layout.listview;
				var cell = listview.TemplatedItems.GetOrCreateContent(0, null);
				Assert.Equal(0, failures);
			}

			void VTChanged(object sender, VisualTreeChangeEventArgs e)
			{
				var parentSourInfo = e.Parent == null ? null : VisualDiagnostics.GetXamlSourceInfo(e.Parent);
				var childSourceInfo = VisualDiagnostics.GetXamlSourceInfo(e.Child);
				if (childSourceInfo == null)
					failures++;
				if (e.Parent != null && parentSourInfo == null)
					failures++;
				if (e.Parent != null && e.ChildIndex == -1)
					failures++;
			}
		}
	}
}
