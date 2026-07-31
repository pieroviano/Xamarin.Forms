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
			_image.Pixbuf = await imageSource.GetNativeImageAsync();
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

		private void OnContentContainerWrapperSizeAllocated(object o, SizeAllocatedArgs args)
		{
			_image.SetSizeRequest(args.Allocation.Width, args.Allocation.Height);
		}
	}
}
