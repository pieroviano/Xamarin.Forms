using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh1766 : ContentPage
	{
		public Gh1766()
		{
			InitializeComponent();
		}

		public Gh1766(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
				Xamarin.Forms.Internals.Registrar.RegisterAll(new Type[0]);
			}

			public void Dispose()
{
				Device.PlatformServices = null;
			}

			[Theory]
			[InlineData(true), InlineData(false)]
			public void CSSPropertiesNotInerited(bool useCompiledXaml)
			{
				var layout = new Gh1766(useCompiledXaml);
				Assert.Equal(Color.Pink, layout.stack.BackgroundColor);
				Assert.Equal(VisualElement.BackgroundColorProperty.DefaultValue, layout.entry.BackgroundColor);
			}
		}
	}
}
