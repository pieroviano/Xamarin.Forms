using System;
using System.Collections.Generic;

using Xunit;
using Xamarin.Forms.Build.Tasks;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh4751VM
	{
		public string Title { get; }
		public Gh4751VM(string title = null) => Title = title; //a .ctor with a default value IS NOT a default .ctor
	}

	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class Gh4751 : ContentPage
	{
		public Gh4751() => InitializeComponent();
		public Gh4751(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void ErrorOnMissingDefaultCtor(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					Assert.Throws<BuildException>(() => MockCompiler.Compile(typeof(Gh4751)));
				else
					Assert.Throws<XamlParseException>(() => new Gh4751(useCompiledXaml));
			}
		}
	}
}
