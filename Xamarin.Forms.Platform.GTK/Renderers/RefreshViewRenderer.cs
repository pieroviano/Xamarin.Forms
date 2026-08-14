using System;
using System.ComponentModel;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	/// <summary>
	/// GTK has no pull-to-refresh widget, so this synthesizes one: the content is laid out by the
	/// inherited layout path (RefreshView is a ContentView, i.e. a Layout), and a Gtk.Spinner is
	/// parked on top of it, shown while <see cref="RefreshView.IsRefreshing"/> is set.
	///
	/// Refresh is started by a downward drag from the top edge - the desktop stand-in for the
	/// mobile pull gesture - which sets IsRefreshing, and Core takes it from there: setting it
	/// raises <c>Refreshing</c> and executes <c>Command</c>, and refuses (coerces back to false)
	/// when the view is disabled or the command cannot execute.
	/// </summary>
	public class RefreshViewRenderer : LayoutRenderer
	{
		// How far down the drag has to travel before it counts as a pull-to-refresh, and how far
		// from the top edge it has to start.
		const int RefreshTriggerDistance = 60;
		const int TopEdgeThreshold = 40;
		const int SpinnerSize = 32;

		Gtk.Spinner _spinner;

		// Gtk.Spinner has no GdkWindow of its own, so it is painted into its parent's window -
		// which puts it UNDERNEATH the content renderer's container, because a child with its own
		// window always stacks above the no-window drawing of its siblings, whatever the child
		// order says. Measured: the spinner was correctly allocated at [184,52 32x32], last in the
		// child list, mapped, and still invisible. Hosting it in an EventBox gives it a real
		// window that can be raised above the content.
		Gtk.EventBox _spinnerHost;

		Gtk.GestureDrag _dragGesture;
		bool _spinnerPlaced;
		bool _positionUpdateQueued;

		RefreshView RefreshView => Element as RefreshView;

		protected override void OnElementChanged(ElementChangedEventArgs<Layout> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement == null || Control == null)
				return;

			if (_spinner == null)
			{
				_spinner = new Gtk.Spinner();
				_spinner.SetSizeRequest(SpinnerSize, SpinnerSize);

				_spinnerHost = new Gtk.EventBox { VisibleWindow = true };
				_spinnerHost.NoShowAll = true;   // visibility is ours to drive, not ShowAll's
				_spinnerHost.SetSizeRequest(SpinnerSize, SpinnerSize);
				_spinnerHost.Add(_spinner);

				Control.Add(_spinnerHost);
				_spinnerPlaced = true;

				// AddController, not a widget argument to the constructor: Gtk 4 gestures are
				// constructed unattached and then handed to a widget, which is what lets several
				// of them watch the same widget in different propagation phases.
				_dragGesture = new Gtk.GestureDrag();
				Control.AddController(_dragGesture);
				_dragGesture.DragEnded += OnDragEnd;
			}

			UpdateIsRefreshing();
			UpdateRefreshColor();
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == RefreshView.IsRefreshingProperty.PropertyName)
				UpdateIsRefreshing();
			else if (e.PropertyName == RefreshView.RefreshColorProperty.PropertyName)
				UpdateRefreshColor();
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			// Deferred, like every other geometry change in this backend: moving a Fixed child
			// from inside a size-allocate is discarded by GTK3.
			if (!_spinnerPlaced || _positionUpdateQueued)
				return;

			_positionUpdateQueued = true;

			GLib.Idle.Add(() =>
			{
				_positionUpdateQueued = false;
				CentreSpinner();
				return false;
			});
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_dragGesture != null)
				{
					_dragGesture.DragEnded -= OnDragEnd;
					_dragGesture.Dispose();
					_dragGesture = null;
				}

				_spinner = null;
				_spinnerHost = null;
				_spinnerPlaced = false;
			}

			base.Dispose(disposing);
		}

		void CentreSpinner()
		{
			if (_spinnerHost == null || Control == null)
				return;

			var x = Math.Max(0, (Control.Width - SpinnerSize) / 2);

			// Near the top, where a pull-to-refresh indicator belongs.
			_spinnerHost.MoveTo(x, 8);
		}

		void UpdateIsRefreshing()
		{
			if (_spinner == null || RefreshView == null)
				return;

			if (RefreshView.IsRefreshing)
			{
				// Position first, then show: showing a widget allocates it afresh, whereas moving
				// an already-visible Gtk.Fixed child can leave the old allocation in place.
				CentreSpinner();
				_spinner.Visible = true;
				_spinnerHost.Visible = true;
				_spinner.Start();
				_spinnerHost.Raise();
			}
			else
			{
				_spinner.Stop();
				_spinnerHost.Visible = false;
			}
		}

		void UpdateRefreshColor()
		{
			if (_spinner == null || RefreshView == null)
				return;

			if (RefreshView.RefreshColor == Color.Default)
				_spinner.ClearStyle();
			else
				_spinner.SetForegroundColor(RefreshView.RefreshColor.ToGtkColor());
		}

		void OnDragEnd(object o, Gtk.DragEndedArgs args)
		{
			if (RefreshView == null || RefreshView.IsRefreshing || !RefreshView.IsEnabled)
				return;

			if (!_dragGesture.GetStartPoint(out _, out var startY))
				return;

			if (!_dragGesture.GetOffset(out _, out var offsetY))
				return;

			// A downward drag that began near the top edge. Core coerces IsRefreshing back to
			// false if the command cannot run, so no CanExecute check is needed here.
			if (startY <= TopEdgeThreshold && offsetY >= RefreshTriggerDistance)
				ElementController.SetValueFromRenderer(RefreshView.IsRefreshingProperty, true);
		}
	}
}
