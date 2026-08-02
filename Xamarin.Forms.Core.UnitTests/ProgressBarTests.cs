using System;
using System.Collections.Generic;
using System.Linq;

using Xamarin.Forms.Internals;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class ProgressBarTests : BaseTestFixture
	{
		public ProgressBarTests()
		{
			Device.PlatformServices = new MockPlatformServices();
			Ticker.Default = new BlockingTicker();
		}

		public override void Dispose()
		{
			base.Dispose();
			Device.PlatformServices = null;
			Ticker.Default = null;
		}

		[Fact]
		public void TestClamp()
		{
			ProgressBar bar = new ProgressBar();

			bar.Progress = 2;
			Assert.Equal(1, bar.Progress);

			bar.Progress = -1;
			Assert.Equal(0, bar.Progress);
		}

		[Fact]
		public void TestProgressTo()
		{
			var bar = new ProgressBar();

			bar.ProgressTo(0.8, 250, Easing.Linear);

			Assert.Equal(0.8, bar.Progress, 0.001);
		}
	}
}
