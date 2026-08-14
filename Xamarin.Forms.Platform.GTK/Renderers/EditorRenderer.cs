using System;
using System.ComponentModel;
using Gtk;
using Pango;
using Xamarin.Forms.Platform.GTK.Controls;
using Xamarin.Forms.Platform.GTK.Extensions;
using Xamarin.Forms.Platform.GTK.Helpers;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public class EditorRenderer : ViewRenderer<Editor, ScrolledTextView>
	{
		private const string TextColorTagName = "text-color";

		private bool _disposed;

		/// <summary>
		/// True while <see cref="UpdateText"/> is pushing the element's text into the buffer, so
		/// the buffer's own change notifications are not mistaken for the user typing.
		/// </summary>
		private bool _updatingTextFromElement;

		protected IEditorController EditorController => Element as IEditorController;

		protected override void UpdateBackgroundColor()
		{
			if (!Element.BackgroundColor.IsDefaultOrTransparent())
			{
				var backgroundColor = Element.BackgroundColor.ToGtkColor();

				Control.SetBackgroundColor(backgroundColor);
			}
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
		{
			if (Control == null)
			{
				var scrolled = new ScrolledTextView();

				scrolled.TextView.Buffer.TagTable.Add(new TextTag(TextColorTagName));

				scrolled.TextView.Buffer.Changed += TextViewBufferChanged;
				scrolled.TextView.Focused += TextViewFocused;
				scrolled.TextView.FocusOutEvent += TextViewFocusedOut;

				SetNativeControl(scrolled);
				AdjustMinimumHeight(scrolled.TextView);
			}

			if (e.NewElement != null)
			{
				UpdateText();
				UpdateFont();
				UpdateTextColor();
				UpdatePlaceholder();
				UpdatePlaceholderColor();
				UpdateMaxLength();
			}

			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == Editor.TextProperty.PropertyName ||
				e.PropertyName == Editor.TextTransformProperty.PropertyName)
				UpdateText();
			else if (e.PropertyName == Editor.TextColorProperty.PropertyName)
				UpdateTextColor();
			else if (e.PropertyName == Editor.PlaceholderProperty.PropertyName)
				UpdatePlaceholder();
			else if (e.PropertyName == Editor.PlaceholderColorProperty.PropertyName)
				UpdatePlaceholderColor();
			else if (e.PropertyName == Editor.FontAttributesProperty.PropertyName)
				UpdateFont();
			else if (e.PropertyName == Editor.FontFamilyProperty.PropertyName)
				UpdateFont();
			else if (e.PropertyName == Editor.FontSizeProperty.PropertyName)
				UpdateFont();
			else if (e.PropertyName == InputView.MaxLengthProperty.PropertyName)
				UpdateMaxLength();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;

				if (Control != null)
				{
					Control.TextView.Buffer.Changed -= TextViewBufferChanged;
					Control.TextView.Focused -= TextViewFocused;
					// -=, not +=. This subscribed a SECOND handler while disposing, so a focused
					// Editor being torn down raised Completed twice instead of not at all.
					Control.TextView.FocusOutEvent -= TextViewFocusedOut;
				}
			}

			base.Dispose(disposing);
		}

		private void UpdateText()
		{
			TextBuffer buffer = Control.TextView.Buffer;

			var text = Element.UpdateFormsText(Element.Text, Element.TextTransform);
			if (buffer.Text == text)
				return;

			// gtk_text_buffer_set_text deletes the existing contents and then inserts the new
			// ones, and BOTH steps raise Changed. Without this guard the delete's notification
			// reaches TextViewBufferChanged while the buffer is momentarily empty, which writes
			// "" back into Element.Text - so a programmatic `editor.Text = "x"` ends with an
			// empty control, and any binding on Text observes a spurious empty value first.
			_updatingTextFromElement = true;

			try
			{
				buffer.Text = text;
			}
			finally
			{
				_updatingTextFromElement = false;
			}

			UpdateTextColor();
		}

		private void UpdateFont()
		{
			FontDescription fontDescription = FontDescriptionHelper.CreateFontDescription(
				Element.FontSize, Element.FontFamily, Element.FontAttributes);
			Control.TextView.SetFont(fontDescription);

			AdjustMinimumHeight(Control.TextView, fontDescription);
		}

		private void UpdateTextColor()
		{
			if (!Element.TextColor.IsDefaultOrTransparent())
			{
				var textColor = Element.TextColor.ToGtkColor();

				TextBuffer buffer = Control.TextView.Buffer;
				TextTag tag = buffer.TagTable.Lookup(TextColorTagName);
				tag.ForegroundGdk = Element.IsEnabled ? textColor : Control.GetDefaultForegroundColor(Gtk.StateFlags.Normal);
				Control.TextView.Buffer.ApplyTag(tag, buffer.StartIter, buffer.EndIter);
			}
		}

		private void TextViewBufferChanged(object sender, EventArgs e)
		{
			if (_updatingTextFromElement)
				return;

			TextBuffer buffer = Control.TextView.Buffer;

			if (Element.Text != buffer.Text)
				ElementController.SetValueFromRenderer(Editor.TextProperty, buffer.Text);

			UpdateTextColor();
		}

		private void TextViewFocused(object o, FocusedArgs args)
		{
			ElementController.SetValueFromRenderer(VisualElement.IsFocusedPropertyKey, true);
		}

		private void TextViewFocusedOut(object o, FocusOutEventArgs args)
		{
			ElementController.SetValueFromRenderer(VisualElement.IsFocusedPropertyKey, false);
			EditorController.SendCompleted();
		}

		private static void AdjustMinimumHeight(TextView textView, FontDescription font = null)
		{
			var fDescr = font != null ? font : textView.GetDefaultFont(Gtk.StateFlags.Normal);
			var metrics = textView.PangoContext.GetMetrics(font, Language.Default);
			var pangoUnits = (metrics.Ascent + metrics.Descent) / Pango.Scale.PangoScale;

			// Gtk 4 removed the screen resolution (it went with GdkScreen), and the Pango context
			// already resolves font sizes for the display it belongs to - so the metrics above are
			// in device units and no dots-per-inch conversion is left to do. The Gtk 3 code scaled
			// by resolution/72 to get there from points.
			var minHeight = (int)pangoUnits;

			if (textView.HeightRequest < minHeight)
			{
				textView.HeightRequest = minHeight;
			}
		}

		private void UpdateMaxLength()
		{
			Control.SetMaxLength(Element.MaxLength);
		}

		private void UpdatePlaceholder()
		{
			if (Element.Placeholder != Control.PlaceholderText)
			{
				Control.PlaceholderText = Element.Placeholder;
			}
		}

		private void UpdatePlaceholderColor()
		{
			if (!Element.PlaceholderColor.IsDefaultOrTransparent())
			{
				var placeholderColor = Element.PlaceholderColor.ToGtkColor();

				Control.SetPlaceholderTextColor(placeholderColor);
			}
		}
	}
}
