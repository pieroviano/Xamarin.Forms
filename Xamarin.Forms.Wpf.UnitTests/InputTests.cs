using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xunit;

namespace Wpf.UnitTests
{
	/// <summary>
	/// GTK input as WPF input: key values and virtual keys, the Preview/bubble pair, text input, key bindings,
	/// the default and cancel buttons. The events enter where GTK's controllers call in (NativeInput).
	/// </summary>
	public class InputTests : WpfTestBase
	{
		const uint KeyA = 'a', KeyReturn = 0xFF0D, KeyEscape = 0xFF1B, KeyF5 = 0xFFC2, KeyShiftL = 0xFFE1;

		[Theory]
		[InlineData('a', Key.A)]
		[InlineData('Z', Key.Z)]
		[InlineData('7', Key.D7)]
		[InlineData('!', Key.D1)]
		[InlineData(0xFF0D, Key.Enter)]
		[InlineData(0xFF1B, Key.Escape)]
		[InlineData(0xFF09, Key.Tab)]
		[InlineData(0xFF08, Key.Back)]
		[InlineData(0xFFBE, Key.F1)]
		[InlineData(0xFFC2, Key.F5)]
		[InlineData(0xFFB3, Key.NumPad3)]
		[InlineData(0xFF51, Key.Left)]
		[InlineData(0xFFE3, Key.LeftCtrl)]
		[InlineData(';', Key.Oem1)]
		[InlineData(0x20, Key.Space)]
		public void GdkKeyValuesAreWpfKeys(uint keyval, Key expected) => Assert.Equal(expected, GdkKeys.ToKey(keyval));

		[Theory]
		[InlineData(Key.A, 0x41)]
		[InlineData(Key.D0, 0x30)]
		[InlineData(Key.Enter, 0x0D)]
		[InlineData(Key.Escape, 0x1B)]
		[InlineData(Key.F1, 0x70)]
		[InlineData(Key.F24, 0x87)]
		[InlineData(Key.NumPad0, 0x60)]
		[InlineData(Key.LeftShift, 0xA0)]
		[InlineData(Key.Oem1, 0xBA)]
		[InlineData(Key.Delete, 0x2E)]
		public void VirtualKeysAreWpfs(Key key, int virtualKey)
		{
			Assert.Equal(virtualKey, KeyInterop.VirtualKeyFromKey(key));
			Assert.Equal(key, KeyInterop.KeyFromVirtualKey(virtualKey));
		}

		[Fact]
		public void AKeyIsPreviewedFromTheWindowDownThenBubblesUp()
		{
			Run(() =>
			{
				var box = new TextBox();
				var window = Host(new StackPanel { Children = { box } });
				Keyboard.FocusedElement = box;
				var order = new List<string>();
				window.PreviewKeyDown += (s, e) => order.Add("window preview");
				box.PreviewKeyDown += (s, e) => order.Add("box preview");
				box.KeyDown += (s, e) =>
				{
					order.Add("box");
					Assert.Equal(Key.F5, e.Key);
				};
				window.KeyDown += (s, e) => order.Add("window");

				NativeInput.OnKey(window, KeyF5, 0, true);

				Assert.Equal(new[] { "window preview", "box preview", "box", "window" }, order);
				window.Close();
			});
		}

		[Fact]
		public void AHandledKeyIsKeptFromTheWidget()
		{
			Run(() =>
			{
				var window = Host(new StackPanel());
				window.PreviewKeyDown += (s, e) => e.Handled = true;

				Assert.True(NativeInput.OnKey(window, KeyA, 0, true));
				window.Close();
			});
		}

		[Fact]
		public void AltMakesASystemKey()
		{
			Run(() =>
			{
				var window = Host(new StackPanel());
				KeyEventArgs seen = null;
				window.KeyDown += (s, e) => seen = e;

				NativeInput.OnKey(window, 'x', Gdk.ModifierType.AltMask, true);

				Assert.Equal(Key.System, seen.Key);
				Assert.Equal(Key.X, seen.SystemKey);
				window.Close();
			});
		}

		[Fact]
		public void ModifierKeysFollowTheKeyboard()
		{
			Run(() =>
			{
				var window = Host(new StackPanel());

				NativeInput.OnKey(window, KeyShiftL, 0, true);
				Assert.Equal(ModifierKeys.Shift, Keyboard.Modifiers);
				Assert.True(Keyboard.IsKeyDown(Key.LeftShift));
				NativeInput.OnKey(window, KeyShiftL, Gdk.ModifierType.ShiftMask, false);
				Assert.Equal(ModifierKeys.None, Keyboard.Modifiers);
				window.Close();
			});
		}

