using System;
using Xamarin.Forms.Platform.GTK.Extensions;
using System.Linq;
using Gtk;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	public class DateEventArgs : EventArgs
	{
		DateTime _date;

		public DateTime Date
		{
			get
			{
				return _date;
			}
		}

		public DateEventArgs(DateTime date)
		{
			_date = date;
		}
	}

	/// <summary>The calendar drop-down of <see cref="DatePicker"/>.</summary>
	/// <remarks>
	/// A <see cref="Gtk.Popover"/>, where Gtk 3 used a borderless WindowType.Popup toplevel that
	/// positioned itself by hand and took a seat grab. Gtk 4 removed all three of those - there is
	/// no popup window type, no gtk_window_move, and no application-driven pointer grab - and a
	/// popover is what replaced them wholesale: Autohide gives it the implicit grab and the
	/// dismiss-on-outside-click, and PointingTo gives it the placement. That is why this class
	/// lost roughly half its code rather than gaining a workaround for each.
	///
	/// It is no longer a toplevel, so it is reached through the widget that owns it rather than by
	/// walking Gtk.Window.ListToplevels; see DatePicker.ClosePicker.
	/// </remarks>
	public partial class DatePickerWindow : Popover
	{
		Box _datebox;
		RangeCalendar _calendar;

		public DatePickerWindow()
		{
			BuildDatePickerWindow();
			SelectedDate = DateTime.Now;
		}

		public DateTime SelectedDate
		{
			get
			{
				return _calendar.SelectedDate;
			}

			set
			{
				_calendar.SelectedDate = value;
			}
		}

		public DateTime MinimumDate
		{
			get
			{
				return _calendar.MinimumDate;
			}

			set
			{
				_calendar.MinimumDate = value;
			}
		}

		public DateTime MaximumDate
		{
			get
			{
				return _calendar.MaximumDate;
			}

			set
			{
				_calendar.MaximumDate = value;
			}
		}

		public delegate void DateEventHandler(object sender, DateEventArgs args);

		public event DateEventHandler OnDateTimeChanged;

		// The 1px Cairo frame this used to stroke is gone with the window that needed it. A
		// WindowType.Popup toplevel had no decoration whatsoever, so the border had to be drawn by
		// hand from the theme's insensitive foreground colour; a Gtk 4 popover is a themed widget
		// and gets its border, background and shadow from CSS like anything else. Drawing over
		// that would put a second, differently-coloured line inside the real one.

		protected virtual void OnCalendarDaySelected(object sender, EventArgs e)
		{
			OnDateTimeChanged?.Invoke(this, new DateEventArgs(SelectedDate));
		}

		private void BuildDatePickerWindow()
		{
			// Autohide is the whole of what GrabHelper used to do: Gtk takes the implicit grab
			// for the popover and dismisses it when the user clicks outside. The Gtk 3 code
			// needed an explicit seat grab AND a button-press handler on the popup to close it,
			// and had to undo both by hand on every exit path.
			Autohide = true;
			HasArrow = false;

			_datebox = new Box(Gtk.Orientation.Vertical, 0);
			_datebox.Spacing = 6;
			_datebox.BorderWidth = 3;

			_calendar = new RangeCalendar();
			_calendar.CanFocus = true;
			_calendar.ShowHeading = true;
			_datebox.Add(_calendar);

			Child = _datebox;

			_calendar.DaySelected += new EventHandler(OnCalendarDaySelected);

			// Double click confirms and dismisses, as GtkCalendar::day-selected-double-click did.
			// Gtk 4 deleted that signal - a click's repeat count now comes from a GtkGestureClick
			// - so the gesture is attached explicitly rather than the signal being handled.
			var confirm = new GestureClick();
			confirm.Pressed += (o, args) =>
			{
				if (args.NPress < 2)
					return;

				OnDateTimeChanged?.Invoke(this, new DateEventArgs(SelectedDate));
				Close();
			};
			_calendar.AddController(confirm);
		}

		/// <summary>Dismisses the drop-down.</summary>
		/// <remarks>
		/// Popdown, not Destroy: a popover is owned by the widget it is parented to and is meant
		/// to be shown again, where the Gtk 3 popup was a throwaway toplevel built per open.
		/// </remarks>
		internal void Close()
		{
			Popdown();
		}

		void NotifyDateChanged()
		{
			OnDateTimeChanged?.Invoke(this, new DateEventArgs(SelectedDate));
		}

		class RangeCalendar : Calendar
		{
			DateTime _minimumDate;
			DateTime _maximumDate;

			public RangeCalendar()
			{
				_minimumDate = new DateTime(1900, 1, 1);
				_maximumDate = new DateTime(2199, 1, 1);
			}

			public DateTime MinimumDate
			{
				get
				{
					return _minimumDate;
				}

				set
				{
					if (MaximumDate < value)
					{
						throw new InvalidOperationException($"{nameof(MinimumDate)} must be lower than {nameof(MaximumDate)}");
					}

					_minimumDate = value;
				}
			}

			public DateTime MaximumDate
			{
				get
				{
					return _maximumDate;
				}

				set
				{
					if (MinimumDate > value)
					{
						throw new InvalidOperationException($"{nameof(MaximumDate)} must be greater than {nameof(MinimumDate)}");
					}

					_maximumDate = value;
				}
			}

			/// <summary>
			/// The selected day, as a <see cref="System.DateTime"/>.
			/// </summary>
			/// <remarks>
			/// Gtk 4 exchanges GtkCalendar's date as a GDateTime, where Gtk 3 used separate
			/// year/month/day integers that the binding surfaced as a System.DateTime. The two
			/// are converted through their broken-down fields rather than a Unix timestamp - see
			/// GLib.DateTime.ToSystemDateTime - because a timestamp round trip shifts the date
			/// across midnight outside UTC, which is precisely what a date picker must not do.
			/// </remarks>
			public DateTime SelectedDate
			{
				get { return Date.ToSystemDateTime(); }
				set { Date = GLib.DateTime.FromSystemDateTime(value); }
			}

			protected override void OnDaySelected()
			{
				if (SelectedDate < MinimumDate)
				{
					SelectedDate = MinimumDate;
				}

				if (SelectedDate > MaximumDate)
				{
					SelectedDate = MaximumDate;
				}
			}
		}
	}

	public partial class DatePicker : EventBox
	{
		CustomComboBox _comboBox;
		DatePickerWindow _picker;
		Gdk.Color _color;
		DateTime _currentDate;
		DateTime _minDate;
		DateTime _maxDate;
		string _dateFormat;

		public event EventHandler DateChanged;
		public event EventHandler GotFocus;
		public event EventHandler LostFocus;

		public DatePicker()
		{
			BuildDatePicker();

			CurrentDate = DateTime.Now;

			TextColor = _comboBox.Entry.GetDefaultTextColor(Gtk.StateFlags.Normal);

			// CanDefault is gone: Gtk 4 dropped the can-default/has-default pair, so a widget is
			// the default one by being set as the window's default widget and by nothing else.
			_comboBox.Entry.CanFocus = false;
			_comboBox.Entry.IsEditable = false;
			_comboBox.Entry.SetStateFlags(StateFlags.Normal, true);
			// Focused, not Gtk 3's grab-focus signal: Gtk 4 has no signal for "focus was grabbed",
			// and a GtkEventControllerFocus reports focus arriving however it arrived - which is
			// what this wants, since the popup should open when the entry gains focus at all.
			_comboBox.Entry.Focused += OnEntryFocused;
			_comboBox.PopupButton.Clicked += new EventHandler(OnBtnShowCalendarClicked);
		}

		public DateTime CurrentDate
		{
			get
			{
				return _currentDate;
			}
			set
			{
				if (_currentDate == value)
					return;

				_currentDate = value;
				UpdateEntryText();

				// Raised here rather than only from the calendar popup, which is what the sibling
				// Controls.TimePicker effectively does (its CurrentTime setter retexts the entry,
				// whose Changed handler raises TimeChanged). Two things were wrong without it:
				// DatePickerRenderer.OnDateChanged - the whole native-to-element direction - never
				// fired for any change that did not come from the popup, and OnPopupDateChanged's
				// two clamping branches returned before their explicit Invoke, so picking a date
				// outside Min/MaxDate moved the control and left the element stale.
				//
				// The guard above is what keeps this from echoing: the renderer's UpdateDate sets
				// this property from the element, so the round trip back through
				// SetValueFromRenderer must settle, and an unchanged value now stops at the door.
				DateChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public DateTime MinDate
		{
			get
			{
				return _minDate;
			}
			set
			{
				_minDate = value;
			}
		}

		public DateTime MaxDate
		{
			get
			{
				return _maxDate;
			}
			set
			{
				_maxDate = value;
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

		public string DateFormat
		{
			get
			{
				return _dateFormat;
			}
			set
			{
				_dateFormat = value;
				UpdateEntryText();
			}
		}

		public void SetBackgroundColor(Gdk.Color color)
		{
			_comboBox.SetBackgroundColor(color);
		}

		public void OpenPicker()
		{
			ShowPickerWindow();
		}

		public void ClosePicker()
		{
			// The popover this control owns, rather than a search of Gtk.Window.ListToplevels for
			// one of the right type. That search existed because the Gtk 3 popup was a toplevel
			// built fresh on every open and reachable no other way; it also found the wrong one
			// when two date pickers were on screen. A popover is a child of the widget it points
			// at, so the owner simply keeps it.
			_picker?.Close();
		}

		void ShowPickerWindow()
		{
			if (_picker == null)
			{
				_picker = new DatePickerWindow();
				_picker.OnDateTimeChanged += OnPopupDateChanged;
				_picker.Closed += OnPickerClosed;

				// Parented to this control, which is also what positions it: a popover appears
				// against its parent, so the Gtk 3 dance of reading the toplevel's screen origin
				// and calling Move is gone with the API that made it necessary.
				_picker.Parent = this;
				_picker.Position = PositionType.Bottom;
			}

			_picker.SelectedDate = CurrentDate;
			_picker.MinimumDate = _minDate;
			_picker.MaximumDate = _maxDate;
			_picker.Popup();
		}

		void OnPopupDateChanged(object sender, DateEventArgs args)
		{
			var date = args.Date;

			if (date < MinDate)
			{
				CurrentDate = MinDate;
				return;
			}

			if (date > MaxDate)
			{
				CurrentDate = MaxDate;
				return;
			}

			// The setter raises DateChanged, including for the two clamped paths above.
			CurrentDate = args.Date;
		}

		void BuildDatePicker()
		{
			_comboBox = new CustomComboBox();
			Add(_comboBox);

			if ((Child != null))
			{
				Child.ShowAll();
			}

			Visible = true;
		}

		void UpdateEntryText()
		{
			_comboBox.Entry.Text = _currentDate.ToString(string.IsNullOrEmpty(_dateFormat) ? "D" : _dateFormat);
		}

		void OnBtnShowCalendarClicked(object sender, EventArgs e)
		{
			ShowPickerWindow();
		}

		void OnEntryFocused(object sender, FocusedArgs e)
		{
			ShowPickerWindow();
			GotFocus?.Invoke(this, EventArgs.Empty);
		}

		void OnPickerClosed(object sender, EventArgs e)
		{
			// No Remove: the popover is not a child of this container, and the Gtk 3 code's
			// Remove(window) never did anything either - it was gtk_container_remove against a
			// toplevel that was never its child, which GTK logged a critical for and ignored.
			// The popover is kept for the next open and unparented in Dispose.
			LostFocus?.Invoke(this, EventArgs.Empty);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && _picker != null)
			{
				_picker.OnDateTimeChanged -= OnPopupDateChanged;
				_picker.Closed -= OnPickerClosed;

				// Gtk 4 warns if a widget is finalized while it still has a child, and a popover
				// is parented to the widget it points at rather than owned by it.
				_picker.Unparent();
				_picker = null;
			}

			base.Dispose(disposing);
		}
	}
}
