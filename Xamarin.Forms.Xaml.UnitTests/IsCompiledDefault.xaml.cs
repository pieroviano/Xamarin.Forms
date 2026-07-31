using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class IsCompiledDefault : ContentPage
	{
		public IsCompiledDefault()
		{
			InitializeComponent();
		}

		public IsCompiledDefault(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[InlineData(false)]
			[InlineData(true)]
			public void IsCompiled(bool useCompiledXaml)
			{
				var layout = new IsCompiledDefault(useCompiledXaml);
				Assert.Equal(true, typeof(IsCompiledDefault).IsCompiled());
			}
		}
	}
}