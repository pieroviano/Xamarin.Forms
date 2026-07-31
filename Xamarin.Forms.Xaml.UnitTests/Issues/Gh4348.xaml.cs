using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh4348VM : ObservableCollection<string>
	{
		public Gh4348VM()
		{
			Add("foo");
			Add("bar");
		}
	}

	public partial class Gh4348 : ContentPage
	{
		public Gh4348()
		{
			InitializeComponent();
		}

		public Gh4348(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
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
			[InlineData(true), InlineData(false)]
			public void GenericBaseClassResolution(bool useCompiledXaml)
			{
				var layout = new Gh4348(useCompiledXaml) { BindingContext = new Gh4348VM() };
				Assert.Equal("2", layout.labelCount.Text);
			}
		}
	}
}
