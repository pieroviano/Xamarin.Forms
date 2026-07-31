using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	//this covers Issue2125 as well
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class Issue2450 : ContentPage
	{
		public Issue2450()
		{
			InitializeComponent();
		}

		public Issue2450(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			[InlineData(false)]
			public void ThrowMeaningfulExceptionOnDuplicateXName(bool useCompiledXaml)
			{
				var layout = new Issue2450(useCompiledXaml);
				Assert.Throws(new XamlParseExceptionConstraint(11, 13, m => m == "An element with the name \"label0\" already exists in this NameScope"),
							  () => (layout.Resources["foo"] as Forms.DataTemplate).CreateContent());
			}
		}
	}
}