using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK;
using Xamarin.Forms.Platform.GTK.Cells;
using Xamarin.Forms.Platform.GTK.Renderers;

// So the renderer test suite can exercise Platform.DisposeModelAndChildrenRenderers - the
// path navigation runs when a page leaves the stack, and the one plan risk R8 is about.
// Without this the lifecycle tests could only assert preconditions.
// Fully qualified: a `using System.Runtime.CompilerServices;` here makes the
// [Dependency] attributes below ambiguous with that namespace's DependencyAttribute.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Xamarin.Forms.Platform.GTK.UnitTests")]

[assembly: ExportImageSourceHandler(typeof(FileImageSource), typeof(FileImageSourceHandler))]
[assembly: ExportImageSourceHandler(typeof(StreamImageSource), typeof(StreamImagesourceHandler))]
[assembly: ExportImageSourceHandler(typeof(UriImageSource), typeof(UriImageSourceHandler))]
[assembly: ExportImageSourceHandler(typeof(FontImageSource), typeof(FontImageSourceHandler))]

[assembly: Dependency(typeof(ResourcesProvider))]
[assembly: Dependency(typeof(GtkSerializer))]

// FontRegistrar resolves the IEmbeddedFontLoader through the renderer registry, so
// [assembly: ExportFont(...)] only works once EmbeddedFont has a handler registered.
[assembly: ExportRenderer(typeof(EmbeddedFont), typeof(GtkEmbeddedFontLoader))]

[assembly: ExportRenderer(typeof(ActivityIndicator), typeof(ActivityIndicatorRenderer))]
[assembly: ExportRenderer(typeof(BoxView), typeof(BoxViewRenderer))]
[assembly: ExportRenderer(typeof(Button), typeof(ButtonRenderer))]
[assembly: ExportRenderer(typeof(CarouselPage), typeof(CarouselPageRenderer))]
[assembly: ExportRenderer(typeof(CarouselView), typeof(CarouselViewRenderer))]
[assembly: ExportRenderer(typeof(CheckBox), typeof(CheckBoxRenderer))]
[assembly: ExportRenderer(typeof(CollectionView), typeof(CollectionViewRenderer))]
[assembly: ExportRenderer(typeof(DatePicker), typeof(DatePickerRenderer))]
[assembly: ExportRenderer(typeof(Editor), typeof(EditorRenderer))]
[assembly: ExportRenderer(typeof(Entry), typeof(EntryRenderer))]
[assembly: ExportRenderer(typeof(Frame), typeof(FrameRenderer))]
[assembly: ExportRenderer(typeof(Image), typeof(ImageRenderer))]
[assembly: ExportRenderer(typeof(ImageButton), typeof(ImageButtonRenderer))]
[assembly: ExportRenderer(typeof(IndicatorView), typeof(IndicatorViewRenderer))]
[assembly: ExportRenderer(typeof(Label), typeof(LabelRenderer))]
[assembly: ExportRenderer(typeof(Layout), typeof(LayoutRenderer))]
[assembly: ExportRenderer(typeof(ListView), typeof(ListViewRenderer))]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: ExportRenderer(typeof(MasterDetailPage), typeof(MasterDetailPageRenderer))]
#pragma warning restore CS0618 // Type or member is obsolete
[assembly: ExportRenderer(typeof(FlyoutPage), typeof(FlyoutPageRenderer))]
[assembly: ExportRenderer(typeof(NavigationPage), typeof(NavigationPageRenderer))]
[assembly: ExportRenderer(typeof(OpenGLView), typeof(OpenGLViewRenderer))]
[assembly: ExportRenderer(typeof(Page), typeof(PageRenderer))]
[assembly: ExportRenderer(typeof(Picker), typeof(PickerRenderer))]
[assembly: ExportRenderer(typeof(ProgressBar), typeof(ProgressBarRenderer))]
[assembly: ExportRenderer(typeof(RadioButton), typeof(RadioButtonRenderer))]
[assembly: ExportRenderer(typeof(RefreshView), typeof(RefreshViewRenderer))]
[assembly: ExportRenderer(typeof(ScrollView), typeof(ScrollViewRenderer))]
[assembly: ExportRenderer(typeof(SearchBar), typeof(SearchBarRenderer))]
[assembly: ExportRenderer(typeof(Shell), typeof(ShellRenderer))]
[assembly: ExportRenderer(typeof(Slider), typeof(SliderRenderer))]
[assembly: ExportRenderer(typeof(Stepper), typeof(StepperRenderer))]
[assembly: ExportRenderer(typeof(SwipeView), typeof(SwipeViewRenderer))]
[assembly: ExportRenderer(typeof(Switch), typeof(SwitchRenderer))]
[assembly: ExportRenderer(typeof(TabbedPage), typeof(TabbedPageRenderer))]
[assembly: ExportRenderer(typeof(TableView), typeof(TableViewRenderer))]
[assembly: ExportRenderer(typeof(TimePicker), typeof(TimePickerRenderer))]
[assembly: ExportRenderer(typeof(WebView), typeof(WebViewRenderer))]

[assembly: ExportCell(typeof(Cell), typeof(CellRenderer))]
[assembly: ExportCell(typeof(Xamarin.Forms.EntryCell), typeof(EntryCellRenderer))]
[assembly: ExportCell(typeof(Xamarin.Forms.TextCell), typeof(TextCellRenderer))]
[assembly: ExportCell(typeof(Xamarin.Forms.ImageCell), typeof(ImageCellRenderer))]
[assembly: ExportCell(typeof(Xamarin.Forms.SwitchCell), typeof(SwitchCellRenderer))]
[assembly: ExportCell(typeof(Xamarin.Forms.ViewCell), typeof(ViewCellRenderer))]

[assembly: ExportRenderer(typeof(Xamarin.Forms.Shapes.Rectangle), typeof(RectangleRenderer))]
[assembly: ExportRenderer(typeof(Xamarin.Forms.Shapes.Ellipse), typeof(EllipseRenderer))]
[assembly: ExportRenderer(typeof(Xamarin.Forms.Shapes.Path), typeof(PathRenderer))]
[assembly: ExportRenderer(typeof(Xamarin.Forms.Shapes.Polygon), typeof(PolygonRenderer))]
[assembly: ExportRenderer(typeof(Xamarin.Forms.Shapes.Polyline), typeof(PolylineRenderer))]
[assembly: ExportRenderer(typeof(Xamarin.Forms.Shapes.Line), typeof(LineRenderer))]