using System;
using System.Collections.Generic;
using Xunit;

namespace Xamarin.Forms.Controls.Tests
{
	public class ObjectDisposedExceptionTests : CrossPlatformTestFixture
	{
		// NUnit's TestCaseSource fed the Func<VisualElement> straight into the test. xUnit
		// builds the display name from the arguments, and a delegate has no useful name, so
		// the case is identified by the control name and the factory is looked up here. This
		// also keeps the arguments serializable, which is what lets xUnit pre-enumerate the
		// theory into one test case per control.
		static readonly Dictionary<string, Func<VisualElement>> VisualElementFactories =
			new Dictionary<string, Func<VisualElement>>
			{
				{ nameof(BoxView), () => new BoxView() },
				{ nameof(Button), () => new Button() },
				{ nameof(CheckBox), () => new CheckBox() },
				{ nameof(DatePicker), () => new DatePicker() },
				{ nameof(Editor), () => new Editor() },
				{ nameof(Entry), () => new Entry() },
				{ nameof(Frame), () => new Frame() },
				{ nameof(Image), () => new Image() },
				{ nameof(ImageButton), () => new ImageButton() },
				{ nameof(Label), () => new Label() },
				{ nameof(Picker), () => new Picker() },
				{ nameof(ProgressBar), () => new ProgressBar() },
				{ nameof(SearchBar), () => new SearchBar() },
				{ nameof(Slider), () => new Slider() },
				{ nameof(Stepper), () => new Stepper() },
				{ nameof(Switch), () => new Switch() },
				{ nameof(TimePicker), () => new TimePicker() },
			};

		public static IEnumerable<object[]> VisualElementTestCases
		{
			get
			{
				foreach (var control in VisualElementFactories.Keys)
				{
					yield return new object[] { control };
				}
			}
		}

		[Theory]
		[MemberData(nameof(VisualElementTestCases))]
		[Trait("Description", "[Bug] ObjectDisposedException (BoxView inside CollectionView)")]
		public void GitHub9431(string control)
		{
			var createVisualElement = VisualElementFactories[control];

			var color1 = Color.Linen;
			var color2 = Color.HotPink;
			var model = new _9431Model() { BGColor = color1 };

			for (int m = 0; m < 3; m++)
			{
				var visualElement = createVisualElement();
				visualElement.SetBinding(VisualElement.BackgroundColorProperty, new Binding("BGColor"));
				visualElement.BindingContext = model;
				TestingPlatform.CreateRenderer(visualElement);

				if (m == 1)
				{
					GC.Collect();
				}

				model.BGColor = model.BGColor == color1 ? color2 : color1;
			}
		}
	}
}
