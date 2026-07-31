using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz41048 : ContentPage
	{
		public Bz41048()
		{
			InitializeComponent();
		}

		public Bz41048(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			public void Dispose()
{
				Application.Current = null;
				Device.PlatformServices = null;
			}

			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			public void StyleDoesNotOverrideValues(bool useCompiledXaml)
			{
				var layout = new Bz41048(useCompiledXaml);
				var label = layout.label0;
				Assert.Equal(Color.Red, label.TextColor);
				Assert.Equal(FontAttributes.Bold, label.FontAttributes);
				Assert.Equal(LineBreakMode.WordWrap, label.LineBreakMode);
			}
		}
	}
}