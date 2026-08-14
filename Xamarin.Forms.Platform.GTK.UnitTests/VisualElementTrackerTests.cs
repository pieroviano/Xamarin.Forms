using Xamarin.Forms;
using Xunit;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// Covers the VisualElement properties that VisualElementTracker maps onto the container
	/// widget rather than onto a control-specific native property.
	/// </summary>
	/// <remarks>
	/// These exist because Opacity and InputTransparent were previously wired into the
	/// property-change pipeline with EMPTY implementations - setting either did nothing at all,
	/// silently, and no test noticed. A silent no-op is worse than a NotImplementedException,
	/// because the only way to discover it is to look at the running app.
	///
	/// VisualElementRenderer derives from GtkFormsContainer and returns itself as Container, so
	/// the renderer returned by the host IS the widget the tracker writes to.
	/// </remarks>
	public class VisualElementTrackerTests : GtkTestBase
	{
		/// <summary>
		/// Decimal places for opacity comparisons. MEASURED: GTK stores opacity as an 8-bit
		/// value, so it round-trips quantised to 1/255 - setting 0.25 reads back as
		/// 64/255 = 0.25098. Two places is inside that quantisation step (~0.004) and still tight
		/// enough to catch a wrong value.
		/// </summary>
		const int OpacityPrecision = 2;

		[Fact]
		public void OpacityIsAppliedToTheContainerAtCreation()
		{
			Run(() =>
			{
					var box = new BoxView { Color = Color.Red, Opacity = 0.25 };

					using (var host = GtkTestHost.HostView(box))
					{
						var widget = (Gtk.Widget)host.Renderer;

						Assert.Equal(0.25, widget.Opacity, OpacityPrecision);
					}
			});
		}

		[Fact]
		public void OpacityIsAppliedToTheContainerOnChange()
		{
			Run(() =>
			{
					// The half that actually catches regressions: a renderer that maps only in
					// OnElementChanged looks right in a screenshot of the initial state and is broken
					// forever afterwards.
					var box = new BoxView { Color = Color.Red };

					using (var host = GtkTestHost.HostView(box))
					{
						var widget = (Gtk.Widget)host.Renderer;

						Assert.Equal(1.0, widget.Opacity, OpacityPrecision);

						box.Opacity = 0.5;
						host.Pump();

						Assert.Equal(0.5, widget.Opacity, OpacityPrecision);

						box.Opacity = 1.0;
						host.Pump();

						Assert.Equal(1.0, widget.Opacity, OpacityPrecision);
					}
			});
		}

		[Theory]
		[InlineData(2.0, 1.0)]
		[InlineData(-1.0, 0.0)]
		public void OpacityIsClampedToTheRangeGtkAccepts(double set, double expected)
		{
			Run(() =>
			{
					// Forms does not validate Opacity, and gtk_widget_set_opacity documents 0..1.
					var box = new BoxView { Color = Color.Red };

					using (var host = GtkTestHost.HostView(box))
					{
						var widget = (Gtk.Widget)host.Renderer;

						box.Opacity = set;
						host.Pump();

						Assert.Equal(expected, widget.Opacity, OpacityPrecision);
					}
			});
		}

		[Fact]
		public void InputTransparentIsScopedToTheElementItIsSetOn()
		{
			Run(() =>
			{
					// REGRESSION GUARD. Under Gtk 3 this guarded a specific hazard: GtkFormsContainer was a
					// windowless EventBox, so gtk_widget_get_window returned the PARENT's GdkWindow, shared
					// with every other element on the page - and setting Gdk.Window.PassThrough there (the
					// API whose name matches Forms' semantics) would have made the whole page unclickable.
					//
					// Gtk 4 has no GdkWindows at all, so that hazard cannot exist and the assertions that
					// checked for it have nothing to read. What replaced the mechanism is Widget.CanTarget,
					// which is per-widget by construction - so the guard becomes the property that matters:
					// an InputTransparent element opts ITSELF out of hit-testing and leaves its parent alone.
					var box = new BoxView { Color = Color.Red, InputTransparent = true };

					using (var host = GtkTestHost.HostView(box))
					{
						var widget = (Gtk.Widget)host.Renderer;

						Assert.False(widget.CanTarget,
							"an InputTransparent element must opt itself out of hit-testing");

						Assert.True(widget.Parent == null || widget.Parent.CanTarget,
							"InputTransparent must not disable input on the parent it shares with the page");
					}
			});
		}

		/// <summary>
		/// The behaviour, not the mechanism: an InputTransparent element must stop responding to
		/// taps, and must start again when it is reverted.
		/// </summary>
		/// <remarks>
		/// What this replaced asserted <c>widget.Window.PassThrough == false</c> before and after
		/// the toggle. Nothing in this backend ever writes that property - <see
		/// cref="Xamarin.Forms.Platform.GTK.VisualElementTracker{TElement,TNativeElement}"/>
		/// implements InputTransparent by attaching or detaching the container's ButtonPressEvent
		/// handler - so both assertions held whether or not UpdateInputTransparent had a body at
		/// all. That is precisely the blindness the class remarks above say these tests exist to
		/// end, reproduced.
		///
		/// <para>The baseline press is not ceremony. Without it, an element that never responded to
		/// taps in the first place would satisfy the "no tap while transparent" half, and the
		/// revert half is what stops the fix being "detach the handler and never re-attach it".</para>
		/// </remarks>
		[Fact]
		public void InputTransparentSuppressesTapsAndRevertingRestoresThem()
		{
			Run(() =>
			{
					var box = new BoxView { Color = Color.Red, WidthRequest = 100, HeightRequest = 100 };

					var taps = 0;
					var tap = new TapGestureRecognizer();
					tap.Tapped += (s, e) => taps++;
					box.GestureRecognizers.Add(tap);

					using (var host = GtkTestHost.HostView(box))
					{
						var widget = (Gtk.Widget)host.Renderer;

						GtkTestHost.PressButton(widget);
						host.Pump();

						Assert.True(taps == 1,
							$"a press on an ordinary BoxView with a TapGestureRecognizer raised {taps} taps, " +
							"so the rest of this test would be measuring nothing");

						box.InputTransparent = true;
						host.Pump();

						GtkTestHost.PressButton(widget);
						host.Pump();

						Assert.True(taps == 1,
							$"the element is InputTransparent and still took the tap (total {taps}) - an " +
							"overlay marked input-transparent goes on swallowing every press underneath it");

						box.InputTransparent = false;
						host.Pump();

						GtkTestHost.PressButton(widget);
						host.Pump();

						Assert.True(taps == 2,
							$"reverting InputTransparent left the element deaf (total {taps}) - the handler " +
							"was detached and never re-attached");

						// And reverting it left the element targetable again, rather than only re-attaching
						// handlers - see the test above for why the two are not the same thing.
						Assert.True(widget.CanTarget,
							"reverting InputTransparent must make the element hit-testable again");
					}
			});
		}
	}
}
