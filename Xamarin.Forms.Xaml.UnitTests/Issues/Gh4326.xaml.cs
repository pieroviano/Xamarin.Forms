using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh4326 : ContentPage
	{
		public static string Foo = "Foo";
		internal static string Bar = "Bar";

		public Gh4326()
		{
			InitializeComponent();
		}

		public Gh4326(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			public void Dispose()
{
				Device.PlatformServices = null;
			}

			[InlineData(true), TestCase(false)]
			public void FindStaticInternal(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					Assert.DoesNotThrow(() => MockCompiler.Compile(typeof(Gh4326)));
				var layout = new Gh4326(useCompiledXaml);

				Assert.Equal("Foo", layout.labelfoo.Text);
				Assert.Equal("Bar", layout.labelbar.Text);
				Assert.Equal(Style.StyleClassPrefix, layout.labelinternalvisibleto.Text);
			}
		}
	}
}
