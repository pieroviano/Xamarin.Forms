using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh2171 : ContentPage
	{
		public Gh2171()
		{
			InitializeComponent();
		}

		public Gh2171(bool useCompiledXaml)
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
			public void ParsingNestedMarkups(bool useCompiledXaml)
			{
				var layout = new Gh2171(useCompiledXaml);
				var markup = layout.BindingContext as Gh2171Extension;
				Assert.NotNull(markup);
				Assert.Equal("foo", markup.Foo);
				Assert.Equal("bar", markup.Bar);
				Assert.Equal("Text", (markup.Binding as Binding).Path);
			}
		}
	}

	public class Gh2171Extension : IMarkupExtension
	{
		public string Foo { get; set; }
		public string Bar { get; set; }
		public BindingBase Binding { get; set; }
		object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => this;
	}
}
