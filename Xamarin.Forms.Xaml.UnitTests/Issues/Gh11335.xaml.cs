using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;
using Xamarin.Forms.Xaml.Diagnostics;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh11335 : ContentPage
	{
		public Gh11335() => InitializeComponent();
		public Gh11335(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		void Remove(object sender, EventArgs e) => stack.Children.Remove(label);

		void Add(object sender, EventArgs e)
		{
			int index = stack.Children.IndexOf(label);
			Label newLabel = new Label { Text = "New Inserted Label" };
			stack.Children.Insert(index + 1, newLabel);
		}

		class Tests
		: IDisposable{
			bool _debuggerinitialstate;

			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
				_debuggerinitialstate = Xamarin.Forms.Xaml.Diagnostics.DebuggerHelper._mockDebuggerIsAttached;
				DebuggerHelper._mockDebuggerIsAttached = true;
			}

			public void Dispose()
{
				DebuggerHelper._mockDebuggerIsAttached = _debuggerinitialstate;
				Device.PlatformServices = null;
				VisualDiagnostics.VisualTreeChanged -= OnVTChanged;
			}

			void OnVTChanged(object sender, VisualTreeChangeEventArgs e)
			{
				Assert.Equal(VisualTreeChangeType.Add, e.ChangeType);
				Assert.Equal(1, e.ChildIndex);
				Assert.Pass();
			}

			[Fact]
			public void ChildIndexOnAdd([Values(false, true)] bool useCompiledXaml)
			{
				var layout = new Gh11335(useCompiledXaml);
				VisualDiagnostics.VisualTreeChanged += OnVTChanged;
				layout.Add(null, EventArgs.Empty);
				Assert.Fail();
			}
		}
	}
}