using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class Gh2034 : ContentPage
	{
		public Gh2034()
		{
			InitializeComponent();
		}

		public Gh2034(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[Theory]
			[InlineData(true)]
			public void Compiles(bool useCompiledXaml)
			{
				if (!useCompiledXaml)
					return;
				MockCompiler.Compile(typeof(Gh2034));
				return;
			}
		}
	}
}
