using System;
using Gdk;
using Gtk;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	public class NotebookWrapper : GtkFormsContainer
	{
		private Notebook _noteBook;
		private Pixbuf _backgroundPixbuf;

		public NotebookWrapper()
		{
			Build();
		}

		public Notebook NoteBook => _noteBook;

		public void InsertPage(Widget container, string title, Pixbuf icon, int position)
		{
			var header = new TabbedPageHeader(title ?? string.Empty, icon);
			container.Unparent();

			var wrapper = new NotebookPageWrapper(container);
			_noteBook.InsertPage(
				wrapper,
				header,
				position);
		}

		public void SetTabLabelText(int tabIndex, string label)
		{
			var page = _noteBook.GetNthPage(tabIndex);
			var tabbedPageHeader = _noteBook.GetTabLabel(page) as TabbedPageHeader;

			if (tabbedPageHeader != null)
			{
				tabbedPageHeader.Label.Text = label ?? string.Empty;
			}
		}

		public void SetTabIcon(int tabIndex, Pixbuf pixbuf)
		{
			var page = _noteBook.GetNthPage(tabIndex);
			var tabbedPageHeader = _noteBook.GetTabLabel(page) as TabbedPageHeader;

			if (tabbedPageHeader != null)
			{
				tabbedPageHeader.Icon.Pixbuf = pixbuf;
			}
		}

		public void SetTabBackgroundColor(int tabIndex, Gdk.Color color)
		{
			var page = _noteBook.GetNthPage(tabIndex);
			var tabbedPageHeader = _noteBook.GetTabLabel(page) as TabbedPageHeader;

			if (tabbedPageHeader != null)
			{
				tabbedPageHeader.SetBackgroundColor(color, StateType.Normal);
				tabbedPageHeader.SetBackgroundColor(color, StateType.Active);
			}
		}

		public void SetTabTextColor(int tabIndex, Gdk.Color color)
		{
			var page = _noteBook.GetNthPage(tabIndex);
			var tabbedPageHeader = _noteBook.GetTabLabel(page) as TabbedPageHeader;

			if (tabbedPageHeader != null)
			{
				tabbedPageHeader.Label.SetForegroundColor(color, StateType.Normal);
				tabbedPageHeader.Label.SetForegroundColor(color, StateType.Active);
			}
		}

		public void RemoveAllPages()
		{
			while (_noteBook.NPages > 0)
			{
				_noteBook.RemovePage(0);
			}
		}

		public void RemovePage(Widget widget)
		{
			for (int i = 0; i < _noteBook.NPages; i++)
			{
				var page = _noteBook.GetNthPage(i) as NotebookPageWrapper;

				if (page?.Widget == widget)
				{
					_noteBook.RemovePage(i);
					break;
				}
			}
		}

		public async void SetBackgroundImage(ImageSource imageSource)
		{
			// async void: a load failure here has nowhere to surface and would crash the process.
			// See the note on Page.SetBackgroundImage.
			try
			{
				_backgroundPixbuf = await imageSource.GetNativeImageAsync();
			}
			catch (Exception)
			{
				return;
			}

			if (_noteBook == null)
				return;

			for (int i = 0; i < _noteBook.NPages; i++)
			{
				var page = _noteBook.GetNthPage(i) as NotebookPageWrapper;

				if (page != null)
				{
					page.SetPixbuf(_backgroundPixbuf);
				}
			}
		}

		private void Build()
		{
			_noteBook = new Notebook
			{
				CanFocus = true,
				Scrollable = true,
				ShowTabs = true,
				TabPos = PositionType.Top
			};

			Add(_noteBook);
		}
	}

	internal class NotebookPageWrapper : Fixed
	{
		private Gdk.Rectangle _lastAllocation = Gdk.Rectangle.Zero;
		private ImageControl _image;
		private Widget _widget;

		public NotebookPageWrapper(Widget widget)
		{
			_widget = widget;
			Build();
		}

		public Widget Widget => _widget;

		public void SetPixbuf(Pixbuf pixbuf)
		{
			_image.Pixbuf = pixbuf;
		}

		bool _childResizeQueued;

		/// <summary>
		/// Keeps the background image and the hosted page the size of this Fixed.
		/// </summary>
		/// <remarks>
		/// Deferred to idle, never applied inline. SetSizeRequest calls gtk_widget_queue_resize,
		/// and doing that from inside a size-allocate handler is invalid in GTK3: the resize is
		/// not merely lost, the alloc-needed flag is left standing on this widget's ancestors and
		/// gtk_widget_queue_resize_internal bails out at the first ancestor that already carries
		/// it - so every later resize raised anywhere in this tab's subtree was swallowed too.
		/// Same treatment as Controls.Page.OnContentContainerWrapperSizeAllocated.
		/// </remarks>
		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			if (_lastAllocation == allocation)
				return;

			_lastAllocation = allocation;

			if (_childResizeQueued)
				return;

			_childResizeQueued = true;

			GLib.Idle.Add(() =>
			{
				// A pending idle can outlive the page: RemovePage destroys this wrapper, and a
				// destroyed or disposed GtkSharp wrapper is left holding a null handle, so check
				// that too and not only the field. _lastAllocation is read here, not captured, so
				// an allocation that arrived while this was queued still lands.
				_childResizeQueued = false;

				if (_image != null && _image.Handle != IntPtr.Zero)
					_image.SetSizeRequest(_lastAllocation.Width, _lastAllocation.Height);

				// The hosted widget is also checked for still being ours: InsertPage unparents it
				// to move it into another wrapper, and by the time this idle runs it may already
				// belong to a different tab, whose size is none of our business.
				if (_widget != null && _widget.Handle != IntPtr.Zero && _widget.Parent?.Handle == Handle)
					_widget.SetSizeRequest(_lastAllocation.Width, _lastAllocation.Height);

				return false;
			});
		}

		private void Build()
		{
			_image = new ImageControl();
			_image.Aspect = ImageAspect.AspectFill;

			Add(_image);
			Add(_widget);
		}
	}
}
