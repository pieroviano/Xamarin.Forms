using System;
using System.IO;
using System.Linq;
using Xamarin.Forms;
using Xunit;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// The rest of coverage target 2 of plan §10.2. <see cref="PropertyMappingTests"/> covers the
	/// text and value controls; this covers the four on that list whose native side is not a plain
	/// property bag - <see cref="Image"/> (asynchronous source loading), <see cref="BoxView"/>
	/// (nothing but Cairo drawing, so the only honest ground truth is pixels) and the two picker
	/// controls, whose native counterparts are hand-written widgets rather than GTK ones.
	/// </summary>
	public class CoreControlMappingTests : GtkTestBase
	{
		// ---- Image ---------------------------------------------------------------------
		//
		// ImageRenderer.SetImage is `async void` and awaits GetNativeImageAsync, so the Pixbuf
		// does not exist when the renderer is created. Every assertion here pumps until it does
		// rather than sleeping, and fails on the timeout instead of on a null reference.

		[Fact]
		public void ImageLoadsItsSourceAndFollowsAChange()
		{
			Run(() =>
			{
					var small = WriteTempPng(24, 16, 0xFF0000FF);
					var large = WriteTempPng(48, 32, 0x00FF00FF);

					try
					{
						var image = new Image { Source = ImageSource.FromFile(small) };

						using (var host = GtkTestHost.HostView(image, 200, 200))
						{
							var native = host.Control<Controls.ImageControl>();

							// GetDesiredSize, NOT the Pixbuf property. Both report the decoded image, which
							// is the independent ground truth wanted here - it comes from the PNG on disk,
							// not from anything the test set - but only GetDesiredSize reports the ORIGINAL:
							// ImageControl.Pixbuf returns the child Gtk.Image's pixbuf, which
							// OnSizeAllocated replaces with an aspect-scaled copy sized to the allocation
							// (ImageControl.cs:137). Asserting the source dimensions on it is a race that is
							// only winnable between the load and the next allocation, and pumping for a
							// reload loses it by construction. GetDesiredSize reads _original and is stable.
							WaitFor(host, () => native.GetDesiredSize().Width == 24,
								"the Image never loaded its source");

							Assert.Equal(24, native.GetDesiredSize().Width);
							Assert.Equal(16, native.GetDesiredSize().Height);

							image.Source = ImageSource.FromFile(large);
							WaitFor(host, () => native.GetDesiredSize().Width == 48,
								"changing Image.Source did not reload the pixbuf - " +
								"OnElementPropertyChanged is not handling SourceProperty");

							Assert.Equal(48, native.GetDesiredSize().Width);
							Assert.Equal(32, native.GetDesiredSize().Height);
						}
					}
					finally
					{
						Delete(small);
						Delete(large);
					}
		
			});
		}

		[Fact]
		public void ImageMapsAspectAtCreationAndOnChange()
		{
			Run(() =>
			{
					var image = new Image { Aspect = Aspect.AspectFill };

					using (var host = GtkTestHost.HostView(image, 200, 200))
					{
						var native = host.Control<Controls.ImageControl>();

						Assert.Equal(Controls.ImageAspect.AspectFill, native.Aspect);

						image.Aspect = Aspect.Fill;
						host.Pump();
						Assert.Equal(Controls.ImageAspect.Fill, native.Aspect);

						image.Aspect = Aspect.AspectFit;
						host.Pump();
						Assert.Equal(Controls.ImageAspect.AspectFit, native.Aspect);
					}
		
			});
		}

		// ---- BoxView -------------------------------------------------------------------
		//
		// Controls.BoxView keeps its colour in a private field and paints it in Draw(); there is
		// no property to read back, and asserting "the Color I set is the Color the element has"
		// would test Core rather than the renderer. So this one is measured off the X server, in
		// painted pixels - the same ground truth the M3 smoke scripts use.

		[Fact]
		public void BoxViewPaintsItsColourAndRepaintsOnChange()
		{
			Run(() =>
			{
					var box = new BoxView { Color = Color.Red, WidthRequest = 120, HeightRequest = 80 };

					using (var host = GtkTestHost.HostView(box, 200, 160))
					{
						// host.Renderer, not Platform.GetRenderer: ViewHost builds the renderer with
						// Platform.CreateRenderer and parents it by hand, and never calls
						// Platform.SetRenderer, so the RendererProperty this element carries is unset and
						// the lookup returns null. Only the HostPage tests, which go through the real
						// Forms path, can use the static accessor.
						var widget = (Gtk.Widget)host.Renderer;

						Assert.False(GtkTestHost.IsUnallocated(widget),
							$"BoxView was never allocated: {GtkTestHost.Describe(widget)}");

						host.Pump(10);

						var bounds = GtkTestHost.BoundsIn(widget);
						var red = PixelAt(host.Window, bounds.X + 10, bounds.Y + 10);

						Assert.True(red.R > 200 && red.G < 60 && red.B < 60,
							$"a red BoxView painted rgb({red.R},{red.G},{red.B}) at its top-left corner");

						box.Color = Color.Blue;
						host.Pump(10);

						var blue = PixelAt(host.Window, bounds.X + 10, bounds.Y + 10);

						Assert.True(blue.B > 200 && blue.R < 60 && blue.G < 60,
							$"Color Red -> Blue painted rgb({blue.R},{blue.G},{blue.B}); " +
							"OnElementPropertyChanged is not repainting");
					}
		
			});
		}

		// ---- DatePicker ----------------------------------------------------------------

		[Fact]
		public void DatePickerMapsDateBoundsAndFormat()
		{
			Run(() =>
			{
					var picker = new DatePicker
					{
						MinimumDate = new DateTime(2019, 1, 1),
						MaximumDate = new DateTime(2021, 12, 31),
						Date = new DateTime(2020, 5, 17),
						Format = "yyyy-MM-dd"
					};

					using (var host = GtkTestHost.HostView(picker))
					{
						var native = host.Control<Controls.DatePicker>();

						Assert.Equal(new DateTime(2020, 5, 17), native.CurrentDate.Date);
						Assert.Equal(new DateTime(2019, 1, 1), native.MinDate.Date);
						Assert.Equal(new DateTime(2021, 12, 31), native.MaxDate.Date);
						Assert.Equal("yyyy-MM-dd", native.DateFormat);

						picker.Date = new DateTime(2020, 9, 3);
						picker.Format = "dd/MM/yyyy";
						host.Pump();

						Assert.Equal(new DateTime(2020, 9, 3), native.CurrentDate.Date);
						Assert.Equal("dd/MM/yyyy", native.DateFormat);
					}
		
			});
		}

		[Fact]
		public void DatePickerDateChangesFlowBackFromTheNativeControl()
		{
			Run(() =>
			{
					var picker = new DatePicker { Date = new DateTime(2020, 5, 17) };

					using (var host = GtkTestHost.HostView(picker))
					{
						host.Control<Controls.DatePicker>().CurrentDate = new DateTime(2022, 2, 22);
						host.Pump();

						Assert.Equal(new DateTime(2022, 2, 22), picker.Date.Date);
					}
		
			});
		}

		// ---- TimePicker ----------------------------------------------------------------

		[Fact]
		public void TimePickerMapsTimeAndFormat()
		{
			Run(() =>
			{
					var picker = new TimePicker
					{
						Time = new TimeSpan(13, 45, 0),
						Format = "HH:mm"
					};

					using (var host = GtkTestHost.HostView(picker))
					{
						var native = host.Control<Controls.TimePicker>();

						Assert.Equal(new TimeSpan(13, 45, 0), native.CurrentTime);
						Assert.Equal("HH:mm", native.TimeFormat);

						picker.Time = new TimeSpan(7, 5, 0);
						picker.Format = "hh:mm tt";
						host.Pump();

						Assert.Equal(new TimeSpan(7, 5, 0), native.CurrentTime);
						Assert.Equal("hh:mm tt", native.TimeFormat);
					}
		
			});
		}

		[Fact]
		public void TimePickerSelectionReachesTheElement()
		{
			Run(() =>
			{
					// The OTHER direction. TimePickerMapsTimeAndFormat above only covers Time flowing
					// INTO the native control, which is why this went unnoticed: the renderer pushed
					// `DateTime.Today + CurrentTime` into TimePicker.TimeProperty, which is typed
					// TimeSpan. BindableProperty.TryConvert has no DateTime->TimeSpan route, so
					// SetValueCore logged and returned without assigning and every user selection was
					// silently discarded while the entry still showed the new time.
					var picker = new TimePicker { Time = new TimeSpan(9, 0, 0) };

					using (var host = GtkTestHost.HostView(picker))
					{
						var native = host.Control<Controls.TimePicker>();

						// What the popup does when the user picks a time.
						native.CurrentTime = new TimeSpan(14, 30, 0);
						host.Pump();

						Assert.Equal(new TimeSpan(14, 30, 0), picker.Time);
					}
		
			});
		}

		/// <summary>
		/// Regression guard for a real defect found while writing this suite:
		/// <c>TimePickerRenderer.Dispose</c> read
		///
		///     Control.GotFocus += GotFocus;
		///     Control.LostFocus += LostFocus;
		///
		/// - <c>+=</c> where every other renderer, <c>DatePickerRenderer</c> included, has
		/// <c>-=</c>. Disposing the renderer therefore SUBSCRIBED it a second time instead of
		/// unsubscribing, so a disposed renderer kept pushing IsFocused into a dead element and
		/// each create/dispose cycle added another handler. Nothing about it is visible in a
		/// screenshot. The test counts the handlers on the native control's event, which is the
		/// only observable that moves.
		/// </summary>
		[Fact]
		public void DisposingATimePickerRendererUnsubscribesFromTheNativeControl()
		{
			Run(() =>
			{
					var picker = new TimePicker { Time = new TimeSpan(9, 0, 0) };
					var renderer = Platform.CreateRenderer(picker);
					var native = (Controls.TimePicker)((IVisualNativeElementRenderer)renderer).Control;

					var before = HandlerCount(native, "GotFocus");

					Assert.True(before >= 1,
						"the renderer never subscribed to GotFocus, so this test proves nothing");

					renderer.Dispose();

					var after = HandlerCount(native, "GotFocus");

					Assert.True(after < before,
						$"disposing the renderer left {after} GotFocus handlers where there were {before} " +
						"before - Dispose is subscribing (+=) instead of unsubscribing (-=)");
		
			});
		}

		// ---- helpers -------------------------------------------------------------------

		/// <summary>
		/// Counts the delegates on a native control's event by reading the backing field. The
		/// events are plain field-like C# events on the hand-written GTK controls, so the compiler
		/// generates a same-named private field holding the multicast delegate.
		/// </summary>
		static int HandlerCount(object target, string eventName)
		{
			var field = target.GetType().GetField(
				eventName,
				System.Reflection.BindingFlags.Instance |
				System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.NonPublic);

			Assert.True(field != null,
				$"{target.GetType().Name} has no backing field for the {eventName} event; " +
				"this test needs rewriting rather than deleting");

			return (field.GetValue(target) as Delegate)?.GetInvocationList().Length ?? 0;
		}

		static void WaitFor<TView>(GtkTestHost.ViewHost<TView> host, Func<bool> condition, string because)
			where TView : View
		{
			for (int i = 0; i < 100 && !condition(); i++)
				host.Pump(2);

			Assert.True(condition(), because);
		}

		// The Gtk 4 readback path lives in GtkTestHost.PixelAt: a widget owns no pixels there, so
		// it is rendered through a GtkWidgetPaintable rather than photographed off a GdkWindow.
		static (byte R, byte G, byte B) PixelAt(Gtk.Window window, int x, int y) =>
			GtkTestHost.PixelAt(window, x, y);

		static string WriteTempPng(int width, int height, uint rgba)
		{
			var path = Path.Combine(
				Path.GetTempPath(),
				$"xf-gtk-test-{Guid.NewGuid():N}.png");

			using (var pixbuf = new Gdk.Pixbuf(Gdk.Colorspace.Rgb, true, 8, width, height))
			{
				pixbuf.Fill(rgba);
				pixbuf.Save(path, "png");
			}

			return path;
		}

		static void Delete(string path)
		{
			try
			{
				File.Delete(path);
			}
			catch (IOException)
			{
			}
		}
	}
}
