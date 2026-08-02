using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	internal class BlockingTicker : Ticker
	{
		bool _enabled;

		protected override void EnableTimer()
		{
			_enabled = true;

			while (_enabled)
			{
				SendSignals(16);
			}
		}

		protected override void DisableTimer()
		{
			_enabled = false;
		}
	}

	internal class AsyncTicker : Ticker
	{
		bool _systemEnabled = true;
		public override bool SystemEnabled => _systemEnabled;

		bool _enabled;

		public void SetEnabled(bool enabled)
		{
			_systemEnabled = enabled;

			_enabled = enabled;

			OnSystemEnabledChanged();
		}

		protected override async void EnableTimer()
		{
			_enabled = true;

			while (_enabled)
			{
				SendSignals(16);
				await Task.Delay(16);
			}
		}

		protected override void DisableTimer()
		{
			_enabled = false;
		}
	}

	public class MotionTests : BaseTestFixture
	{
		public MotionTests()
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
		public void TestLinearTween()
		{
			var tweener = new Tweener(250);

			double value = 0;
			int updates = 0;
			tweener.ValueUpdated += (sender, args) =>
			{
				Assert.True(tweener.Value >= value);
				value = tweener.Value;
				updates++;
			};
			tweener.Start();

			Assert.True(updates >= 10);
		}

		[Fact]
		public void ThrowsWithNullCallback()
		{
			Assert.Throws<ArgumentNullException>(() => new View().Animate("Test", (Action<double>)null));
		}

		[Fact]
		public void ThrowsWithNullTransform()
		{
			Assert.Throws<ArgumentNullException>(() => new View().Animate<float>("Test", null, f => { }));
		}

		[Fact]
		public void ThrowsWithNullSelf()
		{
			Assert.Throws<ArgumentNullException>(() => AnimationExtensions.Animate(null, "Foo", d => (float)d, f => { }));
		}

		[Fact]
		public void Kinetic()
		{
			var view = new View();
			var resultList = new List<Tuple<double, double>>();
			view.AnimateKinetic(
				name: "Kinetics",
				callback: (distance, velocity) =>
				{
					resultList.Add(new Tuple<double, double>(distance, velocity));
					return true;
				},
				velocity: 100,
				drag: 1);

			Assert.NotEmpty(resultList);
			int checkVelo = 100;
			int dragStep = 16;

			foreach (var item in resultList)
			{
				checkVelo -= dragStep;
				Assert.Equal(checkVelo, item.Item2);
				Assert.Equal(checkVelo * dragStep, item.Item1);
			}
		}

		[Fact]
		public void KineticFinished()
		{
			var view = new View();
			bool finished = false;
			view.AnimateKinetic(
				name: "Kinetics",
				callback: (distance, velocity) => true,
				velocity: 100,
				drag: 1,
				finished: () => finished = true);

			Assert.True(finished);
		}
	}

	public class TickerSystemEnabledTests : IDisposable
	{
		public TickerSystemEnabledTests()
		{
			Device.PlatformServices = new MockPlatformServices();
			Ticker.Default = new AsyncTicker();
		}

		public void Dispose()
		{
			Device.PlatformServices = null;
			Ticker.Default = null;
		}

		static async Task DisableTicker()
		{
			await Task.Delay(32);
			((AsyncTicker)Ticker.Default).SetEnabled(false);
		}

		static async Task EnableTicker()
		{
			await Task.Delay(32);
			((AsyncTicker)Ticker.Default).SetEnabled(true);
		}

		async Task SwapFadeViews(View view1, View view2)
		{
			await view1.FadeTo(0, 1000);
			await view2.FadeTo(1, 1000);
		}

		[Fact]
		public async Task DisablingTickerFinishesAnimationInProgress()
		{
			var view = new View { Opacity = 1 };

			await Task.WhenAll(view.FadeTo(0, 2000), DisableTicker());

			Assert.Equal(0, view.Opacity);
		}

		[Fact]
		public async Task DisablingTickerFinishesAllAnimationsInChain()
		{
			var view1 = new View { Opacity = 1 };
			var view2 = new View { Opacity = 0 };

			await Task.WhenAll(SwapFadeViews(view1, view2), DisableTicker());

			Assert.Equal(0, view1.Opacity);

		}

		static Task<bool> RepeatFade(View view)
		{
			var tcs = new TaskCompletionSource<bool>();
			var fadeIn = new Animation(d => { view.Opacity = d; }, 0, 1);
			var i = 0;

			fadeIn.Commit(view, "fadeIn", length: 2000, repeat: () => ++i < 2, finished: (d, b) =>
			{
				tcs.SetResult(b);
			});

			return tcs.Task;
		}

		[Fact]
		public async Task DisablingTickerPreventsAnimationFromRepeating()
		{
			var view = new View { Opacity = 0 };

			await Task.WhenAll(RepeatFade(view), DisableTicker());

			Assert.Equal(1, view.Opacity);
		}

		[Fact]
		public async Task NewAnimationsFinishImmediatelyWhenTickerDisabled()
		{
			var view = new View { Opacity = 1 };

			await DisableTicker();

			await view.RotateYTo(200);

			Assert.Equal(200, view.RotationY);
		}

		[Fact]
		public async Task AnimationExtensionsReturnTrueIfAnimationsDisabled()
		{
			await DisableTicker();

			var label = new Label { Text = "Foo" };
			var result = await label.ScaleTo(2, 500);

			Assert.True(result);
		}

		[Fact]
		public async Task CanExitAnimationLoopIfAnimationsDisabled()
		{
			await DisableTicker();

			var run = true;
			var label = new Label { Text = "Foo" };

			while (run)
			{
				await label.ScaleTo(2, 500);
				run = !(await label.ScaleTo(0.5, 500));
			}
		}

		[Fact]
		public async Task CanCheckThatAnimationsAreEnabled()
		{
			await EnableTicker();
			Assert.True(Animation.IsEnabled);

			await DisableTicker();
			Assert.False(Animation.IsEnabled);
		}
	}
}
