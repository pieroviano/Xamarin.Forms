using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class InlineCSS : ContentPage
	{
		public InlineCSS()
		{
			InitializeComponent();
		}

		public InlineCSS(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
				Xamarin.Forms.Internals.Registrar.RegisterAll(new Type[0]);
			}

			public void Dispose()
{
				Device.PlatformServices = null;
			}

			[InlineData(false), TestCase(true)]
			public void InlineCSSParsed(bool useCompiledXaml)
			{
				var layout = new InlineCSS(useCompiledXaml);
				Assert.Equal(Color.Pink, layout.label.TextColor);
			}

			[InlineData(false), TestCase(true)]
			public void InitialValue(bool useCompiledXaml)
			{
				var layout = new InlineCSS(useCompiledXaml);
				Assert.Equal(Color.Green, layout.BackgroundColor);
				Assert.Equal(Color.Green, layout.stack.BackgroundColor);
				Assert.Equal(Color.Green, layout.button.BackgroundColor);
				Assert.Equal(VisualElement.BackgroundColorProperty.DefaultValue, layout.label.BackgroundColor);
				Assert.Equal(TextTransform.Uppercase, layout.label.TextTransform);
			}
		}
	}
}
