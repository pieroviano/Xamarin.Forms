using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class LabelTests : BaseTestFixture
	{
		public LabelTests()
		{
			Device.PlatformServices = new MockPlatformServices();
		}

		public override void Dispose()
		{
			base.Dispose();
			Device.PlatformServices = null;
		}

		[Fact]
		public void TextAndAttributedTextMutuallyExclusive()
		{
			var label = new Label();
			Assert.Null(label.Text);
			Assert.Null(label.FormattedText);

			label.Text = "Foo";
			Assert.Equal("Foo", label.Text);
			Assert.Null(label.FormattedText);

			var fs = new FormattedString();
			label.FormattedText = fs;
			Assert.Null(label.Text);
			Assert.Same(fs, label.FormattedText);

			label.Text = "Foo";
			Assert.Equal("Foo", label.Text);
			Assert.Null(label.FormattedText);
		}

		[Fact]
		public void InvalidateMeasureWhenTextChanges()
		{
			var label = new Label();

			bool fired;
			label.MeasureInvalidated += (sender, args) =>
			{
				fired = true;
			};

			fired = false;
			label.Text = "Foo";
			Assert.True(fired);

			fired = false;
			label.TextTransform = TextTransform.Lowercase;
			Assert.True(fired);

			fired = false;
			label.TextTransform = TextTransform.Uppercase;
			Assert.True(fired);

			fired = false;
			label.TextTransform = TextTransform.None;
			Assert.True(fired);

			var fs = new FormattedString();

			fired = false;
			label.FormattedText = fs;
			Assert.True(fired);

			fired = false;
			fs.Spans.Add(new Span { Text = "bar" });
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
			var label = new Label();
			double startSize = label.FontSize;
			var startAttributes = label.FontAttributes;

			bool firedSizeChanged = false;
			bool firedAttributesChanged = false;
			label.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == Label.FontSizeProperty.PropertyName)
					firedSizeChanged = true;
				if (args.PropertyName == Label.FontAttributesProperty.PropertyName)
					firedAttributesChanged = true;
			};

			label.Font = Font.OfSize("Testing123", size).WithAttributes(attributes);

			Assert.Equal(Device.GetNamedSize(size, typeof(Label), true), label.FontSize);
			Assert.Equal(attributes, label.FontAttributes);
			Assert.Equal(startSize != label.FontSize, firedSizeChanged);
			Assert.Equal(startAttributes != label.FontAttributes, firedAttributesChanged);
		}

		[Fact]
		public void AssignToFontFamilyUpdatesFont()
		{
			var label = new Label();

			label.FontFamily = "CrazyFont";
			Assert.Equal(label.Font, Font.OfSize("CrazyFont", label.FontSize));
		}

		[Fact]
		public void AssignToFontSizeUpdatesFont()
		{
			var label = new Label();

			label.FontSize = 1000;
			Assert.Equal(label.Font, Font.SystemFontOfSize(1000));
		}

		[Fact]
		public void AssignedToFontSizeUpdatesFontDouble()
		{
			var label = new Label();

			label.FontSize = 10.7;
			Assert.Equal(label.Font, Font.SystemFontOfSize(10.7));
		}

		[Fact]
		public void AssignedToFontSizeDouble()
		{
			var label = new Label();

			label.FontSize = 10.7;
			Assert.Equal(label.FontSize, 10.7);
		}


		[Fact]
		public void AssignToFontAttributesUpdatesFont()
		{
			var label = new Label();

			label.FontAttributes = FontAttributes.Italic | FontAttributes.Bold;
			Assert.Equal(label.Font, Font.SystemFontOfSize(label.FontSize, FontAttributes.Bold | FontAttributes.Italic));
		}

		[Fact]
		public void LabelResizesWhenFontChanges()
		{
			Device.PlatformServices = new MockPlatformServices(getNativeSizeFunc: (ve, w, h) =>
			{
				var l = (Label)ve;
				return new SizeRequest(new Size(l.Font.FontSize, l.Font.FontSize));
			});

			var label = new Label { IsPlatformEnabled = true };

			Assert.Equal(label.Font.FontSize, label.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity).Request.Width);

			bool fired = false;

			label.MeasureInvalidated += (sender, args) =>
			{
				Assert.Equal(25, label.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity).Request.Width);
				fired = true;
			};


			label.FontSize = 25;

			Assert.True(fired);
		}

		[Fact]
		public void FontSizeConverterTests()
		{
			var converter = new FontSizeConverter();
			// 12d, not 12: the converter boxes a double, and Assert.Equal<object> - unlike
			// NUnit's Assert.AreEqual - does not coerce a boxed int to it.
			Assert.Equal(12d, converter.ConvertFromInvariantString("12"));
			Assert.Equal(10.7, converter.ConvertFromInvariantString("10.7"));
		}

		[Fact]
		public void FontSizeCanBeSetFromStyle()
		{
			var label = new Label();

			Assert.Equal(10.0, label.FontSize);

			label.SetValue(Label.FontSizeProperty, 1.0, true);
			Assert.Equal(1.0, label.FontSize);
		}

		[Fact]
		public void ManuallySetFontSizeNotOverridenByStyle()
		{
			var label = new Label();
			Assert.Equal(10.0, label.FontSize);

			label.SetValue(Label.FontSizeProperty, 2.0, false);
			Assert.Equal(2.0, label.FontSize);

			label.SetValue(Label.FontSizeProperty, 1.0, true);
			Assert.Equal(2.0, label.FontSize);
		}

		[Fact]
		public void ManuallySetFontSizeNotOverridenByFontSetInStyle()
		{
			var label = new Label();
			Assert.Equal(10.0, label.FontSize);

			label.SetValue(Label.FontSizeProperty, 2.0);
			Assert.Equal(2.0, label.FontSize);

			label.SetValue(Label.FontProperty, Font.SystemFontOfSize(1.0), fromStyle: true);
			Assert.Equal(2.0, label.FontSize);
		}

		[Fact]
		public void ChangingHorizontalTextAlignmentFiresXAlignChanged()
		{
			var label = new Label() { HorizontalTextAlignment = TextAlignment.Center };

			var xAlignFired = false;
			var horizontalTextAlignmentFired = false;

			label.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "XAlign")
				{
					xAlignFired = true;
				}
				else if (args.PropertyName == Label.HorizontalTextAlignmentProperty.PropertyName)
				{
					horizontalTextAlignmentFired = true;
				}
			};

			label.HorizontalTextAlignment = TextAlignment.End;

			Assert.True(xAlignFired);
			Assert.True(horizontalTextAlignmentFired);
		}

		[Fact]
		public void ChangingVerticalTextAlignmentFiresYAlignChanged()
		{
			var label = new Label() { VerticalTextAlignment = TextAlignment.Center };

			var yAlignFired = false;
			var verticalTextAlignmentFired = false;

			label.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "YAlign")
				{
					yAlignFired = true;
				}
				else if (args.PropertyName == Label.VerticalTextAlignmentProperty.PropertyName)
				{
					verticalTextAlignmentFired = true;
				}
			};

			label.VerticalTextAlignment = TextAlignment.End;

			Assert.True(yAlignFired);
			Assert.True(verticalTextAlignmentFired);
		}

		[Fact]
		public void EntryCellXAlignBindingMatchesHorizontalTextAlignmentBinding()
		{
			var vm = new ViewModel();
			vm.HorizontalAlignment = TextAlignment.Center;

			var labelXAlign = new Label() { BindingContext = vm };
			labelXAlign.SetBinding(Label.XAlignProperty, new Binding("HorizontalAlignment"));

			var labelHorizontalTextAlignment = new Label() { BindingContext = vm };
			labelHorizontalTextAlignment.SetBinding(Label.HorizontalTextAlignmentProperty, new Binding("HorizontalAlignment"));

			Assert.Equal(TextAlignment.Center, labelXAlign.XAlign);
			Assert.Equal(TextAlignment.Center, labelHorizontalTextAlignment.HorizontalTextAlignment);

			vm.HorizontalAlignment = TextAlignment.End;

			Assert.Equal(TextAlignment.End, labelXAlign.XAlign);
			Assert.Equal(TextAlignment.End, labelHorizontalTextAlignment.HorizontalTextAlignment);
		}

		[Fact]
		public void EntryCellYAlignBindingMatchesVerticalTextAlignmentBinding()
		{
			var vm = new ViewModel();
			vm.VerticalAlignment = TextAlignment.Center;

			var labelYAlign = new Label() { BindingContext = vm };
			labelYAlign.SetBinding(Label.YAlignProperty, new Binding("VerticalAlignment"));

			var labelVerticalTextAlignment = new Label() { BindingContext = vm };
			labelVerticalTextAlignment.SetBinding(Label.VerticalTextAlignmentProperty, new Binding("VerticalAlignment"));

			Assert.Equal(TextAlignment.Center, labelYAlign.YAlign);
			Assert.Equal(TextAlignment.Center, labelVerticalTextAlignment.VerticalTextAlignment);

			vm.VerticalAlignment = TextAlignment.End;

			Assert.Equal(TextAlignment.End, labelYAlign.YAlign);
			Assert.Equal(TextAlignment.End, labelVerticalTextAlignment.VerticalTextAlignment);
		}

		sealed class ViewModel : INotifyPropertyChanged
		{
			TextAlignment horizontalAlignment;
			TextAlignment verticalAlignment;

			public TextAlignment HorizontalAlignment
			{
				get { return horizontalAlignment; }
				set
				{
					horizontalAlignment = value;
					OnPropertyChanged();
				}
			}

			public TextAlignment VerticalAlignment
			{
				get { return verticalAlignment; }
				set
				{
					verticalAlignment = value;
					OnPropertyChanged();
				}
			}

			public event PropertyChangedEventHandler PropertyChanged;

			void OnPropertyChanged([CallerMemberName] string propertyName = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}

