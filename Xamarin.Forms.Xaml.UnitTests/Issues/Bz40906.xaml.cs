using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz40906 : ContentPage
	{
		public Bz40906()
		{
			InitializeComponent();
		}

		public Bz40906(bool useCompiledXaml)
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
			[InlineData(true)]
			[InlineData(false)]
			public void ParsingCDATA(bool useCompiledXaml)
			{
				var page = new Bz40906(useCompiledXaml);
				Assert.Equal("Foo", page.label0.Text);
				Assert.Equal("FooBar>><<", page.label1.Text);
			}
		}
	}
}
