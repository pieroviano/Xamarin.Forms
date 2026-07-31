using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Pr3384 : ContentPage
	{
		public Pr3384()
		{
			InitializeComponent();
		}

		public Pr3384(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices { RuntimePlatform = Device.iOS };
			}

			public void Dispose()
{
				Device.PlatformServices = null;
			}

			[InlineData(false)]
			[InlineData(true)]
			public void RecyclingStrategyIsHandled(bool useCompiledXaml)
			{
				var p = new Pr3384(useCompiledXaml);
				Assert.Equal(ListViewCachingStrategy.RecycleElement, p.listView.CachingStrategy);
			}
		}
	}
}