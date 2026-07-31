using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class TypeLoader : ContentPage
	{
		public TypeLoader()
		{
			InitializeComponent();
		}

		public TypeLoader(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
				Application.Current = new MockApplication();
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void LoadTypeFromXmlns(bool useCompiledXaml)
			{
				TypeLoader layout = null;
				AssertEx.DoesNotThrow(() => layout = new TypeLoader(useCompiledXaml));
				Assert.NotNull(layout.customview0);
				Assert.IsType<CustomView>(layout.customview0);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void LoadTypeFromXmlnsWithoutAssembly(bool useCompiledXaml)
			{
				TypeLoader layout = null;
				AssertEx.DoesNotThrow(() => layout = new TypeLoader(useCompiledXaml));
				Assert.NotNull(layout.customview1);
				Assert.IsType<CustomView>(layout.customview1);
			}
		}
	}
}