		[Fact]
		public void TextInputFollowsAnUnhandledKey()
		{
			Run(() =>
			{
				var window = Host(new StackPanel());
				var texts = new List<string>();
				window.PreviewTextInput += (s, e) => texts.Add(e.Text);

				NativeInput.OnKey(window, KeyA, 0, true);
				NativeInput.OnKey(window, KeyReturn, 0, true);

				Assert.Equal(new[] { "a", "\r" }, texts);
				window.Close();
			});
		}

		[Fact]
		public void AHandledPreviewTextInputKeepsTheCharacterFromTheWidget()
		{
			Run(() =>
			{
				var window = Host(new StackPanel());
				window.PreviewTextInput += (s, e) => e.Handled = true;

				Assert.True(NativeInput.OnKey(window, KeyA, 0, true));
				window.Close();
			});
		}

		[Fact]
		public void AKeyBindingRunsItsCommand()
		{
			Run(() =>
			{
				var window = Host(new StackPanel());
				var command = new RoutedCommand();
				var ran = 0;
				window.CommandBindings.Add(new CommandBinding(command, (s, e) => ran++, (s, e) => e.CanExecute = true));
				window.InputBindings.Add(new KeyBinding(command, Key.S, ModifierKeys.Control));

				Assert.True(NativeInput.OnKey(window, 's', Gdk.ModifierType.ControlMask, true));
				NativeInput.OnKey(window, 's', 0, true);

				Assert.Equal(1, ran);
				window.Close();
			});
		}

		[Fact]
		public void EnterClicksTheDefaultButtonAndEscapeTheCancelOne()
		{
			Run(() =>
			{
				var ok = new Button { Content = "OK", IsDefault = true };
				var cancel = new Button { Content = "Cancel", IsCancel = true };
				var clicks = new List<string>();
				ok.Click += (s, e) => clicks.Add("ok");
				cancel.Click += (s, e) => clicks.Add("cancel");
				var window = Host(new StackPanel { Children = { ok, cancel } });

				NativeInput.OnKey(window, KeyReturn, 0, true);
				NativeInput.OnKey(window, KeyEscape, 0, true);

				Assert.Equal(new[] { "ok", "cancel" }, clicks);
				window.Close();
			});
		}

		[Fact]
		public void AltAndAnAccessKeyClickTheButton()
		{
			Run(() =>
			{
				var run = new Button { Content = "_Run" };
				var clicked = false;
				run.Click += (s, e) => clicked = true;
				var window = Host(new StackPanel { Children = { run } });

				NativeInput.OnKey(window, 'r', Gdk.ModifierType.AltMask, true);

				Assert.True(clicked);
				window.Close();
			});
		}

		[Fact]
		public void AButtonPressIsRoutedAsWpfMouseEvents()
		{
			Run(() =>
			{
				var window = Host(new StackPanel());
				var events = new List<string>();
				window.PreviewMouseDown += (s, e) => events.Add("preview down");
				window.MouseLeftButtonDown += (s, e) => events.Add("left down " + e.ClickCount);
				window.MouseRightButtonUp += (s, e) => events.Add("right up");

				NativeInput.OnButton(window, window.NativeWindow, 1, 2, 5, 5, true, 0);
				NativeInput.OnButton(window, window.NativeWindow, 3, 1, 5, 5, false, 0);

				Assert.Equal(new[] { "preview down", "left down 2", "right up" }, events);
				Assert.Equal(MouseButtonState.Pressed, Mouse.LeftButton);
				window.Close();
			});
		}

		[Fact]
		public void MouseMoveReportsThePositionRelativeToTheWindow()
		{
			Run(() =>
			{
				var window = Host(new StackPanel());
				Point? at = null;
				window.MouseMove += (s, e) => at = e.GetPosition(window);

				NativeInput.OnMotion(window, window.NativeWindow, 12, 34, 0);

				Assert.Equal(new Point(12, 34), at);
				window.Close();
			});
		}

		[Fact]
		public void TheRightButtonOpensTheContextMenuOfTheElementUnderIt()
		{
			Run(() =>
			{
				var menu = new ContextMenu { Items = { new MenuItem { Header = "Item" } } };
				var window = Host(new StackPanel());
				window.ContextMenu = menu;

				NativeInput.OnButton(window, window.NativeWindow, 3, 1, 5, 5, false, 0);

				Assert.Same(window, menu.PlacementTarget);
				Assert.True(menu.IsOpen);
				window.Close();
			});
		}
	}
}
