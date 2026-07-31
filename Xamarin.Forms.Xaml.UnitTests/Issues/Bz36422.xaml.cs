using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Bz36422Control : ContentView
	{
		public IList<ContentView> Views { get; set; }
	}

	public partial class Bz36422 : ContentPage
	{
		public Bz36422()
		{
			InitializeComponent();
		}

		public Bz36422(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			public void xArrayCanBeAssignedToIListT(bool useCompiledXaml)
			{
				var layout = new Bz36422(useCompiledXaml);
				Assert.Equal(3, layout.control.Views.Count);
			}
		}
	}
}