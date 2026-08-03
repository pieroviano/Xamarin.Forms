using System;
using Xamarin.Forms.Platform.GTK.Extensions;
using Gtk;
using Pango;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	// Created a custom control to allow combining Gtk.Entry and Gtk.Label to have placeholder text.
	public class EntryWrapper : EventBox
	{
		private Gtk.Grid _table;
		private Gtk.Entry _entry;
		private Gtk.Label _placeholder;
		private EventBox _placeholderContainer;

		public EntryWrapper()
		{
			_table = new Gtk.Grid { RowHomogeneous = true, ColumnHomogeneous = true };
			_entry = new Gtk.Entry();
			_entry.FocusOutEvent += EntryFocusedOut;
			_entry.Changed += EntryChanged;
			_placeholder = new Gtk.Label();

			_placeholderContainer = new EventBox();
			_placeholderContainer.BorderWidth = 2;
			_placeholderContainer.Add(_placeholder);
			_placeholderContainer.ButtonPressEvent += PlaceHolderContainerPressed;

			SetBackgroundColor(_entry.GetDefaultBaseColor(Gtk.StateFlags.Normal));

			Add(_table);

			_table.Attach(_entry, 0, 0, 1, 1);
			_table.Attach(_placeholderContainer, 0, 0, 1, 1);
		}

		public Gtk.Entry Entry => _entry;

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
			_entry.SetBaseColor(color);
			_placeholderContainer.SetBackgroundColor(color, StateType.Normal);
		}

		public void SetTextColor(Gdk.Color color)
		{
			_entry.SetTextColor(color);
		}

		public void SetPlaceholderTextColor(Gdk.Color color)
		{
			_placeholder.SetForegroundColor(color, StateType.Normal);
		}

		public void SetAlignment(float aligmentValue)
		{
			_entry.Alignment = aligmentValue;
			_placeholder.Xalign = aligmentValue;
			_placeholder.Yalign = 0.5f;
		}

		public void SetFont(FontDescription fontDescription)
		{
			_entry.SetFont(fontDescription);
			_placeholder.SetFont(fontDescription);
		}

		public void SetMaxLength(int maxLength)
		{
			_entry.MaxLength = maxLength;
		}

		Gdk.Rectangle _lastAllocation = Gdk.Rectangle.Zero;
		bool _entryResizeQueued;

		/// <summary>
		/// Keeps the entry filling the wrapper, so the placeholder label stacked on top of it in
		/// the same grid cell covers exactly the same area.
		/// </summary>
		/// <remarks>
		/// The size request is deferred to idle, never applied inline. SetSizeRequest calls
		/// gtk_widget_queue_resize, and doing that from inside a size-allocate handler is invalid
		/// in GTK3: the resize is not merely lost, the alloc-needed flag is left standing on this
		/// widget's ancestors and gtk_widget_queue_resize_internal bails out at the first ancestor
		/// that already carries it - so every later resize raised anywhere in that subtree was
		/// swallowed too. Same treatment as Controls.Page.OnContentContainerWrapperSizeAllocated.
		///
		/// ShowPlaceholderIfNeeded stays inline: it only raises a GdkWindow, it queues no resize.
		/// </remarks>
		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			if (_lastAllocation != allocation)
			{
				_lastAllocation = allocation;

				if (!_entryResizeQueued)
				{
					_entryResizeQueued = true;

					GLib.Idle.Add(() =>
					{
						// A pending idle can outlive the entry; a destroyed or disposed GtkSharp
						// wrapper is left holding a null handle, so check that too and not only
						// the field. _lastAllocation is read here, not captured, so an allocation
						// that arrived while this was queued still lands.
						_entryResizeQueued = false;

						if (_entry != null && _entry.Handle != IntPtr.Zero)
							_entry.SetSizeRequest(_lastAllocation.Width, _lastAllocation.Height);

						return false;
					});
				}
			}

			ShowPlaceholderIfNeeded();
		}

		protected override void OnFocusGrabbed()
		{
			_entry?.GrabFocus();
		}

		private void ShowPlaceholderIfNeeded()
		{
			if (string.IsNullOrEmpty(_entry.Text) && !string.IsNullOrEmpty(_placeholder.Text))
			{
				_placeholderContainer.Window?.Raise();
			}
			else
			{
				_entry.Window?.Raise();
			}
		}

		private void PlaceHolderContainerPressed(object o, ButtonPressEventArgs args)
		{
			if (Sensitive)
			{
				_entry.Sensitive = true;
				_entry.HasFocus = true;
				_entry.Window?.Raise();
			}
		}

		private void EntryFocusedOut(object o, FocusOutEventArgs args)
		{
			ShowPlaceholderIfNeeded();
		}

		private void EntryChanged(object sender, EventArgs e)
		{
			ShowPlaceholderIfNeeded();
		}
	}
}
