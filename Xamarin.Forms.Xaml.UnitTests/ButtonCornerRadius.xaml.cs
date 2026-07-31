using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class ButtonCornerRadius : ContentPage
	{
		public ButtonCornerRadius()
		{
			InitializeComponent();
		}

		public ButtonCornerRadius(bool useCompiledXaml)
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
				Device.PlatformServices = null;
			}

			[InlineData(false)]
			[InlineData(true)]
			public void EscapedStringsAreTreatedAsLiterals(bool useCompiledXaml)
			{
				var layout = new ButtonCornerRadius(useCompiledXaml);
				Assert.Equal(0, layout.Button0.CornerRadius);
			}
		}
	}
}
