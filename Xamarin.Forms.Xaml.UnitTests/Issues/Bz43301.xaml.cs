using Xunit;
using Xamarin.Forms;

namespace Foo.Xamarin.Bar
{
	public partial class Bz43301 : ContentPage
	{
		public Bz43301()
		{
			InitializeComponent();
		}

		public Bz43301(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			//No need for any actual [Theory]. If this compiles, the bug is fixed.
			public void DoesCompile(bool useCompiledXaml)
			{
				var layout = new Bz43301(useCompiledXaml);
				return;
			}
		}
	}
}