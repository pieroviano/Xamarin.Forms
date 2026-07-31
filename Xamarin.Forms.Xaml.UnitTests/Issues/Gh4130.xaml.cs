using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh4130Control : ContentView
	{
		public delegate void TextChangedEventHandler(object sender, TextChangedEventArgs args);
#pragma warning disable 067
		public event TextChangedEventHandler TextChanged;
#pragma warning restore 067
		public void FireEvent()
		{
			TextChanged?.Invoke(this, new TextChangedEventArgs(null, null));
		}
	}

	public partial class Gh4130 : ContentPage
	{
		public Gh4130()
		{
			InitializeComponent();
			var c = new Gh4130Control();
		}

		public Gh4130(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		// Was Assert.Pass() under NUnit, which passed the whole test from inside the handler.
		internal static bool EventFired;

		void OnTextChanged(object sender, EventArgs e)
		{
			EventFired = true;
		}

		public class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			public void Dispose()
{
				Device.PlatformServices = null;
			}

			[Theory]
			[InlineData(false), InlineData(true)]
			public void NonGenericEventHanlders(bool useCompiledXaml)
			{
				Gh4130.EventFired = false;
				var layout = new Gh4130(useCompiledXaml);
				var control = layout.Content as Gh4130Control;
				control.FireEvent();
				Assert.True(Gh4130.EventFired, "the event handler was never invoked");
			}
		}
	}
}
