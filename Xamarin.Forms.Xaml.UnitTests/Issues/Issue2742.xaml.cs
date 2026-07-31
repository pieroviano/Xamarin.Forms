using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Issue2742BasePage : ContentPage
	{

	}

	public partial class Issue2742 : Issue2742BasePage
	{
		public Issue2742()
		{
			InitializeComponent();
		}

		public Issue2742(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}


		public class Tests
		{
			[InlineData(false)]
			[InlineData(true)]
			public void ToolBarItemsOnContentPageInheritors(bool useCompiledXaml)
			{
				var layout = new Issue2742(useCompiledXaml);
				Assert.IsType<Label>(layout.Content);
				Assert.Equal("test", ((Label)layout.Content).Text);

				Assert.NotNull(layout.ToolbarItems);
				Assert.Equal(2, layout.ToolbarItems.Count);
#pragma warning disable 618
				Assert.Equal("One", layout.ToolbarItems[0].Name);
#pragma warning restore 618
			}
		}
	}
}