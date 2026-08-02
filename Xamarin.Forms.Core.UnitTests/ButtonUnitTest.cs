using System;
using System.Diagnostics;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class ButtonUnitTest
		: CommandSourceTests<Button>
	{
		public ButtonUnitTest()
		{
			Device.PlatformServices = new MockPlatformServices();
		}

		public override void Dispose()
		{
			base.Dispose();
			Device.PlatformServices = null;
		}

		[Fact]
		public void MeasureInvalidatedOnTextChange()
		{
			var button = new Button();

			bool fired = false;
			button.MeasureInvalidated += (sender, args) => fired = true;

			button.Text = "foo";
			Assert.True(fired);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void TestClickedvent(bool isEnabled)
		{
			var view = new Button()
			{
				IsEnabled = isEnabled,
			};

			bool activated = false;
			view.Clicked += (sender, e) => activated = true;

			((IButtonController)view).SendClicked();

			Assert.True(activated == isEnabled ? true : false);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void TestPressedEvent(bool isEnabled)
		{
			var view = new Button()
			{
				IsEnabled = isEnabled,
			};

			bool pressed = false;
			view.Pressed += (sender, e) => pressed = true;

			((IButtonController)view).SendPressed();

			Assert.True(pressed == isEnabled ? true : false);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void TestReleasedEvent(bool isEnabled)
		{
			var view = new Button()
			{
				IsEnabled = isEnabled,
			};

			bool released = false;
			view.Released += (sender, e) => released = true;

			((IButtonController)view).SendReleased();

			Assert.True(released == isEnabled ? true : false);
		}

		protected override Button CreateSource()
		{
			return new Button();
		}

		protected override void Activate(Button source)
		{
			((IButtonController)source).SendClicked();
		}

		protected override BindableProperty IsEnabledProperty
		{
			get { return Button.IsEnabledProperty; }
		}

		protected override BindableProperty CommandProperty
		{
			get { return Button.CommandProperty; }
		}

		protected override BindableProperty CommandParameterProperty
		{
			get { return Button.CommandParameterProperty; }
		}


		[Fact]
		public void TestBindingContextPropagation()
		{
			var context = new object();
			var button = new Button();
			button.BindingContext = context;
			var source = new FileImageSource();
			button.ImageSource = source;
			Assert.Same(context, source.BindingContext);

			button = new Button();
			source = new FileImageSource();
			button.ImageSource = source;
			button.BindingContext = context;
			Assert.Same(context, source.BindingContext);
		}

		[Fact]
		public void TestImageSourcePropertiesChangedTriggerResize()
		{
			var source = new FileImageSource();
			var button = new Button { ImageSource = source };
			bool fired = false;
			button.MeasureInvalidated += (sender, e) => fired = true;
			Assert.Null(source.File);
			source.File = "foo.png";
			Assert.NotNull(source.File);
			Assert.True(fired);
		}

		[Theory]
		[InlineData(NamedSize.Default, FontAttributes.None)]
		[InlineData(NamedSize.Default, FontAttributes.Bold)]
		[InlineData(NamedSize.Default, FontAttributes.Italic)]
		[InlineData(NamedSize.Default, FontAttributes.Bold | FontAttributes.Italic)]
		[InlineData(NamedSize.Large, FontAttributes.None)]
		[InlineData(NamedSize.Large, FontAttributes.Bold)]
		[InlineData(NamedSize.Large, FontAttributes.Italic)]
		[InlineData(NamedSize.Large, FontAttributes.Bold | FontAttributes.Italic)]
		[InlineData(NamedSize.Medium, FontAttributes.None)]
		[InlineData(NamedSize.Medium, FontAttributes.Bold)]
		[InlineData(NamedSize.Medium, FontAttributes.Italic)]
		[InlineData(NamedSize.Medium, FontAttributes.Bold | FontAttributes.Italic)]
		[InlineData(NamedSize.Small, FontAttributes.None)]
		[InlineData(NamedSize.Small, FontAttributes.Bold)]
		[InlineData(NamedSize.Small, FontAttributes.Italic)]
		[InlineData(NamedSize.Small, FontAttributes.Bold | FontAttributes.Italic)]
		[InlineData(NamedSize.Micro, FontAttributes.None)]
		[InlineData(NamedSize.Micro, FontAttributes.Bold)]
		[InlineData(NamedSize.Micro, FontAttributes.Italic)]
		[InlineData(NamedSize.Micro, FontAttributes.Bold | FontAttributes.Italic)]
		public void AssignToFontStructUpdatesFontFamily(NamedSize size, FontAttributes attributes)
		{
			var button = new Button();
			double startSize = button.FontSize;
			var startAttributes = button.FontAttributes;

			bool firedSizeChanged = false;
			bool firedAttributesChanged = false;
			button.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == Label.FontSizeProperty.PropertyName)
					firedSizeChanged = true;
				if (args.PropertyName == Label.FontAttributesProperty.PropertyName)
					firedAttributesChanged = true;
			};

			button.Font = Font.OfSize("Testing123", size).WithAttributes(attributes);

			Assert.Equal(Device.GetNamedSize(size, typeof(Label), true), button.FontSize);
			Assert.Equal(attributes, button.FontAttributes);
			Assert.Equal(startSize != button.FontSize, firedSizeChanged);
			Assert.Equal(startAttributes != button.FontAttributes, firedAttributesChanged);
		}

		[Fact]
		public void AssignToFontFamilyUpdatesFont()
		{
			var button = new Button();

			button.FontFamily = "CrazyFont";
			Assert.Equal(button.Font, Font.OfSize("CrazyFont", button.FontSize));
		}

		[Fact]
		public void AssignToFontSizeUpdatesFont()
		{
			var button = new Button();

			button.FontSize = 1000;
			Assert.Equal(button.Font, Font.SystemFontOfSize(1000));
		}

		[Fact]
		public void AssignToFontAttributesUpdatesFont()
		{
			var button = new Button();

			button.FontAttributes = FontAttributes.Italic | FontAttributes.Bold;
			Assert.Equal(button.Font, Font.SystemFontOfSize(button.FontSize, FontAttributes.Bold | FontAttributes.Italic));
		}

		[Fact]
		public void CommandCanExecuteUpdatesEnabled()
		{
			var button = new Button();

			bool result = false;

			var bindingContext = new
			{
				Command = new Command(() => { }, () => result)
			};

			button.SetBinding(Button.CommandProperty, "Command");
			button.BindingContext = bindingContext;

			Assert.False(button.IsEnabled);

			result = true;

			bindingContext.Command.ChangeCanExecute();

			Assert.True(button.IsEnabled);
		}

		[Fact]
		public void ButtonContentLayoutTypeConverterTest()
		{
			var converter = new Button.ButtonContentTypeConverter();
			Assert.True(converter.CanConvertFrom(typeof(string)));

			AssertButtonContentLayoutsEqual(new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 10), converter.ConvertFromInvariantString("left,10"));
			AssertButtonContentLayoutsEqual(new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Right, 10), converter.ConvertFromInvariantString("right"));
			AssertButtonContentLayoutsEqual(new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Top, 20), converter.ConvertFromInvariantString("top,20"));
			AssertButtonContentLayoutsEqual(new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 15), converter.ConvertFromInvariantString("15"));
			AssertButtonContentLayoutsEqual(new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Bottom, 0), converter.ConvertFromInvariantString("Bottom, 0"));

			Assert.Throws<InvalidOperationException>(() => converter.ConvertFromInvariantString(""));
		}

		[Fact]
		public void ButtonClickWhenCommandCanExecuteFalse()
		{
			bool invoked = false;
			var button = new Button()
			{
				Command = new Command(() => invoked = true
				, () => false),
			};

			(button as IButtonController)
				?.SendClicked();

			Assert.False(invoked);
		}

		internal void ButtonBorderRadiusForwardsToButtonCornerRadius()
		{
			var button = new Button();
			button.BorderRadius = 10;

			Assert.Equal(10, button.CornerRadius);
		}

		[Fact]
		public void ButtonCornerRadiusForwardsToButtonBorderRadius()
		{
			var button = new Button();
			button.CornerRadius = 10;

			Assert.Equal(10, button.BorderRadius);
		}

		[Fact]
		public void ButtonCornerRadiusClearValueForwardsToButtonBorderRadius()
		{
			var button = new Button();

			button.CornerRadius = 10;

			button.ClearValue(Button.CornerRadiusProperty);

			Assert.Equal((int)Button.BorderRadiusProperty.DefaultValue, button.BorderRadius);
		}

		[Fact]
		public void ButtonBorderRadiusClearValueForwardsToButtonCornerRadius()
		{
			var button = new Button();

			button.BorderRadius = 10;

			button.ClearValue(Button.BorderRadiusProperty);

			Assert.Equal((int)Button.CornerRadiusProperty.DefaultValue, button.CornerRadius);
		}

		[Fact]
		public void ButtonCornerRadiusSetToFive()
		{
			var button = new Button();

			button.CornerRadius = 25;
			Assert.Equal(25, button.CornerRadius);

			button.CornerRadius = 5;
			Assert.Equal(5, button.CornerRadius);
		}

		[Fact]
		public void ButtonBorderRadiusSetMinusOne()
		{
			var button = new Button();

			button.BorderRadius = 25;
			Assert.Equal(25, button.BorderRadius);

			button.BorderRadius = -1;
			Assert.Equal(-1, button.BorderRadius);
		}

		private void AssertButtonContentLayoutsEqual(Button.ButtonContentLayout layout1, object layout2)
		{
			var bcl = (Button.ButtonContentLayout)layout2;

			Assert.Equal(layout1.Position, bcl.Position);
			Assert.Equal(layout1.Spacing, bcl.Spacing);
		}
	}
}
