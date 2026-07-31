using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Issue3076Button : Button
	{
		public static readonly BindableProperty VerticalContentAlignmentProperty =
			BindableProperty.Create("VerticalContentAlignemnt", typeof(TextAlignment), typeof(Issue3076Button), TextAlignment.Center);

		public TextAlignment VerticalContentAlignment
		{
			get { return (TextAlignment)GetValue(VerticalContentAlignmentProperty); }
			set { SetValue(VerticalContentAlignmentProperty, value); }
		}
	}

	public partial class Issue3076 : ContentPage
	{
		public Issue3076()
		{
			InitializeComponent();
		}

		public Issue3076(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void CanUseBindableObjectDefinedInThisAssembly(bool useCompiledXaml)
			{
				var layout = new Issue3076(useCompiledXaml);

				Assert.IsType<Issue3076Button>(layout.local);
				Assert.Equal(TextAlignment.Start, layout.local.VerticalContentAlignment);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void CanUseBindableObjectDefinedInOtherAssembly(bool useCompiledXaml)
			{
				var layout = new Issue3076(useCompiledXaml);

				Assert.IsType<Controls.Issue3076Button>(layout.controls);
				Assert.Equal(TextAlignment.Start, layout.controls.HorizontalContentAlignment);
			}
		}
	}
}