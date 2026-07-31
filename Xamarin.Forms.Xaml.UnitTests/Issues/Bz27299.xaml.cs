using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Bz27299ViewModel
	{
		public string Text
		{
			get { return "Foo"; }
		}
	}
	public class Bz27299ViewModelLocator
	{
		public static int Count { get; set; }
		public object Bz27299
		{
			get
			{
				Count++;
				return new Bz27299ViewModel();
			}
		}
	}

	public partial class Bz27299 : ContentPage
	{
		public Bz27299()
		{
			InitializeComponent();
		}

		public Bz27299(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			public Tests()
{
				Bz27299ViewModelLocator.Count = 0;
				Device.PlatformServices = new MockPlatformServices();
			}

			public void Dispose()
{
				Device.PlatformServices = null;
			}

			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			public void ViewModelLocatorOnlyCalledOnce(bool useCompiledXaml)
			{
				Assert.Equal(0, Bz27299ViewModelLocator.Count);
				var layout = new Bz27299(useCompiledXaml);
				Assert.Equal(1, Bz27299ViewModelLocator.Count);
				Assert.Equal("Foo", layout.label.Text);
			}
		}
	}
}