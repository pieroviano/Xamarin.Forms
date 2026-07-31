using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class Gh5240 : ContentPage
	{
		public Gh5240() => InitializeComponent();
		public Gh5240(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(true)]
			public void FailOnUnresolvedDataType(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					AssertEx.DoesNotThrow(() => MockCompiler.Compile(typeof(Gh5240)));
			}
		}
	}
}
