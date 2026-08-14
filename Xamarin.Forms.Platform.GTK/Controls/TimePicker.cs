using System;
using Xamarin.Forms.Platform.GTK.Extensions;
using System.Linq;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	public class TimeEventArgs : EventArgs
	{
		private TimeSpan _time;

		public TimeSpan Time
		{
			get
			{
				return _time;
			}
		}

		public TimeEventArgs(TimeSpan time)
		{
			_time = time;
		}
	}

	/// <summary>The hour/minute/second drop-down of <see cref="TimePicker"/>.</summary>
	/// <remarks>
	/// A <see cref="Gtk.Popover"/>, for the reasons set out on
	/// <see cref="DatePickerWindow"/>: Gtk 4 removed the popup window type, hand positioning and
	/// application-driven grabs that the Gtk 3 version was built from, and a popover replaces all
	/// three at once.
	/// </remarks>
	public class TimePickerWindow : Gtk.Popover
	{
		private Gtk.Box _timeBox;
		private Gtk.Label _labelHour;
		private Gtk.SpinButton _txtHour;
		private Gtk.Label _labelMin;
		private Gtk.SpinButton _txtMin;
		private Gtk.Label _labelSec;
		private Gtk.SpinButton _txtSec;

		public TimePickerWindow()
		{
			BuildTimePickerWindow();

			RefreshTime();
		}

		public TimeSpan CurrentTime
		{
			get
			{
				return new TimeSpan((int)_txtHour.Value, (int)_txtMin.Value, (int)_txtSec.Value);
			}

			set
			{
				_txtHour.Value = value.Hours;
				_txtMin.Value = value.Minutes;
				_txtSec.Value = value.Seconds;
			}
		}

		public delegate void TimeEventHandler(object sender, TimeEventArgs args);

		public event TimeEventHandler OnTimeChanged;

		// The hand-stroked 1px frame is gone with the undecorated toplevel that needed it; a
		// popover is themed. See the same note on DatePickerWindow.

		private void BuildTimePickerWindow()
		{
			// Autohide replaces the seat grab AND the click-outside-to-close handler. See
			// DatePickerWindow.BuildDatePickerWindow.
			Autohide = true;
			HasArrow = false;

			_timeBox = new Gtk.Box(Gtk.Orientation.Horizontal, 0);
			_timeBox.Spacing = 6;
			_timeBox.BorderWidth = 3;

			_labelHour = new Gtk.Label();
			_labelHour.LabelProp = "H:";
			_timeBox.Add(_labelHour);

			Gtk.Box.BoxChild w2 = ((Gtk.Box.BoxChild)(_timeBox[_labelHour]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;

			_txtHour = new Gtk.SpinButton(0D, 24D, 1D);
			_txtHour.CanFocus = true;
			_txtHour.Adjustment.PageIncrement = 1D;
			_txtHour.ClimbRate = 1D;
			_txtHour.Numeric = true;
			_timeBox.Add(_txtHour);

			Gtk.Box.BoxChild w3 = ((Gtk.Box.BoxChild)(_timeBox[_txtHour]));
			w3.Position = 1;
			w3.Expand = false;
			w3.Fill = false;

			_labelMin = new Gtk.Label();
			_labelMin.LabelProp = "M:";
			_timeBox.Add(_labelMin);
			Gtk.Box.BoxChild w4 = ((Gtk.Box.BoxChild)(_timeBox[_labelMin]));

			w4.Position = 2;
			w4.Expand = false;
			w4.Fill = false;

			_txtMin = new Gtk.SpinButton(0D, 60D, 1D);
			_txtMin.CanFocus = true;
			_txtMin.Adjustment.PageIncrement = 10D;
			_txtMin.ClimbRate = 1D;
			_txtMin.Numeric = true;
			_timeBox.Add(_txtMin);

			Gtk.Box.BoxChild w5 = ((Gtk.Box.BoxChild)(_timeBox[_txtMin]));
			w5.Position = 3;
			w5.Expand = false;
			w5.Fill = false;

			_labelSec = new Gtk.Label();
			_labelSec.LabelProp = "S:";
			_timeBox.Add(_labelSec);
			Gtk.Box.BoxChild w6 = ((Gtk.Box.BoxChild)(_timeBox[_labelSec]));
			w6.Position = 4;
			w6.Expand = false;
			w6.Fill = false;

			_txtSec = new Gtk.SpinButton(0D, 60D, 1D);
			_txtSec.CanFocus = true;
			_txtSec.Adjustment.PageIncrement = 10D;
			_txtSec.ClimbRate = 1D;
			_txtSec.Numeric = true;
			_timeBox.Add(_txtSec);

			Gtk.Box.BoxChild w7 = ((Gtk.Box.BoxChild)(_timeBox[_txtSec]));
			w7.Position = 5;
			w7.Expand = false;
			w7.Fill = false;

			Child = _timeBox;

			_txtHour.ValueChanged += new EventHandler(OnTxtHourValueChanged);
			_txtHour.ButtonPressEvent += new Gtk.ButtonPressEventHandler(OnTxtHourButtonPressEvent);
			_txtMin.ValueChanged += new EventHandler(OnTxtMinValueChanged);
			_txtMin.ButtonPressEvent += new Gtk.ButtonPressEventHandler(OnTxtMinButtonPressEvent);
			_txtSec.ValueChanged += new EventHandler(OnTxtSecValueChanged);
			_txtSec.ButtonPressEvent += new Gtk.ButtonPressEventHandler(OnTxtSecButtonPressEvent);
		}

		/// <summary>Dismisses the drop-down. See <see cref="DatePickerWindow.Close"/>.</summary>
		internal void Close()
		{
			Popdown();
		}

		private void RefreshTime()
		{
			OnTimeChanged?.Invoke(this, new TimeEventArgs(CurrentTime));
		}

		protected virtual void OnTxtHourValueChanged(object sender, EventArgs e)
		{
			if (_txtHour.Value == 24)
				_txtHour.Value = 0;

			RefreshTime();
		}

		protected virtual void OnTxtMinValueChanged(object sender, EventArgs e)
		{
			if (_txtMin.Value == 60)
				_txtMin.Value = 0;

			RefreshTime();
		}

		protected virtual void OnTxtSecValueChanged(object sender, EventArgs e)
		{
			if (_txtSec.Value == 60)
				_txtSec.Value = 0;

			RefreshTime();
		}

		protected virtual void OnTxtHourButtonPressEvent(object o, Gtk.ButtonPressEventArgs args)
		{
			args.RetVal = true;
		}

		protected virtual void OnTxtMinButtonPressEvent(object o, Gtk.ButtonPressEventArgs args)
		{
			args.RetVal = true;
		}

		protected virtual void OnTxtSecButtonPressEvent(object o, Gtk.ButtonPressEventArgs args)
		{
			args.RetVal = true;
		}
	}

	public class TimePicker : GtkFormsContainer
	{
		private const string DefaultTimeFormat = @"hh\:mm\:ss";

		private CustomComboBox _comboBox;
		private TimePickerWindow _picker;
		private Gdk.Color _color;
		private TimeSpan _currentTime;
		private string _timeFormat;

		public event EventHandler TimeChanged;
		public event EventHandler GotFocus;
		public event EventHandler LostFocus;

		public TimePicker()
		{
			BuildTimePicker();

			CurrentTime = new TimeSpan(DateTime.Now.Ticks);

			TextColor = _comboBox.Entry.GetDefaultTextColor(Gtk.StateFlags.Normal);

			_comboBox.Entry.Changed += new EventHandler(OnTxtTimeChanged);
			_comboBox.PopupButton.Clicked += new EventHandler(OnBtnShowTimePickerClicked);
		}

		public TimeSpan CurrentTime
		{
			get
			{
				return _currentTime;
			}
			set
			{
				_currentTime = value;
				UpdateEntryText();
			}
		}

		public Gdk.Color TextColor
		{
			get
			{
				return _color;
			}
			set
			{
				_color = value;
				_comboBox.Color = _color;
			}
		}

		public string TimeFormat
		{
			get
			{
				return _timeFormat;
			}
			set
			{
				_timeFormat = value;
				UpdateEntryText();
			}
		}

		public void SetBackgroundColor(Gdk.Color color)
		{
			_comboBox.SetBackgroundColor(color);
		}

		public void OpenPicker()
		{
			ShowTimePickerWindow();
		}

		public void ClosePicker()
		{
			// The popover this control owns - see the note in DatePicker.ClosePicker for why the
			// Gtk 3 toplevel search is gone.
			_picker?.Close();
		}

		protected virtual void OnTxtTimeChanged(object sender, EventArgs e)
		{
			_comboBox.Entry.SetTextColor(TextColor);

			TimeChanged?.Invoke(this, e);
		}

		protected virtual void OnBtnShowTimePickerClicked(object sender, EventArgs e)
		{
			ShowTimePickerWindow();
		}

		private void ShowTimePickerWindow()
		{
			if (_picker == null)
			{
				_picker = new TimePickerWindow();
				_picker.OnTimeChanged += OnPopupTimeChanged;
				_picker.Closed += OnPickerClosed;

				_picker.Parent = this;
				_picker.Position = Gtk.PositionType.Bottom;
			}

			_picker.CurrentTime = CurrentTime;
			_picker.Popup();

			GotFocus?.Invoke(this, EventArgs.Empty);
		}

		private void OnPopupTimeChanged(object sender, TimeEventArgs args)
		{
			CurrentTime = args.Time;
		}

		private void BuildTimePicker()
		{
			_comboBox = new CustomComboBox();
			Add(_comboBox);

			if ((Child != null))
			{
				Child.ShowAll();
			}

			Visible = true;
		}

		private void UpdateEntryText()
		{
			_comboBox.Entry.Text = string.IsNullOrEmpty(_timeFormat)
				? _currentTime.ToString(DefaultTimeFormat)
				: DateTime.Today.Date.Add(_currentTime).ToString(_timeFormat);
		}

		private void OnPickerClosed(object sender, EventArgs e)
		{
			// No Remove - see the note in DatePicker.OnPickerClosed.
			LostFocus?.Invoke(this, EventArgs.Empty);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && _picker != null)
			{
				_picker.OnTimeChanged -= OnPopupTimeChanged;
				_picker.Closed -= OnPickerClosed;

				// Gtk 4 warns if a widget is finalized while it still has a child.
				_picker.Unparent();
				_picker = null;
			}

			base.Dispose(disposing);
		}
	}
}
