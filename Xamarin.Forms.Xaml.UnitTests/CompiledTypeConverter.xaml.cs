using System.Collections.Generic;
using Xunit;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class CompiledTypeConverter : ContentPage
	{
		public static readonly BindableProperty RectangleBPProperty =
			BindableProperty.Create("RectangleBP", typeof(Rectangle), typeof(CompiledTypeConverter), default(Rectangle));

		public Rectangle RectangleBP
		{
			get { return (Rectangle)GetValue(RectangleBPProperty); }
			set { SetValue(RectangleBPProperty, value); }
		}

		public Rectangle RectangleP { get; set; }

		[TypeConverter(typeof(ListStringTypeConverter))]
		public IList<string> List { get; set; }


		public CompiledTypeConverter()
		{
			InitializeComponent();
		}

		public CompiledTypeConverter(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[InlineData(false)]
			[InlineData(true)]
			public void CompiledTypeConverterAreInvoked(bool useCompiledXaml)
			{
				var p = new CompiledTypeConverter(useCompiledXaml);
				Assert.Equal(new Rectangle(0, 1, 2, 4), p.RectangleP);
				Assert.Equal(new Rectangle(4, 8, 16, 32), p.RectangleBP);
				Assert.Equal(Color.Pink, p.BackgroundColor);
				Assert.Equal(LayoutOptions.EndAndExpand, p.label.GetValue(View.HorizontalOptionsProperty));
				var xConstraint = RelativeLayout.GetXConstraint(p.label);
				Assert.Equal(2, xConstraint.Compute(null));
				Assert.Equal(new Thickness(2, 3), p.label.Margin);
				Assert.Equal(2, p.List.Count);
				Assert.Equal("Bar", p.List[1]);
			}
		}
	}
}
