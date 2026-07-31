using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh1566
	{
		public Gh1566()
		{
			InitializeComponent();
		}

		public Gh1566(bool useCompiledXaml)
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

			[Theory]
			[InlineData(true), InlineData(false)]
			public void ObsoletePropsDoNotThrow(bool useCompiledXaml)
			{
				var layout = new Gh1566(useCompiledXaml);
				Assert.Equal(Color.Red, layout.frame.BorderColor);
			}
		}
	}
}
