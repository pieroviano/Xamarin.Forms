using Gtk;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	public class ScrolledTextView : EventBox
	{
		private Gtk.Grid _table;
		private ScrolledWindow _scrolledWindow;
		private Gtk.Label _placeholder;
		private EventBox _placeholderContainer;
		private int _maxLength;

		public ScrolledTextView()
		{
			_table = new Gtk.Grid { RowHomogeneous = true, ColumnHomogeneous = true };

			TextView = new TextView
			{
				AcceptsTab = false,
				WrapMode = WrapMode.WordChar
			};

			TextView.Buffer.InsertText += InsertText;
			TextView.FocusOutEvent += FocusedOut;

			_scrolledWindow = new ScrolledWindow
			{
				ShadowType = ShadowType.In,
				HscrollbarPolicy = PolicyType.Never,
				VscrollbarPolicy = PolicyType.Automatic
			};

			_scrolledWindow.Add(TextView);

			_placeholder = new Gtk.Label();
			_placeholder.Xalign = 0;
			_placeholder.Yalign = 0;

			_placeholderContainer = new EventBox
			{
				BorderWidth = 2
			};

			_placeholderContainer.Add(_placeholder);

			_placeholderContainer.ButtonPressEvent += PlaceHolderContainerPressed;

			SetBackgroundColor(TextView.GetDefaultBaseColor(Gtk.StateFlags.Normal));

			Add(_table);

			_table.Attach(_placeholderContainer, 0, 0, 1, 1);
			_table.Attach(_scrolledWindow, 0, 0, 1, 1);
		}

		public TextView TextView { get; }

		public string PlaceholderText
		{
			get
			{
				return _placeholder.Text;
			}
			set
			{
				_placeholder.Text = value ?? string.Empty;
			}
		}

		public void SetBackgroundColor(Gdk.Color color)
		{
			StyleExtensions.SetBackgroundColor(this, color);
			TextView.SetBaseColor(color);
			_placeholderContainer.SetBackgroundColor(color, StateType.Normal);
		}

		public void SetPlaceholderTextColor(Gdk.Color color)
		{
			_placeholder.SetForegroundColor(color, StateType.Normal);
		}

		public void SetMaxLength(int maxLength)
		{
			_maxLength = maxLength;

			if (TextView.Buffer.CharCount > maxLength)
				TextView.Buffer.Text = TextView.Buffer.Text.Substring(0, maxLength);
		}

		protected override void OnFocusGrabbed()
		{
			TextView?.GrabFocus();
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			TextView.SetSizeRequest(allocation.Width, allocation.Height);

			ShowPlaceholderIfNeeded();
		}

		private void InsertText(object o, InsertTextArgs args)
		{
			args.RetVal = args.NewTextLength <= _maxLength;
		}

		private void FocusedOut(object o, FocusOutEventArgs args)
		{
			ShowPlaceholderIfNeeded();
		}

		private void ShowPlaceholderIfNeeded()
		{
			if (string.IsNullOrEmpty(TextView.Buffer.Text) && !string.IsNullOrEmpty(_placeholder.Text))
			{
				_placeholderContainer.VisibleWindow = true;
			}
			else
			{
				_placeholderContainer.VisibleWindow = false;
			}
		}

		private void PlaceHolderContainerPressed(object o, ButtonPressEventArgs args)
		{
			if (Sensitive)
			{
				TextView.Sensitive = true;
				TextView.HasFocus = true;
				TextView.Window?.Raise();
			}
		}
	}
}
