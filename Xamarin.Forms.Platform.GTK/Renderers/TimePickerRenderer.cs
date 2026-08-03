using System;
using System.ComponentModel;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public class TimePickerRenderer : ViewRenderer<TimePicker, Controls.TimePicker>
	{
		private bool _disposed;

		protected override bool PreventGestureBubbling { get; set; } = true;

		protected override void UpdateBackgroundColor()
		{
			base.UpdateBackgroundColor();

			if (!Element.BackgroundColor.IsDefaultOrTransparent())
			{
				Control.SetBackgroundColor(Element.BackgroundColor.ToGtkColor());
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				if (Control != null)
				{
					Control.GotFocus -= GotFocus;
					Control.LostFocus -= LostFocus;
					Control.TimeChanged -= OnTimeChanged;
				}

				_disposed = true;
			}

			base.Dispose(disposing);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<TimePicker> e)
		{
			if (e.NewElement != null)
			{
				if (Control == null)
				{
					// A time picker custom control
					var timePicker = new Controls.TimePicker();
					timePicker.GotFocus += GotFocus;
					timePicker.LostFocus += LostFocus;
					timePicker.TimeChanged += OnTimeChanged;
					SetNativeControl(timePicker);
				}

				UpdateTime();
				UpdateTextColor();
				UpdateFormat();
			}

			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == TimePicker.TimeProperty.PropertyName)
				UpdateTime();
			if (e.PropertyName == TimePicker.TextColorProperty.PropertyName)
				UpdateTextColor();
			else if (e.PropertyName == TimePicker.FormatProperty.PropertyName)
				UpdateFormat();
		}

		internal override void OnElementFocusChangeRequested(object sender, VisualElement.FocusRequestArgs args)
		{
			if (Control == null)
				return;

			if (args.Focus)
				args.Result = OpenPicker();
			else
				args.Result = ClosePicker();

			base.OnElementFocusChangeRequested(sender, args);
		}

		private void UpdateTime()
		{
			if (Control == null || Element == null)
				return;

			if (Element.Time.Equals(default(TimeSpan)))
				Control.CurrentTime = DateTime.Now.TimeOfDay;
			else
				Control.CurrentTime = Element.Time;
		}

		private void UpdateTextColor()
		{
			var textColor = Element.TextColor.ToGtkColor();
			Control.TextColor = textColor;
		}
		private void UpdateFormat()
		{
			Control.TimeFormat = Element.Format;
		}

		private void GotFocus(object sender, EventArgs e)
		{
			ElementController.SetValueFromRenderer(VisualElement.IsFocusedPropertyKey, true);
		}

		private void LostFocus(object sender, EventArgs e)
		{
			ElementController.SetValueFromRenderer(VisualElement.IsFocusedPropertyKey, false);
		}

		private void OnTimeChanged(object sender, EventArgs e)
		{
			// The TimeSpan straight through - NOT `DateTime.Today + Control.CurrentTime`.
			// TimePicker.TimeProperty is typed TimeSpan, and BindableProperty.TryConvert has no
			// DateTime->TimeSpan route (no SimpleConvertTypes entry, no known converter, no
			// implicit operator), so SetValueCore logged "Cannot convert ... to type
			// 'System.TimeSpan'" and returned WITHOUT assigning. Every user time selection was
			// therefore discarded: the entry showed the new time while Element.Time kept the old
			// one and two-way bindings never fired. Even had it converted, TimeProperty's
			// validator (TotalHours < 24) would have rejected a Today-based value.
			ElementController?.SetValueFromRenderer(TimePicker.TimeProperty, Control.CurrentTime);
		}

		private bool OpenPicker()
		{
			if (Control == null)
			{
				return false;
			}

			Control.OpenPicker();

			return true;
		}

		private bool ClosePicker()
		{
			if (Control == null)
			{
				return false;
			}

			Control.ClosePicker();

			return true;
		}
	}
}
