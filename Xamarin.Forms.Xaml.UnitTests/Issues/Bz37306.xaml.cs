using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz37306 : ContentPage
	{
		public Bz37306()
		{
			InitializeComponent();
		}

		public Bz37306(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			public void xStringInResourcesDictionaries(bool useCompiledXaml)
			{
				var layout = new Bz37306(useCompiledXaml);
				Assert.Equal("Mobile App", layout.Resources["AppName"]);
				Assert.Equal("Mobile App", layout.Resources["ApplicationName"]);
			}
		}
	}
}