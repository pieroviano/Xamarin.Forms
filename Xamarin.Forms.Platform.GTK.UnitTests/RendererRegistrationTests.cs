using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.GTK;
using Xamarin.Forms.Platform.GTK.Cells;
using Xamarin.Forms.Platform.GTK.Renderers;
using Xunit;
using GtkShapes = Xamarin.Forms.Shapes;

namespace Xamarin.Forms.Platform.GTK.UnitTests
{
	/// <summary>
	/// Coverage target 1 of plan §10.2, and the cheapest test in the suite to justify: the GTK
	/// backend resolves renderers through <see cref="Registrar"/>, which is populated by scanning
	/// assembly attributes. A renderer can therefore be written, compiled and shipped while being
	/// completely unreachable, with no build error - which is precisely what happened to the five
	/// Shapes renderers (plan M3 step 6) and to <c>ExportFontAttribute</c> (plan §10.7 A1, where
	/// the missing entry made the whole embedded-font feature a silent no-op).
	/// </summary>
	public class RendererRegistrationTests : GtkTestBase
	{
		/// <summary>
		/// Every <c>[assembly: ExportRenderer]</c> / <c>ExportCell</c> / <c>ExportImageSourceHandler</c>
		/// in Xamarin.Forms.Platform.GTK/Properties/AssemblyInfo.cs. Read out of the assembly's own
		/// attributes rather than re-typed here, so the list cannot drift out of date and a renderer
		/// deleted from AssemblyInfo.cs disappears from the suite instead of failing it. The count
		/// guard below is what catches such a deletion.
		/// </summary>
		public static IEnumerable<object[]> ExportedHandlers()
		{
			var platformAssembly = typeof(LabelRenderer).Assembly;

			foreach (var attribute in platformAssembly.GetCustomAttributes<HandlerAttribute>())
			{
				var handled = HandledType(attribute);
				var handler = HandlerType(attribute);

				if (handled == null || handler == null)
					continue;

				yield return new object[] { handled, handler };
			}
		}

		[Theory]
		[MemberData(nameof(ExportedHandlers))]
		public void ExportedHandlerIsResolvable(Type handledType, Type expectedHandler)
		{
			var resolved = Registrar.Registered.GetHandlerType(handledType);

			Assert.True(resolved != null,
				$"{handledType.Name} resolves to no handler at all - its [assembly: Export*] " +
				"attribute is missing or was not scanned by Forms.Init.");

			Assert.True(resolved == expectedHandler,
				$"{handledType.Name} resolved to {resolved.Name}, not {expectedHandler.Name}.");
		}

		/// <summary>
		/// The registrar answers for subclasses too, which is how a plain
		/// <see cref="ContentPage"/> gets <see cref="PageRenderer"/> and a user's
		/// <c>MyLayout : StackLayout</c> gets <see cref="LayoutRenderer"/>. Registering only the
		/// exact types would leave every real application view unrendered.
		/// </summary>
		[Theory]
		[InlineData(typeof(ContentPage), typeof(PageRenderer))]
		[InlineData(typeof(StackLayout), typeof(LayoutRenderer))]
		[InlineData(typeof(Grid), typeof(LayoutRenderer))]
		[InlineData(typeof(AbsoluteLayout), typeof(LayoutRenderer))]
		[InlineData(typeof(ContentView), typeof(LayoutRenderer))]
		public void SubclassesInheritTheirBaseRenderer(Type viewType, Type expectedHandler)
		{
			Assert.Equal(expectedHandler, Registrar.Registered.GetHandlerType(viewType));
		}

		/// <summary>
		/// The gallery's own start-up assertion (<c>CoreRootPage.ValidateRegistrar</c>) is that no
		/// control falls back to <c>Platform.DefaultRenderer</c>, which draws nothing and fails
		/// silently. This is that assertion without needing the gallery. <c>DefaultRenderer</c> is
		/// internal, so the check is on the fallback's precondition instead:
		/// <c>Platform.CreateRenderer</c> substitutes it exactly when
		/// <c>GetHandlerForObject</c> finds nothing, i.e. when the registry has no entry.
		/// </summary>
		[Theory]
		[MemberData(nameof(RenderableControls))]
		public void ControlDoesNotFallBackToDefaultRenderer(Type viewType)
		{
			var resolved = Registrar.Registered.GetHandlerType(viewType);

			Assert.True(resolved != null,
				$"{viewType.Name} has no registered renderer, so Platform.CreateRenderer will " +
				"hand it a DefaultRenderer and it will render nothing.");

			Assert.True(resolved.Assembly == typeof(LabelRenderer).Assembly,
				$"{viewType.Name} resolved to {resolved.FullName}, which is not a GTK renderer.");
		}

