using System.Linq;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;
using Xamarin.Forms.Xaml.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class StyleTests : ContentPage
	{
		public StyleTests()
		{
			InitializeComponent();
		}

		public StyleTests(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
				Application.Current = new MockApplication();
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void TestStyle(bool useCompiledXaml)
			{
				var layout = new StyleTests(useCompiledXaml);
				Assert.IsAssignableFrom<Style>(layout.style0);
				Assert.Same(layout.style0, layout.label0.Style);
				Assert.Equal("FooBar", layout.label0.Text);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void TestConversionOnSetters(bool useCompiledXaml)
			{
				var layout = new StyleTests(useCompiledXaml);
				Style style = layout.style1;
				Setter setter;

				//Test built-in conversions
				setter = style.Setters.Single(s => s.Property == HeightProperty);
				Assert.IsType<double>(setter.Value);
				Assert.Equal(42d, (double)setter.Value);

				//Test TypeConverters
				setter = style.Setters.Single(s => s.Property == BackgroundColorProperty);
				Assert.IsType<Color>(setter.Value);
				Assert.Equal(Color.Pink, (Color)setter.Value);

				//Test implicit cast operator
				setter = style.Setters.Single(s => s.Property == Image.SourceProperty);
				Assert.IsType<FileImageSource>(setter.Value);
				Assert.Equal("foo.png", ((FileImageSource)setter.Value).File);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void ImplicitStyleAreApplied(bool useCompiledXaml)
			{
				var layout = new StyleTests(useCompiledXaml);
				Assert.Equal(Color.Red, layout.label1.TextColor);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void PropertyDoesNotNeedTypes(bool useCompiledXaml)
			{
				var layout = new StyleTests(useCompiledXaml);
				Style style2 = layout.style2;
				var s0 = style2.Setters[0];
				var s1 = style2.Setters[1];
				Assert.Equal(Label.TextProperty, s0.Property);
				Assert.Equal(BackgroundColorProperty, s1.Property);
				Assert.Equal(Color.Red, s1.Value);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			//issue #2406
			public void StylesDerivedFromDynamicStylesThroughStaticResource(bool useCompiledXaml)
			{
				var layout = new StyleTests(useCompiledXaml);
				Application.Current.MainPage = layout;

				var label = layout.labelWithStyleDerivedFromDynamic_StaticResource;

				Assert.Equal(50, label.FontSize);
				Assert.Equal(Color.Red, label.TextColor);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			//issue #2406
			public void StylesDerivedFromDynamicStylesThroughDynamicResource(bool useCompiledXaml)
			{
				var layout = new StyleTests(useCompiledXaml);
				Application.Current.MainPage = layout;

				var label = layout.labelWithStyleDerivedFromDynamic_DynamicResource;

				Assert.Equal(50, label.FontSize);
				Assert.Equal(Color.Red, label.TextColor);
			}
		}
	}
}