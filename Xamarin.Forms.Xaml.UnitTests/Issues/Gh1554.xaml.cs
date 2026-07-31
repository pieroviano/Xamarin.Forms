using System;
using System.Linq;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh1554
	{
		public Gh1554()
		{
			InitializeComponent();
		}

		public Gh1554(bool useCompiledXaml)
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
			public void NestedRDAreOnlyProcessedOnce(bool useCompiledXaml)
			{
				var layout = new Gh1554(useCompiledXaml);
				Assert.Equal("label0", layout.Resources.MergedDictionaries.First().First().Key);
			}
		}
	}
}
