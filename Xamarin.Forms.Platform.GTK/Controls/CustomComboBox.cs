using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	// CustomCombobox, Gtk.Entry + Gtk.Button
	public class CustomComboBox : Gtk.Box
	{
		private Gtk.Entry _entry;
		private Gtk.Button _button;
		private Gtk.Image _arrow;
		private Gdk.Color _color;

		public CustomComboBox()
		{
			BuildCustomComboBox();
		}

		public Gtk.Entry Entry
		{
			get
			{
				return _entry;
			}
		}

		public Gtk.Button PopupButton
		{
			get
			{
				return _button;
			}
		}

		public Gdk.Color Color
		{
			get { return _color; }
			set
			{
				_color = value;
				Entry.SetTextColor(_color);
			}
		}

		public void SetBackgroundColor(Gdk.Color color)
		{
			// Uses its argument. It previously ignored `color` entirely and hardcoded a red frame
			// and a blue entry - leftover debugging colours - so every Forms DatePicker and
			// TimePicker rendered red/blue no matter what BackgroundColor the app set, including
			// the default.
			StyleExtensions.SetBackgroundColor(this, color);
			Entry.SetBaseColor(color);
		}

		private void BuildCustomComboBox()
		{
			_entry = new Gtk.Entry();
			_entry.CanFocus = true;
			_entry.IsEditable = true;
			PackStart(_entry, true, true, 0);

			_button = new Gtk.Button();
			_button.WidthRequest = 30;
			_button.CanFocus = true;
			// GTK3 deprecates Gtk.Arrow; the themed "pan-down" icon is the replacement it
			// recommends, and it follows the icon theme instead of drawing a fixed glyph.
			// NewFromIconName, and no size. Gtk 4 removed the (name, size) constructor along with
			// GtkIconSize's pixel sizes: an icon now sizes itself from CSS, like text does, so
			// asking for "button size" here would be overriding the theme rather than following it.
			_arrow = Gtk.Image.NewFromIconName("pan-down-symbolic");
			_button.Add(_arrow);
			PackEnd(_button, false, false, 0);
		}
	}
}
