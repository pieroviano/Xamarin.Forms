using System;
using System.Collections.Generic;

using Xunit;

using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class DynamicResource : ContentPage
	{
		public DynamicResource()
		{
			InitializeComponent();
		}

		public DynamicResource(bool useCompiledXaml)
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
			[InlineData(false), InlineData(true)]
			public void TestDynamicResources(bool useCompiledXaml)
			{
				var layout = new DynamicResource(useCompiledXaml);
				var label = layout.label0;

				Assert.Null(label.Text);

				layout.Resources = new ResourceDictionary {
					{"FooBar", "FOOBAR"},
				};
				Assert.Equal("FOOBAR", label.Text);
			}
		}
	}
}