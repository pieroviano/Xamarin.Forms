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
		// int.MaxValue, not 0. Forms' Editor.MaxLength defaults to int.MaxValue and the renderer
		// only pushes it down when it maps the property; a 0 default would make the enforcement below
		// truncate every Editor to empty before that happened.
		private int _maxLength = int.MaxValue;

		public ScrolledTextView()
		{
			_table = new Gtk.Grid { RowHomogeneous = true, ColumnHomogeneous = true };

			TextView = new TextView
			{
				AcceptsTab = false,
				WrapMode = WrapMode.WordChar
			};

			// Connected here, in the constructor, so this handler runs BEFORE the one EditorRenderer
			// attaches - GTK invokes handlers in connection order, and the renderer must not observe
			// the over-long intermediate text and push it back into the Forms element.
			TextView.Buffer.Changed += EnforceMaxLength;
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

		/// <summary>
		/// Enforces MaxLength by trimming the buffer back after a change.
		/// </summary>
		/// <remarks>
		/// This replaces an insert-text handler that did nothing:
		/// <c>args.RetVal = args.NewTextLength &lt;= _maxLength</c>. Two independent reasons it
		/// could not work - GtkTextBuffer::insert-text returns void, so a handler's RetVal is
		/// ignored, and NewTextLength is the length of the text BEING INSERTED rather than of the
		/// resulting buffer. An Editor with MaxLength set accepted unlimited input.
		///
		/// Cancelling the insertion would be the tidier fix, but that needs
		/// g_signal_stop_emission_by_name and GtkSharp's GLib.Signal binds only Emit/AddDelegate/
		/// RemoveDelegate/AddEmissionHook - there is no managed way to stop an emission. Trimming
		/// afterwards reaches the same end state.
		/// </remarks>
		private void EnforceMaxLength(object sender, System.EventArgs args)
		{
			var buffer = TextView.Buffer;

			if (_maxLength < 0 || buffer.CharCount <= _maxLength)
				return;

			// Delete re-enters this handler; the guard above then returns immediately.
			var start = buffer.GetIterAtOffset(_maxLength);
			var end = buffer.EndIter;

			buffer.Delete(ref start, ref end);
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
