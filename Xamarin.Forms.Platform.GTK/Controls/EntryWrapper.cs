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

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			_entry.SetSizeRequest(allocation.Width, allocation.Height);

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
