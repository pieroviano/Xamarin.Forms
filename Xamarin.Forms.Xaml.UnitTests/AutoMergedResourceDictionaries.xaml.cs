using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class AutoMergedResourceDictionaries : ContentPage
	{
		public AutoMergedResourceDictionaries()
		{
			InitializeComponent();
		}

		public AutoMergedResourceDictionaries(bool useCompiledXaml)
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
			[InlineData(false)]
			[InlineData(true)]
			public void AutoMergedRd(bool useCompiledXaml)
			{
				var layout = new AutoMergedResourceDictionaries(useCompiledXaml);
				Assert.Equal(Color.Purple, layout.label.TextColor);
				Assert.Equal(Color.FromHex("#FF96F3"), layout.label.BackgroundColor);
			}
		}
	}
}
