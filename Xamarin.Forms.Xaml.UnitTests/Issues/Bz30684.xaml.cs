using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz30684 : ContentPage
	{
		public Bz30684()
		{
			InitializeComponent();
		}

		public Bz30684(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			public void XReferenceFindObjectsInParentNamescopes(bool useCompiledXaml)
			{
				var layout = new Bz30684(useCompiledXaml);
				var cell = (TextCell)layout.listView.TemplatedItems.GetOrCreateContent(0, null);
				Assert.Equal("Foo", cell.Text);
			}
		}
	}
}