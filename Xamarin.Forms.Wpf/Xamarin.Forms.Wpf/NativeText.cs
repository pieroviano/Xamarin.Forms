using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using WpfBrush = System.Windows.Media.Brush;
using WpfThickness = System.Windows.Thickness;

namespace Xamarin.Forms.Wpf
{
	/// <summary>Fonts and colors of a WPF element onto the Xamarin.Forms view that draws its text.</summary>
	internal static class NativeText
	{
		/// <summary>
		/// Font family, size, weight and style of <paramref name="source"/> onto <paramref name="view"/>. What nothing
		/// in the tree sets keeps the view's default - the GTK theme's font, not WPF's Segoe UI.
		/// </summary>
		internal static void ApplyFont(View view, DependencyObject source)
		{
			var family = source.HasEffectiveValue(TextElement.FontFamilyProperty)
				? ((FontFamily)source.GetValue(TextElement.FontFamilyProperty))?.FirstFamily
				: null;
			var size = source.HasEffectiveValue(TextElement.FontSizeProperty)
				? Units.DipsToPoints((double)source.GetValue(TextElement.FontSizeProperty))
				: -1;
			var attributes = FontAttributes.None;
			if (((System.Windows.FontWeight)source.GetValue(TextElement.FontWeightProperty)).ToOpenTypeWeight() >= 600)
				attributes |= FontAttributes.Bold;
			var style = (System.Windows.FontStyle)source.GetValue(TextElement.FontStyleProperty);
			if (style == FontStyles.Italic || style == FontStyles.Oblique)
				attributes |= FontAttributes.Italic;

			switch (view)
			{
				case Label v:
					Set(v, Label.FontFamilyProperty, Label.FontSizeProperty, Label.FontAttributesProperty, family, size, attributes);
					break;
				case Button v:
					Set(v, Button.FontFamilyProperty, Button.FontSizeProperty, Button.FontAttributesProperty, family, size, attributes);
					break;
				case Entry v:
					Set(v, Entry.FontFamilyProperty, Entry.FontSizeProperty, Entry.FontAttributesProperty, family, size, attributes);
					break;
				case Editor v:
					Set(v, Editor.FontFamilyProperty, Editor.FontSizeProperty, Editor.FontAttributesProperty, family, size, attributes);
					break;
				case Picker v:
					Set(v, Picker.FontFamilyProperty, Picker.FontSizeProperty, Picker.FontAttributesProperty, family, size, attributes);
					break;
				case DatePicker v:
					Set(v, DatePicker.FontFamilyProperty, DatePicker.FontSizeProperty, DatePicker.FontAttributesProperty, family, size, attributes);
					break;
				case RadioButton v:
					Set(v, RadioButton.FontFamilyProperty, RadioButton.FontSizeProperty, RadioButton.FontAttributesProperty, family, size, attributes);
					break;
				case SearchBar v:
					Set(v, SearchBar.FontFamilyProperty, SearchBar.FontSizeProperty, SearchBar.FontAttributesProperty, family, size, attributes);
					break;
			}
		}

		static void Set(BindableObject view, BindableProperty familyProperty, BindableProperty sizeProperty, BindableProperty attributesProperty,
			string family, double size, FontAttributes attributes)
		{
			if (family != null)
				view.SetValue(familyProperty, family);
			else
				view.ClearValue(familyProperty);

			if (size > 0)
				view.SetValue(sizeProperty, size);
			else
				view.ClearValue(sizeProperty);

			view.SetValue(attributesProperty, attributes);
		}

		/// <summary>The foreground of <paramref name="source"/> as the text color of <paramref name="view"/>, when something sets it.</summary>
		internal static void ApplyForeground(View view, DependencyObject source)
		{
			var color = source.HasEffectiveValue(TextElement.ForegroundProperty)
				? ((WpfBrush)source.GetValue(TextElement.ForegroundProperty))?.ToFormsColor() ?? Color.Default
				: Color.Default;

			switch (view)
			{
				case Label v:
					v.TextColor = color;
					break;
				case Button v:
					v.TextColor = color;
					break;
				case Entry v:
					v.TextColor = color;
					break;
				case Editor v:
					v.TextColor = color;
					break;
				case Picker v:
					v.TextColor = color;
					break;
				case DatePicker v:
					v.TextColor = color;
					break;
				case RadioButton v:
					v.TextColor = color;
					break;
				case SearchBar v:
					v.TextColor = color;
					break;
				case CheckBox v:
					v.Color = color;
					break;
			}
		}

