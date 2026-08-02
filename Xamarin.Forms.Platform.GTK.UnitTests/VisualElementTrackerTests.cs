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
			var box = new BoxView { Color = Color.Red, Opacity = 0.25 };

			using (var host = GtkTestHost.HostView(box))
			{
				var widget = (Gtk.Widget)host.Renderer;

				Assert.Equal(0.25, widget.Opacity, OpacityPrecision);
			}
		}

		[Fact]
		public void OpacityIsAppliedToTheContainerOnChange()
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
		}

		[Theory]
		[InlineData(2.0, 1.0)]
		[InlineData(-1.0, 0.0)]
		public void OpacityIsClampedToTheRangeGtkAccepts(double set, double expected)
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
		}

		[Fact]
		public void InputTransparentMakesTheContainerWindowPassThrough()
		{
			var box = new BoxView { Color = Color.Red, InputTransparent = true };

			using (var host = GtkTestHost.HostView(box))
			{
				var widget = (Gtk.Widget)host.Renderer;

				// The GdkWindow only exists once realized; HostView shows the toplevel, so by
				// here it must. If this is null the test is not measuring anything, so assert it.
				Assert.NotNull(widget.Window);
				Assert.True(widget.Window.PassThrough,
					"an InputTransparent element must let events reach what is underneath it");
			}
		}

		[Fact]
		public void InputTransparentIsAppliedToTheContainerWindowOnChange()
		{
			var box = new BoxView { Color = Color.Red };

			using (var host = GtkTestHost.HostView(box))
			{
				var widget = (Gtk.Widget)host.Renderer;

				Assert.NotNull(widget.Window);
				Assert.False(widget.Window.PassThrough);

				box.InputTransparent = true;
				host.Pump();

				Assert.True(widget.Window.PassThrough);

				box.InputTransparent = false;
				host.Pump();

				Assert.False(widget.Window.PassThrough);
			}
		}
	}
}
