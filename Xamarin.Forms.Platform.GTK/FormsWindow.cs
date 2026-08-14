using System;
using System.ComponentModel;
using System.Threading;
using Gtk;

namespace Xamarin.Forms.Platform.GTK
{
	public class FormsWindow : Window
	{
		private Application _application;
		private Gdk.Size _lastSize;

		// No WindowType: Gtk 4 removed the enum, because a GtkWindow is always a toplevel now -
		// the popup case it also covered is a GtkPopover.
		public FormsWindow()
		{
			SetDefaultSize(800, 600);
			SetSizeRequest(400, 400);

			MainThreadID = Thread.CurrentThread.ManagedThreadId;
			MainWindow = this;

			if (SynchronizationContext.Current == null)
				SynchronizationContext.SetSynchronizationContext(new GtkSynchronizationContext());

			// GtkWindow:suspended, not Gtk 3's window-state-event, which Gtk 4 removed along with
			// GdkWindowState. It is a better fit than the state bit it replaces here: "suspended"
			// means the window's content is not visible to the user - minimised, on another
			// workspace, or fully occluded - which is exactly when an application should sleep,
			// whereas the Gtk 3 code could only see the minimise case.
			AddNotification("suspended", OnSuspendedNotified);
		}

		public static int MainThreadID { get; set; }
		public static Window MainWindow { get; set; }

		public void LoadApplication(Application application)
		{
			if (application == null)
				throw new ArgumentNullException(nameof(application));

			Xamarin.Forms.Application.SetCurrentApplication(application);
			_application = application;

			application.PropertyChanged += ApplicationOnPropertyChanged;
			UpdateMainPage();

			_application.SendStart();
		}

		public void SetApplicationTitle(string title)
		{
			if (string.IsNullOrEmpty(title))
				return;

			Title = title;
		}

		public void SetApplicationIcon(string icon)
		{
			if (string.IsNullOrEmpty(icon))
				return;

			// IconName, not an icon Pixbuf. Gtk 4 removed gtk_window_set_icon: a window is
			// identified to the desktop by a themed icon NAME, which the shell looks up itself, so
			// there is nowhere to hand a loaded image. The file's base name is the closest thing to
			// the caller's intent - it is what an installed .desktop file's Icon= key would carry.
			IconName = System.IO.Path.GetFileNameWithoutExtension(icon);
		}

		// GtkSharp 3: GLib.Object.Dispose() is no longer virtual - the disposal hook is
		// Dispose(bool) (overridden below), which the base Dispose() calls for us.

		/// <remarks>
		/// GtkWindow::close-request, not Gtk 3's delete-event: Gtk 4 removed GdkEvent delivery to
		/// widgets entirely, and close-request is the signal that replaced it. The return value
		/// means the same thing - true stops the default handling, which is what keeps the window
		/// alive long enough for Application.Quit to unwind the main loop.
		/// </remarks>
		protected override bool OnCloseRequest()
		{
			Gtk.Application.Quit();

			return true;
		}

		private void ApplicationOnPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == nameof(Xamarin.Forms.Application.MainPage))
			{
				UpdateMainPage();
			}
		}

		/// <remarks>
		/// The size_allocate vfunc, not Gtk 3's configure-event. Gtk 4 has no configure-event -
		/// there is no GdkWindow to configure - and a widget learns its geometry from being
		/// allocated. The allocation is in the window's own coordinates, so it carries the size
		/// this needs and none of the screen position the Gtk 3 event also had; nothing here read
		/// the position.
		/// </remarks>
		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			Gdk.Size newSize = new Gdk.Size(allocation.Width, allocation.Height);

			if (_lastSize == newSize || _application == null)
				return;

			_lastSize = newSize;
			var pageRenderer = Platform.GetRenderer(_application.MainPage);
			pageRenderer?.SetElementSize(new Size(newSize.Width, newSize.Height));
		}

		private void UpdateMainPage()
		{
			if (_application.MainPage == null)
				return;

			var platformRenderer = Child as PlatformRenderer;

			if (platformRenderer != null)
			{
				RemoveChildIfExists();
				((IDisposable)platformRenderer.Platform).Dispose();
			}

			var platform = new Platform();
			platform.PlatformRenderer.SetSizeRequest(WidthRequest, HeightRequest);
			Add(platform.PlatformRenderer);
			platform.SetPage(_application.MainPage);

			Child.ShowAll();
		}

		private void RemoveChildIfExists()
		{
			foreach (var child in Children)
			{
				var widget = child as Widget;

				if (widget != null)
				{
					Remove(widget);
				}
			}
		}

		private void OnSuspendedNotified(object o, GLib.NotifyArgs args)
		{
			// The notification fires on every change of the property, so there is no changed-mask
			// to test as there was in Gtk 3 - the property IS the news.
			if (_application == null)
				return;

			if (Suspended)
				_application.SendSleep();
			else
				_application.SendResume();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && _application != null)
			{
				RemoveNotification("suspended", OnSuspendedNotified);
				_application.PropertyChanged -= ApplicationOnPropertyChanged;
			}

			base.Dispose(disposing);
		}
	}
}
