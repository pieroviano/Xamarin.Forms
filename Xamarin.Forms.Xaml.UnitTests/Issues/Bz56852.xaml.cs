using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz56852
	{
		public Bz56852()
		{
			InitializeComponent();
		}

		public Bz56852(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			public void Dispose()
{
				Device.PlatformServices = null;
				Application.Current = null;
			}

			[InlineData(true)]
			[InlineData(false)]
			public void DynamicResourceApplyingOrder(bool useCompiledXaml)
			{
				var layout = new Bz56852(useCompiledXaml);
				Assert.Equal(50, layout.label.FontSize);
			}
		}
	}
}
