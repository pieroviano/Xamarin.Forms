using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.GTK;
using Xamarin.Forms.Platform.GTK.Cells;
using Xamarin.Forms.Platform.GTK.Renderers;
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
	[TestFixture]
	public class RendererRegistrationTests
	{
		/// <summary>
		/// Every <c>[assembly: ExportRenderer]</c> / <c>ExportCell</c> / <c>ExportImageSourceHandler</c>
		/// in Xamarin.Forms.Platform.GTK/Properties/AssemblyInfo.cs. Read out of the assembly's own
		/// attributes rather than re-typed here, so the list cannot drift out of date and a renderer
		/// deleted from AssemblyInfo.cs disappears from the suite instead of failing it.
		/// </summary>
		public static IEnumerable<TestCaseData> ExportedHandlers()
		{
			var platformAssembly = typeof(LabelRenderer).Assembly;

			foreach (var attribute in platformAssembly.GetCustomAttributes<HandlerAttribute>())
			{
				var handled = HandledType(attribute);
				var handler = HandlerType(attribute);

				if (handled == null || handler == null)
					continue;

				yield return new TestCaseData(handled, handler)
					.SetName($"Resolves_{handled.Name}_to_{handler.Name}");
			}
		}

		[TestCaseSource(nameof(ExportedHandlers))]
		public void ExportedHandlerIsResolvable(Type handledType, Type expectedHandler)
		{
			var resolved = Registrar.Registered.GetHandlerType(handledType);

			Assert.That(resolved, Is.Not.Null,
				$"{handledType.Name} resolves to no handler at all - its [assembly: Export*] " +
				"attribute is missing or was not scanned by Forms.Init.");

			Assert.That(resolved, Is.EqualTo(expectedHandler),
				$"{handledType.Name} resolved to {resolved.Name}, not {expectedHandler.Name}.");
		}

		/// <summary>
		/// The registrar answers for subclasses too, which is how a plain
		/// <see cref="ContentPage"/> gets <see cref="PageRenderer"/> and a user's
		/// <c>MyLayout : StackLayout</c> gets <see cref="LayoutRenderer"/>. Registering only the
		/// exact types would leave every real application view unrendered.
		/// </summary>
		[TestCase(typeof(ContentPage), typeof(PageRenderer))]
		[TestCase(typeof(StackLayout), typeof(LayoutRenderer))]
		[TestCase(typeof(Grid), typeof(LayoutRenderer))]
		[TestCase(typeof(AbsoluteLayout), typeof(LayoutRenderer))]
		[TestCase(typeof(ContentView), typeof(LayoutRenderer))]
		public void SubclassesInheritTheirBaseRenderer(Type viewType, Type expectedHandler)
		{
			Assert.That(Registrar.Registered.GetHandlerType(viewType), Is.EqualTo(expectedHandler));
		}

		/// <summary>
		/// The gallery's own start-up assertion (<c>CoreRootPage.ValidateRegistrar</c>) is that no
		/// control falls back to <c>Platform.DefaultRenderer</c>, which draws nothing and fails
		/// silently. This is that assertion without needing the gallery. <c>DefaultRenderer</c> is
		/// internal, so the check is on the fallback's precondition instead:
		/// <c>Platform.CreateRenderer</c> substitutes it exactly when
		/// <c>GetHandlerForObject</c> finds nothing, i.e. when the registry has no entry.
		/// </summary>
		[TestCaseSource(nameof(RenderableControls))]
		public void ControlDoesNotFallBackToDefaultRenderer(Type viewType)
		{
			var resolved = Registrar.Registered.GetHandlerType(viewType);

			Assert.That(resolved, Is.Not.Null,
				$"{viewType.Name} has no registered renderer, so Platform.CreateRenderer will " +
				"hand it a DefaultRenderer and it will render nothing.");

			Assert.That(resolved.Assembly, Is.EqualTo(typeof(LabelRenderer).Assembly),
				$"{viewType.Name} resolved to {resolved.FullName}, which is not a GTK renderer.");
		}

		public static IEnumerable<Type> RenderableControls()
		{
			yield return typeof(ActivityIndicator);
			yield return typeof(BoxView);
			yield return typeof(Button);
			yield return typeof(CarouselPage);
			yield return typeof(CarouselView);
			yield return typeof(CheckBox);
			yield return typeof(CollectionView);
			yield return typeof(DatePicker);
			yield return typeof(Editor);
			yield return typeof(Entry);
			yield return typeof(FlyoutPage);
			yield return typeof(Frame);
			yield return typeof(Image);
			yield return typeof(ImageButton);
			yield return typeof(IndicatorView);
			yield return typeof(Label);
			yield return typeof(ListView);
			yield return typeof(NavigationPage);
			yield return typeof(OpenGLView);
			yield return typeof(Picker);
			yield return typeof(ProgressBar);
			yield return typeof(RadioButton);
			yield return typeof(RefreshView);
			yield return typeof(ScrollView);
			yield return typeof(SearchBar);
			yield return typeof(Shell);
			yield return typeof(Slider);
			yield return typeof(Stepper);
			yield return typeof(SwipeView);
			yield return typeof(Switch);
			yield return typeof(TabbedPage);
			yield return typeof(TableView);
			yield return typeof(TimePicker);
			yield return typeof(WebView);

			// The Shapes family: written, compiled, and for a while unregistered.
			yield return typeof(GtkShapes.Rectangle);
			yield return typeof(GtkShapes.Ellipse);
			yield return typeof(GtkShapes.Line);
			yield return typeof(GtkShapes.Path);
			yield return typeof(GtkShapes.Polygon);
			yield return typeof(GtkShapes.Polyline);
		}

		/// <summary>
		/// <see cref="EmbeddedFont"/> is not a view, so it never appears in a gallery page and no
		/// screenshot can show it missing - but <c>FontRegistrar</c> resolves
		/// <c>IEmbeddedFontLoader</c> through this same registry, so without it every
		/// <c>[assembly: ExportFont]</c> in a consuming app silently does nothing.
		/// </summary>
		[Test]
		public void EmbeddedFontLoaderIsRegistered()
		{
			Assert.That(Registrar.Registered.GetHandlerType(typeof(EmbeddedFont)),
				Is.EqualTo(typeof(GtkEmbeddedFontLoader)));
		}

		[Test]
		public void CellRenderersAreRegistered()
		{
			Assert.Multiple(() =>
			{
				Assert.That(Registrar.Registered.GetHandlerType(typeof(TextCell)), Is.EqualTo(typeof(TextCellRenderer)));
				Assert.That(Registrar.Registered.GetHandlerType(typeof(ImageCell)), Is.EqualTo(typeof(ImageCellRenderer)));
				Assert.That(Registrar.Registered.GetHandlerType(typeof(EntryCell)), Is.EqualTo(typeof(EntryCellRenderer)));
				Assert.That(Registrar.Registered.GetHandlerType(typeof(SwitchCell)), Is.EqualTo(typeof(SwitchCellRenderer)));
				Assert.That(Registrar.Registered.GetHandlerType(typeof(ViewCell)), Is.EqualTo(typeof(ViewCellRenderer)));
			});
		}

		[Test]
		public void ImageSourceHandlersAreRegistered()
		{
			Assert.Multiple(() =>
			{
				Assert.That(Registrar.Registered.GetHandlerType(typeof(FileImageSource)), Is.EqualTo(typeof(FileImageSourceHandler)));
				Assert.That(Registrar.Registered.GetHandlerType(typeof(StreamImageSource)), Is.EqualTo(typeof(StreamImagesourceHandler)));
				Assert.That(Registrar.Registered.GetHandlerType(typeof(UriImageSource)), Is.EqualTo(typeof(UriImageSourceHandler)));
				Assert.That(Registrar.Registered.GetHandlerType(typeof(FontImageSource)), Is.EqualTo(typeof(FontImageSourceHandler)));
			});
		}

		/// <summary>
		/// Guards the count as well as the contents: a renderer silently dropped from
		/// AssemblyInfo.cs still passes every test above, because the data source is read from the
		/// same file. Only a total tells you something went missing.
		/// </summary>
		[Test]
		public void TheExpectedNumberOfHandlersIsExported()
		{
			var exported = typeof(LabelRenderer).Assembly
				.GetCustomAttributes<HandlerAttribute>()
				.Count(a => HandledType(a) != null);

			Assert.That(exported, Is.EqualTo(54),
				"The GTK backend exports 44 renderers + 6 cells + 4 image-source handlers. " +
				"If you added or removed one deliberately, update this number.");
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
