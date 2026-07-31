using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Issue3106 : ContentPage
	{
		public Issue3106()
		{
			InitializeComponent();
		}
		public Issue3106(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void NewDoesNotThrow(bool useCompiledXaml)
			{
				var p = new Issue3106(useCompiledXaml);
			}
		}
	}
}

