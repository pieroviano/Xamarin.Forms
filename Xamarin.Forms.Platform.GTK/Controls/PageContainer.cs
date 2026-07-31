namespace Xamarin.Forms.Platform.GTK.Controls
{
	// GTK3 removed Gtk.Object. This type is only ever held in plain IList/List<T>
	// (Carousel.RefreshItemsSource, CarouselPageRenderer), never handed to a GTK
	// container, so it does not need to be a GLib.Object at all.
	public class PageContainer
	{
		public PageContainer(Xamarin.Forms.Page element, int index)
		{
			Page = element;
			Index = index;
		}

		public Xamarin.Forms.Page Page { get; }

		public int Index { get; set; }
	}
}
