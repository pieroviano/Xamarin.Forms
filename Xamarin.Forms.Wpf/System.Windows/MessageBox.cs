using System.Windows.Controls;
using System.Windows.Input;

namespace System.Windows
{
	public enum MessageBoxButton
	{
		OK = 0,
		OKCancel = 1,
		YesNoCancel = 3,
		YesNo = 4,
	}

	public enum MessageBoxImage
	{
		None = 0,
		Error = 16,
		Hand = 16,
		Stop = 16,
		Question = 32,
		Exclamation = 48,
		Warning = 48,
		Asterisk = 64,
		Information = 64,
	}

	public enum MessageBoxResult
	{
		None = 0,
		OK = 1,
		Cancel = 2,
		Yes = 6,
		No = 7,
	}

	[Flags]
	public enum MessageBoxOptions
	{
		None = 0,
		DefaultDesktopOnly = 131072,
		RightAlign = 524288,
		RtlReading = 1048576,
		ServiceNotification = 2097152,
	}

	/// <summary>
	/// A modal message: a window of this library's own, shown with <see cref="Window.ShowDialog"/>. Escape (or the
	/// window's close button) answers Cancel - or No, or OK, when there is no Cancel - as WPF's does.
	/// </summary>
	public static class MessageBox
	{
		public static MessageBoxResult Show(string messageBoxText) =>
			Show(null, messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);

		public static MessageBoxResult Show(string messageBoxText, string caption) =>
			Show(null, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);

		public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button) =>
			Show(null, messageBoxText, caption, button, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);

		public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon) =>
			Show(null, messageBoxText, caption, button, icon, MessageBoxResult.None, MessageBoxOptions.None);

		public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult) =>
			Show(null, messageBoxText, caption, button, icon, defaultResult, MessageBoxOptions.None);

		public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options) =>
			Show(null, messageBoxText, caption, button, icon, defaultResult, options);

		public static MessageBoxResult Show(Window owner, string messageBoxText) =>
			Show(owner, messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);

		public static MessageBoxResult Show(Window owner, string messageBoxText, string caption) =>
			Show(owner, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);

		public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button) =>
			Show(owner, messageBoxText, caption, button, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);

		public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon) =>
			Show(owner, messageBoxText, caption, button, icon, MessageBoxResult.None, MessageBoxOptions.None);

		public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult) =>
			Show(owner, messageBoxText, caption, button, icon, defaultResult, MessageBoxOptions.None);

		public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
		{
			var answers = Answers(button);
			var result = CloseResult(button);

			var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
			var dialog = new Window
			{
				Title = caption ?? string.Empty,
				SizeToContent = SizeToContent.WidthAndHeight,
				ResizeMode = ResizeMode.NoResize,
				WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
				Owner = owner,
				ShowInTaskbar = false,
			};

			foreach (var answer in answers)
			{
				var b = new Button
				{
					Content = Caption(answer),
					MinWidth = 75,
					Margin = new Thickness(6, 0, 0, 0),
					IsDefault = defaultResult == MessageBoxResult.None ? answer == answers[0] : answer == defaultResult,
					IsCancel = answer == result,
				};
				b.Click += (s, e) =>
				{
					result = answer;
					dialog.Close();
				};
				buttons.Children.Add(b);
			}

			var text = new TextBlock { Text = messageBoxText ?? string.Empty, TextWrapping = TextWrapping.Wrap, MaxWidth = 480, VerticalAlignment = VerticalAlignment.Center };
			var body = new DockPanel { Margin = new Thickness(12) };
			var glyph = Glyph(icon);
			if (glyph != null)
			{
				var mark = new TextBlock { Text = glyph, FontSize = 28, Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Top };
				DockPanel.SetDock(mark, Dock.Left);
				body.Children.Add(mark);
			}

			DockPanel.SetDock(buttons, Dock.Bottom);
			body.Children.Add(buttons);
			body.Children.Add(text);
			dialog.Content = body;
			dialog.ShowDialog();
			return result;
		}

		static MessageBoxResult[] Answers(MessageBoxButton button)
		{
			switch (button)
			{
				case MessageBoxButton.OKCancel:
					return new[] { MessageBoxResult.OK, MessageBoxResult.Cancel };
				case MessageBoxButton.YesNo:
					return new[] { MessageBoxResult.Yes, MessageBoxResult.No };
				case MessageBoxButton.YesNoCancel:
					return new[] { MessageBoxResult.Yes, MessageBoxResult.No, MessageBoxResult.Cancel };
				default:
					return new[] { MessageBoxResult.OK };
			}
		}

		/// <summary>What closing the box without a button answers: Cancel, else No for Yes/No, else OK.</summary>
		static MessageBoxResult CloseResult(MessageBoxButton button) =>
			button == MessageBoxButton.YesNo ? MessageBoxResult.No : button == MessageBoxButton.OK ? MessageBoxResult.OK : MessageBoxResult.Cancel;

		static string Caption(MessageBoxResult answer)
		{
			switch (answer)
			{
				case MessageBoxResult.Yes:
					return "_Yes";
				case MessageBoxResult.No:
					return "_No";
				case MessageBoxResult.Cancel:
					return "Cancel";
				default:
					return "OK";
			}
		}

		static string Glyph(MessageBoxImage icon)
		{
			switch (icon)
			{
				case MessageBoxImage.Error:
					return "⛔";
				case MessageBoxImage.Question:
					return "❓";
				case MessageBoxImage.Warning:
					return "⚠";
				case MessageBoxImage.Information:
					return "ℹ";
				default:
					return null;
			}
		}
	}
}