		public static IEnumerable<object[]> RenderableControls()
		{
			foreach (var type in new[]
			{
				typeof(ActivityIndicator),
				typeof(BoxView),
				typeof(Button),
				typeof(CarouselPage),
				typeof(CarouselView),
				typeof(CheckBox),
				typeof(CollectionView),
				typeof(DatePicker),
				typeof(Editor),
				typeof(Entry),
				typeof(FlyoutPage),
				typeof(Frame),
				typeof(Image),
				typeof(ImageButton),
				typeof(IndicatorView),
				typeof(Label),
				typeof(ListView),
				typeof(NavigationPage),
				typeof(OpenGLView),
				typeof(Picker),
				typeof(ProgressBar),
				typeof(RadioButton),
				typeof(RefreshView),
				typeof(ScrollView),
				typeof(SearchBar),
				typeof(Shell),
				typeof(Slider),
				typeof(Stepper),
				typeof(SwipeView),
				typeof(Switch),
				typeof(TabbedPage),
				typeof(TableView),
				typeof(TimePicker),
				typeof(WebView),

				// The Shapes family: written, compiled, and for a while unregistered.
				typeof(GtkShapes.Rectangle),
				typeof(GtkShapes.Ellipse),
				typeof(GtkShapes.Line),
				typeof(GtkShapes.Path),
				typeof(GtkShapes.Polygon),
				typeof(GtkShapes.Polyline)
			})
			{
				yield return new object[] { type };
			}
		}

		/// <summary>
		/// <see cref="EmbeddedFont"/> is not a view, so it never appears in a gallery page and no
		/// screenshot can show it missing - but <c>FontRegistrar</c> resolves
		/// <c>IEmbeddedFontLoader</c> through this same registry, so without it every
		/// <c>[assembly: ExportFont]</c> in a consuming app silently does nothing.
		/// </summary>
		[Fact]
		public void EmbeddedFontLoaderIsRegistered()
		{
			Assert.Equal(typeof(GtkEmbeddedFontLoader),
				Registrar.Registered.GetHandlerType(typeof(EmbeddedFont)));
		}

		[Fact]
		public void CellRenderersAreRegistered()
		{
			Assert.Equal(typeof(TextCellRenderer), Registrar.Registered.GetHandlerType(typeof(TextCell)));
			Assert.Equal(typeof(ImageCellRenderer), Registrar.Registered.GetHandlerType(typeof(ImageCell)));
			Assert.Equal(typeof(EntryCellRenderer), Registrar.Registered.GetHandlerType(typeof(EntryCell)));
			Assert.Equal(typeof(SwitchCellRenderer), Registrar.Registered.GetHandlerType(typeof(SwitchCell)));
			Assert.Equal(typeof(ViewCellRenderer), Registrar.Registered.GetHandlerType(typeof(ViewCell)));
		}

		[Fact]
		public void ImageSourceHandlersAreRegistered()
		{
			Assert.Equal(typeof(FileImageSourceHandler), Registrar.Registered.GetHandlerType(typeof(FileImageSource)));
			Assert.Equal(typeof(StreamImagesourceHandler), Registrar.Registered.GetHandlerType(typeof(StreamImageSource)));
			Assert.Equal(typeof(UriImageSourceHandler), Registrar.Registered.GetHandlerType(typeof(UriImageSource)));
			Assert.Equal(typeof(FontImageSourceHandler), Registrar.Registered.GetHandlerType(typeof(FontImageSource)));
		}

		/// <summary>
		/// Guards the count as well as the contents: a renderer silently dropped from
		/// AssemblyInfo.cs still passes every test above, because the data source is read from the
		/// same file. Only a total tells you something went missing.
		/// </summary>
		[Fact]
		public void TheExpectedNumberOfHandlersIsExported()
		{
			var exported = typeof(LabelRenderer).Assembly
				.GetCustomAttributes<HandlerAttribute>()
				.Count(a => HandledType(a) != null);

			Assert.True(exported == 54,
				$"The GTK backend exports 44 renderers + 6 cells + 4 image-source handlers = 54; " +
				$"this assembly exports {exported}. If you added or removed one deliberately, " +
				"update this number.");
		}

		// HandlerAttribute.TargetType / .HandlerType are internal to Xamarin.Forms.Core, so this
		// assembly cannot see them without an InternalsVisibleTo in Core; reflect rather than
		// modify Core for a test's benefit.
		//
		// THE TWO PROPERTY NAMES ARE INVERTED relative to their meaning, inherited from upstream:
		// HandlerAttribute's constructor is (Type handler, Type target) but every usage reads
		// [assembly: ExportRenderer(typeof(Label), typeof(LabelRenderer))]. Registrar settles it -
		// Registrar.cs:277 calls Register(attribute.HandlerType, attribute.TargetType) against
		// Register(Type tview, Type trender), so:
		//
		//     HandlerType -> the Forms type being handled  (Label)
		//     TargetType  -> the renderer handling it      (LabelRenderer)
		//
		// Reading them the other way round produced 54 confidently-named, uniformly failing tests.
		static Type HandledType(HandlerAttribute attribute) => Member(attribute, "HandlerType");

		static Type HandlerType(HandlerAttribute attribute) => Member(attribute, "TargetType");

		static Type Member(HandlerAttribute attribute, string name)
		{
			const BindingFlags Flags =
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

			var property = typeof(HandlerAttribute).GetProperty(name, Flags);
			if (property != null)
				return property.GetValue(attribute) as Type;

			var field = typeof(HandlerAttribute).GetField(name, Flags);
			return field?.GetValue(attribute) as Type;
		}
	}
}
