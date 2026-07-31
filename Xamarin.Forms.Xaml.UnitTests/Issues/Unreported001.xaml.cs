using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class U001Page : ContentPage
	{
		public U001Page()
		{
			;
		}

	}

	public partial class Unreported001 : TabbedPage
	{
		public Unreported001()
		{
			InitializeComponent();
		}

		public Unreported001(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[InlineData(false)]
			[InlineData(true)]
			public void DoesNotThrow(bool useCompiledXaml)
			{
				var p = new Unreported001(useCompiledXaml);
				Assert.IsType<U001Page>(p.navpage.CurrentPage);
			}
		}
	}
}