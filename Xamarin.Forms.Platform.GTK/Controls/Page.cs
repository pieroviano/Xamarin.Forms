using System;
using Gdk;
using Gtk;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	public class Page : Gtk.Grid
	{
		private Gdk.Rectangle _lastAllocation = Gdk.Rectangle.Zero;
		private GtkFormsContainer _headerContainer;
		private GtkFormsContainer _contentContainerWrapper;
		private Fixed _contentContainer;
		private Box _toolbar;
		private GtkFormsContainer _content;
		private ImageControl _image;
		private Gdk.Color _defaultBackgroundColor;

		public Box Toolbar
		{
			get
			{
				return _toolbar;
			}
			set
			{
				if (_toolbar != value)
				{
					RefreshToolbar(value);
				}
			}
		}

		public GtkFormsContainer Content
		{
			get
			{
				return _content;
			}
			set
			{
				if (_content != value)
				{
					RefreshContent(value);
				}
			}
		}

		public Page()
		{
			// Was Gtk.Table(1, 1, homogeneous: true). Gtk.Grid grows on demand, so only the
			// homogeneity carries over; the single cell is created by the Attach in BuildPage.
			RowHomogeneous = true;
			ColumnHomogeneous = true;

			BuildPage();
		}

		public void SetToolbarColor(Color backgroundColor)
		{
			_headerContainer.SetBackgroundColor(backgroundColor);
		}

		public void SetBackgroundColor(Color backgroundColor)
		{
			_contentContainerWrapper.SetBackgroundColor(backgroundColor);
		}

		public async void SetBackgroundImage(ImageSource imageSource)
		{
			// This is async void, so nothing can observe a failure here: an exception would be
			// rethrown on the synchronization context with no handler and take the process down.
			// GetNativeImageAsync does file and network I/O, so a missing file or a dead URI is
			// an ordinary outcome, not an exceptional one.
			Pixbuf pixbuf;

			try
			{
				pixbuf = await imageSource.GetNativeImageAsync();
			}
			catch (Exception)
			{
				return;
			}

			// Re-read the field after the await: Destroy() nulls it, and a page navigated away
			// from while its background image was still loading would otherwise dereference null
			// on that same unobservable path. Same guard ImageRenderer.SetImage already uses.
			var image = _image;

			if (image != null)
				image.Pixbuf = pixbuf;
		}

		public override void Destroy()
		{
			base.Destroy();
			if (_contentContainerWrapper != null)
			{
				_contentContainerWrapper.SizeAllocated -= OnContentContainerWrapperSizeAllocated;
				_contentContainerWrapper = null;
			}
			_contentContainer = null;
			_image = null;
			_toolbar = null;
			_content = null;
			_headerContainer = null;
		}

		private void BuildPage()
		{
			_defaultBackgroundColor = this.GetDefaultBackgroundColor(Gtk.StateFlags.Normal);

			_toolbar = new Box(Gtk.Orientation.Horizontal, 0);
			_content = new GtkFormsContainer();

			var root = new Box(Gtk.Orientation.Vertical, 0);

			_headerContainer = new GtkFormsContainer();
			root.PackStart(_headerContainer, false, false, 0);

			_image = new ImageControl();
			_image.Aspect = ImageAspect.Fill;

			_contentContainerWrapper = new GtkFormsContainer();
			_contentContainerWrapper.SizeAllocated += OnContentContainerWrapperSizeAllocated;
			_contentContainer = new Fixed();
			_contentContainer.Add(_image);
			_contentContainerWrapper.Add(_contentContainer);

			root.PackStart(_contentContainerWrapper, true, true, 0); // Should fill all available space

			Attach(root, 0, 0, 1, 1);

			ShowAll();
		}

		private void RefreshToolbar(Box newToolbar)
		{
			_toolbar.Destroy();
			_toolbar = newToolbar;
			_headerContainer.Add(_toolbar);
			_toolbar.ShowAll();
		}

		private void RefreshContent(GtkFormsContainer newContent)
		{
			_content.Destroy();
			_content = newContent;
			_contentContainer.Add(_content);
			_content.ShowAll();
		}

		bool _imageResizeQueued;

		/// <summary>
		/// Keeps the background image the size of the content area.
		/// </summary>
		/// <remarks>
		/// Deferred to idle, never applied inline - and this one is the root of the resize wedge the
		/// plan records under "The resize wedge under Controls.Page's Gtk.Fixed". SetSizeRequest
		/// calls gtk_widget_queue_resize, and doing that from inside a size-allocate handler does not
		/// merely lose the resize: the alloc-needed flag is left standing on this widget's ancestors,
		/// and gtk_widget_queue_resize_internal bails out at the first ancestor that already carries
		/// it. Every later queue_resize raised anywhere in the page subtree was therefore swallowed,
		/// which is exactly the measured boundary in that section - a QueueResize at or below
		/// _contentContainer did nothing, one above it re-allocated the whole subtree.
		///
		/// The visible consequence: the page's content kept the natural size a Gtk.Fixed hands an
		/// unconstrained child no matter what Forms asked for. In the ControlGallery that was a
		/// detail page laid out 257px wide inside a 500px detail area, with the AbsoluteLayout's
		/// proportional children painted at their natural heights and overlapping each other.
		/// </remarks>
		private void OnContentContainerWrapperSizeAllocated(object o, SizeAllocatedArgs args)
		{
			if (_lastAllocation == args.Allocation)
				return;

			_lastAllocation = args.Allocation;

			if (_imageResizeQueued)
				return;

			_imageResizeQueued = true;

			GLib.Idle.Add(() =>
			{
				_imageResizeQueued = false;
				_image?.SetSizeRequest(_lastAllocation.Width, _lastAllocation.Height);

				return false;
			});
		}
	}
}
