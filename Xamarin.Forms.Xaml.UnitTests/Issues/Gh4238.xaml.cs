using Xunit;

namespace Xamarin.Forms.Xaml.UnitTests
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class Gh4238
	{
		public System.Collections.ArrayList Property { get; set; }

		// InitializeComponent is private and generated, so expose it for the test below.
		public void Load() => InitializeComponent();

		// The test lives in a nested class rather than on Gh4238 itself: xUnit requires a
		// test class to have exactly one public constructor, and the XAML-compiled type does
		// not satisfy that. Every other test in this suite uses the same nested-Tests shape.
		public class Tests
		{
			[Fact]
			public void Test()
			{
				var layout = new Gh4238();
				layout.Load();
				Assert.Equal(0f, layout.Property[0]);
			}
		}
	}
}
