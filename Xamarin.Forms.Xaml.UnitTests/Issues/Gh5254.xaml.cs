using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh5254VM
	{
		public string Title { get; set; }
		public List<Gh5254VM> Answer { get; set; }
	}

	public partial class Gh5254 : ContentPage
	{
		public Gh5254() => InitializeComponent();
		public Gh5254(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void BindToIntIndexer(bool useCompiledXaml)
			{
				var layout = new Gh5254(useCompiledXaml)
				{
					BindingContext = new Gh5254VM
					{
						Answer = new List<Gh5254VM> {
							new Gh5254VM { Title = "Foo"},
							new Gh5254VM { Title = "Bar"},
						}
					}
				};
				Assert.Equal("Foo", layout.label.Text);
			}
		}
	}
}