		internal static TextAlignment ToForms(System.Windows.TextAlignment alignment)
		{
			switch (alignment)
			{
				case System.Windows.TextAlignment.Center:
					return TextAlignment.Center;
				case System.Windows.TextAlignment.Right:
					return TextAlignment.End;
				default:
					return TextAlignment.Start;
			}
		}

		internal static TextAlignment ToForms(System.Windows.HorizontalAlignment alignment)
		{
			switch (alignment)
			{
				case System.Windows.HorizontalAlignment.Center:
					return TextAlignment.Center;
				case System.Windows.HorizontalAlignment.Right:
					return TextAlignment.End;
				default:
					return TextAlignment.Start;
			}
		}

		internal static TextAlignment ToForms(System.Windows.VerticalAlignment alignment)
		{
			switch (alignment)
			{
				case System.Windows.VerticalAlignment.Center:
				case System.Windows.VerticalAlignment.Stretch:
					return TextAlignment.Center;
				case System.Windows.VerticalAlignment.Bottom:
					return TextAlignment.End;
				default:
					return TextAlignment.Start;
			}
		}

		/// <summary>
		/// WPF access text: an underscore marks the access key and is not shown, a doubled one shows one.
		/// </summary>
		internal static string StripAccessKey(string text)
		{
			if (string.IsNullOrEmpty(text) || text.IndexOf('_') < 0)
				return text;

			var sb = new System.Text.StringBuilder(text.Length);
			for (var i = 0; i < text.Length; i++)
			{
				if (text[i] == '_')
				{
					if (i + 1 < text.Length && text[i + 1] == '_')
					{
						sb.Append('_');
						i++;
					}

					continue;
				}

				sb.Append(text[i]);
			}

			return sb.ToString();
		}

		/// <summary>The access key of an access text (the letter after the first single underscore), or '\0'.</summary>
		internal static char AccessKeyOf(string text)
		{
			if (string.IsNullOrEmpty(text))
				return '\0';

			for (var i = 0; i < text.Length - 1; i++)
			{
				if (text[i] != '_')
					continue;
				if (text[i + 1] == '_')
				{
					i++;
					continue;
				}

				return char.ToUpperInvariant(text[i + 1]);
			}

			return '\0';
		}

		internal static Thickness ToForms(WpfThickness t) => new Thickness(t.Left, t.Top, t.Right, t.Bottom);
	}

	/// <summary>
	/// A view with an optional border and padding around another: Xamarin.Forms views draw no border of their own,
	/// and WPF's <c>Border</c>, <c>BorderBrush</c> and <c>Padding</c> need one.
	/// </summary>
	internal sealed class NativeShell : ContentView
	{
		readonly Frame _frame = new Frame { HasShadow = false, CornerRadius = 0, Padding = 0, BackgroundColor = Color.Transparent };
		View _inner;
		bool _bordered;

		internal NativeShell(View inner = null)
		{
			Padding = 0;
			Inner = inner;
		}

		internal View Inner
		{
			get => _inner;
			set
			{
				_inner = value;
				if (_bordered)
					_frame.Content = value;
				else
					Content = value;
			}
		}

		/// <summary>A border in <paramref name="color"/> when <paramref name="thickness"/> is not zero; padding inside it.</summary>
		internal void SetBorder(Color color, WpfThickness thickness, WpfThickness padding)
		{
			var bordered = thickness.Left > 0 || thickness.Top > 0 || thickness.Right > 0 || thickness.Bottom > 0;
			if (bordered != _bordered)
			{
				_bordered = bordered;
				if (bordered)
				{
					Content = null;
					_frame.Content = _inner;
					Content = _frame;
				}
				else
				{
					_frame.Content = null;
					Content = _inner;
				}
			}

			if (bordered)
			{
				_frame.BorderColor = color == Color.Default ? Color.Black : color;
				_frame.Padding = NativeText.ToForms(padding);
				Padding = 0;
			}
			else
			{
				Padding = NativeText.ToForms(padding);
			}
		}
	}
}
