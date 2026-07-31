using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz41296 : ContentPage
	{
		public Bz41296()
		{
			InitializeComponent();
		}

		public Bz41296(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{
			[InlineData(true)]
			[InlineData(false)]
			public void MarkupExtensionInDefaultNamespace(bool useCompiledXaml)
			{
				var layout = new Bz41296(useCompiledXaml);
				Assert.Equal("FooBar", layout.TestLabel.Text.ToString());
			}
		}
	}
}