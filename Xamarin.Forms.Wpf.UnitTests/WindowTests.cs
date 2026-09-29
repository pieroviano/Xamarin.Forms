using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Xunit;

namespace Wpf.UnitTests
{
	/// <summary>Windows: a GTK window each, closing (cancellable), dialogs as nested loops, owners.</summary>
	public class WindowTests : WpfTestBase
	{
		[Fact]
		public void ShowingAWindowOpensAGtkWindowWithItsProperties()
		{
			Run(() =>
			{
				var window = new Window { Title = "Title", ResizeMode = ResizeMode.NoResize, Content = new Border() };
				Assert.Null(window.NativeWindow);

				window.Show();
				Pump(window);

				Assert.NotNull(window.NativeWindow);
				Assert.Equal("Title", window.NativeWindow.Title);
				Assert.False(window.NativeWindow.Resizable);
				Assert.True(window.IsVisible);
				window.Title = "Other";
				Assert.Equal("Other", window.NativeWindow.Title);
				window.Close();
			});
		}

		[Fact]
		public void ClosingCanBeCancelledAndThenClosedFollows()
		{
			Run(() =>
			{
				var window = Shown(new Window());
				var events = new List<string>();
				var cancel = true;
				window.Closing += (s, e) =>
				{
					events.Add("closing");
					e.Cancel = cancel;
				};
				window.Closed += (s, e) => events.Add("closed");

				window.Close();
				Assert.True(window.IsVisible);

				cancel = false;
				window.Close();

				Assert.Equal(new[] { "closing", "closing", "closed" }, events);
				Assert.False(window.IsVisible);
				Assert.False(window.NativeWindow.Visible);
			});
		}

		[Fact]
		public void AClosedWindowCannotBeShownAgain()
		{
			Run(() =>
			{
				var window = Shown(new Window());
				window.Close();

				Assert.Throws<InvalidOperationException>(() => window.Show());
			});
		}

		[Fact]
		public void AWindowManagerCloseRequestIsAWpfClose()
		{
			Run(() =>
			{
				var window = Shown(new Window());
				var closing = 0;
				var cancel = true;
				window.Closing += (s, e) =>
				{
					closing++;
					e.Cancel = cancel;
				};

				window.NativeWindow.Close();
				Pump(window);

				Assert.Equal(1, closing);
				Assert.True(window.IsVisible);
				cancel = false;
				window.Close();
				Assert.False(window.IsVisible);
			});
		}

		[Fact]
		public void ShowDialogReturnsTheDialogResultOnceItIsSet()
		{
			Run(() =>
			{
				var dialog = new Window { Content = new Button() };
				Later(() => dialog.DialogResult = true);

				var result = dialog.ShowDialog();

				Assert.True(result);
				Assert.False(dialog.IsVisible);
			});
		}

		[Fact]
		public void TheCancelButtonOfADialogAnswersFalse()
		{
			Run(() =>
			{
				var cancel = new Button { IsCancel = true };
				var dialog = new Window { Content = cancel };
				Later(() => cancel.PerformClick());

				Assert.False(dialog.ShowDialog());
			});
		}

		[Fact]
		public void DialogResultOutsideADialogThrows()
		{
			Run(() =>
			{
				var window = new Window();

				Assert.Throws<InvalidOperationException>(() => window.DialogResult = true);
				window.Close();
			});
		}

		[Fact]
		public void ClosingAnOwnerClosesTheWindowsItOwns()
		{
			Run(() =>
			{
				var owner = Shown(new Window());
				var owned = Shown(new Window { Owner = owner });
				Assert.Same(owner.NativeWindow, owned.NativeWindow.TransientFor);

				owner.Close();

				Assert.False(owned.IsVisible);
			});
		}

		[Fact]
		public void MessageBoxAnswersWithTheButtonClicked()
		{
			Run(() =>
			{
				Later(() =>
				{
					var dialog = LastOpenWindow;
					Assert.Equal("Caption", dialog.Title);
					System.Windows.Input.NativeInput.OnKey(dialog, 0xFF1B, 0, true);
				});

				var answer = MessageBox.Show("Save?", "Caption", MessageBoxButton.YesNo, MessageBoxImage.Question);

				Assert.Equal(MessageBoxResult.No, answer);
			});
		}

		[Fact]
		public void MessageBoxEnterAnswersTheDefaultButton()
		{
			Run(() =>
			{
				Later(() => System.Windows.Input.NativeInput.OnKey(LastOpenWindow, 0xFF0D, 0, true));

				Assert.Equal(MessageBoxResult.OK, MessageBox.Show("Done"));
			});
		}

		[Fact]
		public void TheDispatcherRunsWorkInPriorityOrder()
		{
			Run(() =>
			{
				var order = new List<string>();
				var dispatcher = Dispatcher.CurrentDispatcher;
				dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => order.Add("background")));
				dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => order.Add("normal")));

				Assert.True(PumpUntil(() => order.Count == 2));
				Assert.Equal(new[] { "normal", "background" }, order);
			});
		}

		[Fact]
		public void InvokeOnTheDispatchersThreadRunsAtOnce()
		{
			Run(() =>
			{
				var ran = false;
				Dispatcher.CurrentDispatcher.Invoke(() => ran = true);

				Assert.True(ran);
				Assert.Equal(42, Dispatcher.CurrentDispatcher.Invoke(() => 42));
			});
		}

		[Fact]
		public void PushFrameRunsUntilTheFrameStops()
		{
			Run(() =>
			{
				var frame = new DispatcherFrame();
				var ran = false;
				Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
				{
					ran = true;
					frame.Continue = false;
				}));

				Dispatcher.PushFrame(frame);

				Assert.True(ran);
			});
		}

		[Fact]
		public void ADispatcherTimerTicksUntilStopped()
		{
			Run(() =>
			{
				var ticks = 0;
				var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
				timer.Tick += (s, e) =>
				{
					if (++ticks == 3)
						timer.Stop();
				};
				timer.Start();

				Assert.True(PumpUntil(() => ticks == 3));
				Assert.False(PumpUntil(() => ticks > 3, timeoutMs: 100));
				Assert.False(timer.IsEnabled);
			});
		}

		[Fact]
		public void ChangingTheIntervalOfARunningTimerRestartsIt()
		{
			Run(() =>
			{
				var ticks = 0;
				var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
				timer.Tick += (s, e) => ticks++;
				timer.Start();

				timer.Interval = TimeSpan.FromMilliseconds(10);

				Assert.True(PumpUntil(() => ticks > 0));
				timer.Stop();
			});
		}
	}
}
