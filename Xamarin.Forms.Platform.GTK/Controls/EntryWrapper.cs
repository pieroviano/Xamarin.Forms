using System;
using Xamarin.Forms.Platform.GTK.Extensions;
using Gtk;
using Pango;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	// Created a custom control to allow combining Gtk.Entry and Gtk.Label to have placeholder text.
	public class EntryWrapper : EventBox
	{
		private Gtk.Overlay _overlay;
		private Gtk.Entry _entry;
		private Gtk.Label _placeholder;
		private EventBox _placeholderContainer;

		public EntryWrapper()
		{
			// A Gtk.Overlay, not two children attached to the same Gtk.Grid cell. Gtk 3's GtkGrid
			// tolerated overlapping attachments and simply stacked them; Gtk 4's does not, and the
			// pair sent it into an endless measure/allocate cycle that hung the process outright
			// (MEASURED: PropertyMappingTests.EntryMapsTextPlaceholderAndPassword never returned).
			// GtkOverlay is the widget Gtk 4 provides for exactly this - one child on top of
			// another - and it is what Controls/ShellWidget already uses for the flyout.
			_overlay = new Gtk.Overlay();
			_entry = new Gtk.Entry();
			_entry.FocusOutEvent += EntryFocusedOut;
			_entry.Changed += EntryChanged;
			_placeholder = new Gtk.Label();

			_placeholderContainer = new EventBox();
			_placeholderContainer.BorderWidth = 2;
			_placeholderContainer.Add(_placeholder);
			_placeholderContainer.ButtonPressEvent += PlaceHolderContainerPressed;

			SetBackgroundColor(_entry.GetDefaultBaseColor(Gtk.StateFlags.Normal));

			Add(_overlay);

			_overlay.Child = _entry;
			_overlay.AddOverlay(_placeholderContainer);

			ShowPlaceholderIfNeeded();
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
		/// ShowPlaceholderIfNeeded stays inline, but only because it is idempotent - see there. Under
		/// Gtk 3 it merely restacked a GdkWindow and could not queue a resize at all; it now toggles
		/// a child's visibility, which does, so the guard is what keeps this call legal here.
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

		/// <remarks>
		/// OnGrabFocus, not Gtk 3's OnFocusGrabbed: Gtk 4 renamed the vfunc and gave it a return
		/// value - true means "focus was taken". Forwarding to the inner widget and reporting what
		/// IT says is the honest answer; the Gtk 3 version could only forward and hope.
		/// </remarks>
		protected override bool OnGrabFocus()
		{
			return _entry?.GrabFocus() ?? false;
		}

		/// <remarks>
		/// <para>Shows or hides the placeholder, where the Gtk 3 code restacked GdkWindows to put
		/// one of the two on top. There are no per-widget windows in Gtk 4, and the overlay child
		/// is always above the main child by construction - so "which one is visible" is the only
		/// question left, and it is the one that was really being asked.</para>
		///
		/// <para>The guard is load-bearing, not tidiness. This runs from ::changed - which Gtk
		/// emits SYNCHRONOUSLY from inside gtk_editable_set_text - and from size-allocate. Toggling
		/// visibility queues a resize, so an unconditional assignment turns "set the entry's text"
		/// into resize, allocate, set visibility, resize, ... and the native setter never returns.
		/// MEASURED: PropertyMappingTests.EntryMapsTextPlaceholderAndPassword hung the whole test
		/// process there, with the stack stopped inside the P/Invoke for Gtk.Entry.set_Text.</para>
		/// </remarks>
		private void ShowPlaceholderIfNeeded()
		{
			var shouldShow = string.IsNullOrEmpty(_entry.Text) && !string.IsNullOrEmpty(_placeholder.Text);

			if (_placeholderContainer.Visible != shouldShow)
				_placeholderContainer.Visible = shouldShow;
		}

		private void PlaceHolderContainerPressed(object o, ButtonPressEventArgs args)
		{
			if (Sensitive)
			{
				_entry.Sensitive = true;
				// GrabFocus - see VisualElementRenderer for why has-focus can no longer be assigned.
				_entry.GrabFocus();
				_placeholderContainer.Visible = false;
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
