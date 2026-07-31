using Xunit;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Unreported006 : ContentPage
	{
		public Unreported006()
		{
			InitializeComponent();
		}

		public Unreported006(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public Layout<View> GenericProperty
		{
			get { return (Layout<View>)GetValue(GenericPropertyProperty); }
			set { SetValue(GenericPropertyProperty, value); }
		}

		public static readonly BindableProperty GenericPropertyProperty =
			BindableProperty.Create(nameof(GenericProperty), typeof(Layout<View>), typeof(Unreported006));

		public class Tests
		{
			[Theory]
			[InlineData(true), InlineData(false)]
			public void CanAssignGenericBP(bool useCompiledXaml)
			{
				var page = new Unreported006();
				Assert.NotNull(page.GenericProperty);
				Assert.IsType<StackLayout>(page.GenericProperty);
			}
		}
	}
}