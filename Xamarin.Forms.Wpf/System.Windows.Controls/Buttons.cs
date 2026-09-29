using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows.Controls.Primitives
{
	public enum ClickMode
	{
		Release,
		Press,
		Hover,
	}

	/// <summary>A clickable control: its <see cref="Click"/> runs its command too.</summary>
	public abstract class ButtonBase : ContentControl, ICommandSource
	{
		public static readonly RoutedEvent ClickEvent =
			EventManager.RegisterRoutedEvent("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ButtonBase));

		public static readonly XF.BindableProperty CommandProperty = Dp.Register<ButtonBase>(nameof(Command), typeof(ICommand), null);
		public static readonly XF.BindableProperty CommandParameterProperty = Dp.Register<ButtonBase>(nameof(CommandParameter), typeof(object), null);
		public static readonly XF.BindableProperty CommandTargetProperty = Dp.Register<ButtonBase>(nameof(CommandTarget), typeof(IInputElement), null);
		public static readonly XF.BindableProperty ClickModeProperty = Dp.Register<ButtonBase>(nameof(ClickMode), typeof(ClickMode), ClickMode.Release);
		public static readonly XF.BindableProperty IsPressedProperty = Dp.Register<ButtonBase>(nameof(IsPressed), typeof(bool), false);

		public event RoutedEventHandler Click { add => AddHandler(ClickEvent, value); remove => RemoveHandler(ClickEvent, value); }

		public ICommand Command
		{
			get => Get<ICommand>(CommandProperty);
			set => SetValue(CommandProperty, value);
		}

		public object CommandParameter
		{
			get => Get<object>(CommandParameterProperty);
			set => SetValue(CommandParameterProperty, value);
		}

		public IInputElement CommandTarget
		{
			get => Get<IInputElement>(CommandTargetProperty);
			set => SetValue(CommandTargetProperty, value);
		}

		public ClickMode ClickMode
		{
			get => Get<ClickMode>(ClickModeProperty);
			set => SetValue(ClickModeProperty, value);
		}

		public bool IsPressed
		{
			get => Get<bool>(IsPressedProperty);
			protected set => SetValue(IsPressedProperty, value);
		}

		/// <summary>Raises <see cref="Click"/>, then runs the command.</summary>
		protected virtual void OnClick()
		{
			RaiseEvent(new RoutedEventArgs(ClickEvent, this));
			CommandHelpers.Execute(Command, CommandParameter, CommandTarget ?? this);
		}

		/// <summary>What a click by the user does: <see cref="OnClick"/>, when the button can be used.</summary>
		internal void PerformClick()
		{
			if (IsEnabled)
				OnClick();
		}

		protected override bool IsEnabledCore => Command == null || CommandHelpers.CanExecute(Command, CommandParameter, CommandTarget ?? this);

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property.Bindable == CommandProperty)
			{
				if (e.OldValue is ICommand old)
					old.CanExecuteChanged -= OnCanExecuteChanged;
				if (e.NewValue is ICommand command)
					command.CanExecuteChanged += OnCanExecuteChanged;
				CoerceIsEnabled();
			}
		}

		void OnCanExecuteChanged(object sender, EventArgs e) => CoerceIsEnabled();

		/// <summary>A routed command finds its bindings up the tree: a new place may enable or disable it.</summary>
		internal override void OnLogicalParentChanged(DependencyObject oldParent)
		{
			base.OnLogicalParentChanged(oldParent);
			if (Command != null)
				CoerceIsEnabled();
		}

		/// <summary>WPF: a focused button clicks on Enter, and on Space when the key comes up.</summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled || !ReferenceEquals(e.OriginalSource, this))
				return;

			if (e.Key == Key.Enter)
			{
				e.Handled = true;
				PerformClick();
			}
			else if (e.Key == Key.Space)
			{
				e.Handled = true;
			}
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			if (!e.Handled && e.Key == Key.Space && ReferenceEquals(e.OriginalSource, this))
			{
				e.Handled = true;
				PerformClick();
			}
		}

		internal override XF.View CreateNativeView()
		{
			var button = new XF.Button { Padding = new XF.Thickness(4, 1) };
			button.Clicked += (s, e) => OnNativeClicked();
			button.Pressed += (s, e) => IsPressed = true;
			button.Released += (s, e) => IsPressed = false;
			return button;
		}

		/// <summary>The native button was clicked by the user.</summary>
		internal virtual void OnNativeClicked() => PerformClick();

		internal override XF.View TextView => NativeView as XF.Button;

		internal override void ApplyContent()
		{
			if (NativeView is XF.Button button)
				button.Text = ContentText;
			else
				base.ApplyContent();
		}

		internal override void ApplyBorder()
		{
			if (NativeView is XF.Button button)
			{
				button.BorderColor = BorderBrush?.ToFormsColor() ?? XF.Color.Default;
				button.BorderWidth = BorderBrush == null ? -1 : BorderThickness.Left;
				if (IsSet(PaddingProperty))
					button.Padding = NativeText.ToForms(Padding);
			}
			else
			{
				base.ApplyBorder();
			}
		}

		internal override void ApplyText()
		{
			var view = TextView;
			if (view == null)
			{
				base.ApplyText();
				return;
			}

			NativeText.ApplyFont(view, this);
			NativeText.ApplyForeground(view, this);
		}
	}

	/// <summary>A button that stays pressed: <see cref="IsChecked"/> is true, false or (three-state) null.</summary>
	public class ToggleButton : ButtonBase
	{
		/// <summary>Set while this control pushes <see cref="IsChecked"/> to its view, so the view's change is not a click.</summary>
		internal bool Syncing;

		public static readonly XF.BindableProperty IsCheckedProperty = Dp.Register<ToggleButton>(nameof(IsChecked), typeof(bool?), (bool?)false, mode: XF.BindingMode.TwoWay);
		public static readonly XF.BindableProperty IsThreeStateProperty = Dp.Register<ToggleButton>(nameof(IsThreeState), typeof(bool), false);

		public static readonly RoutedEvent CheckedEvent =
			EventManager.RegisterRoutedEvent("Checked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToggleButton));

		public static readonly RoutedEvent UncheckedEvent =
			EventManager.RegisterRoutedEvent("Unchecked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToggleButton));

		public static readonly RoutedEvent IndeterminateEvent =
			EventManager.RegisterRoutedEvent("Indeterminate", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToggleButton));

		public bool? IsChecked
		{
			get => Get<bool?>(IsCheckedProperty);
			set => SetValue(IsCheckedProperty, value);
		}

		public bool IsThreeState
		{
			get => Get<bool>(IsThreeStateProperty);
			set => SetValue(IsThreeStateProperty, value);
		}

		public event RoutedEventHandler Checked { add => AddHandler(CheckedEvent, value); remove => RemoveHandler(CheckedEvent, value); }

		public event RoutedEventHandler Unchecked { add => AddHandler(UncheckedEvent, value); remove => RemoveHandler(UncheckedEvent, value); }

		public event RoutedEventHandler Indeterminate { add => AddHandler(IndeterminateEvent, value); remove => RemoveHandler(IndeterminateEvent, value); }

		protected virtual void OnChecked(RoutedEventArgs e) => RaiseEvent(e);

		protected virtual void OnUnchecked(RoutedEventArgs e) => RaiseEvent(e);

		protected virtual void OnIndeterminate(RoutedEventArgs e) => RaiseEvent(e);

		/// <summary>WPF: a click toggles first (false, true, then null when three-state), then raises Click.</summary>
		protected override void OnClick()
		{
			OnToggle();
			base.OnClick();
		}

		protected internal virtual void OnToggle()
		{
			var current = IsChecked;
			IsChecked = current == true ? (IsThreeState ? (bool?)null : false) : current == null ? false : (bool?)true;
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property.Bindable != IsCheckedProperty)
				return;

			if (HasNativeView)
				ApplyChecked();

			var value = (bool?)e.NewValue;
			if (value == true)
				OnChecked(new RoutedEventArgs(CheckedEvent, this));
			else if (value == false)
				OnUnchecked(new RoutedEventArgs(UncheckedEvent, this));
			else
				OnIndeterminate(new RoutedEventArgs(IndeterminateEvent, this));
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplyChecked();
		}

		/// <summary>A plain toggle button shows its state by looking pressed.</summary>
		internal virtual void ApplyChecked()
		{
			if (NativeView is XF.Button button)
				button.BackgroundColor = IsChecked == true ? SystemColors.ControlDarkBrush.ToFormsColor() : Background?.ToFormsColor() ?? XF.Color.Default;
		}
	}

	public class RepeatButton : ButtonBase
	{
		DispatcherTimer _timer;

		public static readonly XF.BindableProperty DelayProperty = Dp.Register<RepeatButton>(nameof(Delay), typeof(int), 500);
		public static readonly XF.BindableProperty IntervalProperty = Dp.Register<RepeatButton>(nameof(Interval), typeof(int), 33);

		public RepeatButton() => SetValue(ClickModeProperty, ClickMode.Press);

		public int Delay
		{
			get => Get<int>(DelayProperty);
			set => SetValue(DelayProperty, value);
		}

		public int Interval
		{
			get => Get<int>(IntervalProperty);
			set => SetValue(IntervalProperty, value);
		}

		/// <summary>A click on press, then one per <see cref="Interval"/> after <see cref="Delay"/> for as long as it is held.</summary>
		internal override XF.View CreateNativeView()
		{
			var button = new XF.Button { Padding = new XF.Thickness(2, 0) };
			button.Pressed += (s, e) =>
			{
				IsPressed = true;
				PerformClick();
				_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(1, Delay)) };
				_timer.Tick += (t, a) =>
				{
					_timer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, Interval));
					PerformClick();
				};
				_timer.Start();
			};
			button.Released += (s, e) =>
			{
				IsPressed = false;
				_timer?.Stop();
				_timer = null;
			};
			return button;
		}
	}

	internal static class CommandHelpers
	{
		internal static void Execute(ICommand command, object parameter, IInputElement target)
		{
			if (command is RoutedCommand routed)
			{
				if (routed.CanExecute(parameter, target))
					routed.Execute(parameter, target);
			}
			else if (command != null && command.CanExecute(parameter))
			{
				command.Execute(parameter);
			}
		}

		internal static bool CanExecute(ICommand command, object parameter, IInputElement target) =>
			command is RoutedCommand routed ? routed.CanExecute(parameter, target) : command == null || command.CanExecute(parameter);
	}
}

namespace System.Windows.Input
{
	public interface ICommandSource
	{
		ICommand Command { get; }

		object CommandParameter { get; }

		IInputElement CommandTarget { get; }
	}
}

namespace System.Windows.Controls
{
	/// <summary>A push button; Enter clicks the default one and Escape the cancel one (see <see cref="Window"/>).</summary>
	public class Button : ButtonBase
	{
		public static readonly XF.BindableProperty IsDefaultProperty = Dp.Register<Button>(nameof(IsDefault), typeof(bool), false);
		public static readonly XF.BindableProperty IsCancelProperty = Dp.Register<Button>(nameof(IsCancel), typeof(bool), false);

		public bool IsDefault
		{
			get => Get<bool>(IsDefaultProperty);
			set => SetValue(IsDefaultProperty, value);
		}

		public bool IsCancel
		{
			get => Get<bool>(IsCancelProperty);
			set => SetValue(IsCancelProperty, value);
		}

		public bool IsDefaulted => IsDefault && !(Keyboard.FocusedElement is ButtonBase);

		/// <summary>WPF: the cancel button of a dialog closes it (with a false result) after its handlers run.</summary>
		protected override void OnClick()
		{
			base.OnClick();
			if (IsCancel && Window.GetWindow(this) is Window window && window.IsDialog)
				window.DialogResult = false;
		}
	}

	/// <summary>A check box: the box and its caption, which toggles it too.</summary>
	public class CheckBox : ToggleButton
	{
		XF.CheckBox _box;
		XF.Label _caption;

		public CheckBox() => SetValue(HorizontalContentAlignmentProperty, HorizontalAlignment.Left);

		internal override XF.View CreateNativeView()
		{
			_box = new XF.CheckBox { VerticalOptions = XF.LayoutOptions.Center, Margin = 0 };
			_caption = new XF.Label { VerticalOptions = XF.LayoutOptions.Center, VerticalTextAlignment = XF.TextAlignment.Center };
			_box.CheckedChanged += (s, e) =>
			{
				if (!Syncing)
					PerformClick();
			};
			var tap = new XF.TapGestureRecognizer();
			tap.Tapped += (s, e) => PerformClick();
			_caption.GestureRecognizers.Add(tap);
			return new XF.StackLayout { Orientation = XF.StackOrientation.Horizontal, Spacing = 2, Children = { _box, _caption } };
		}

		internal override XF.View TextView => _caption;

		internal override void ApplyContent()
		{
			if (_caption != null)
				_caption.Text = ContentText;
		}

		internal override void ApplyBorder()
		{
		}

		internal override void ApplyText()
		{
			if (_caption == null)
				return;

			NativeText.ApplyFont(_caption, this);
			NativeText.ApplyForeground(_caption, this);
		}

		/// <summary>The box shows true as checked; false and the indeterminate null as not.</summary>
		internal override void ApplyChecked()
		{
			if (_box == null)
				return;

			Syncing = true;
			try
			{
				_box.IsChecked = IsChecked == true;
				_box.Opacity = IsChecked == null ? 0.5 : 1;
			}
			finally
			{
				Syncing = false;
			}
		}
	}

	/// <summary>An option button: checking one unchecks the others of its group - its parent's, or its <see cref="GroupName"/>.</summary>
	public class RadioButton : ToggleButton
	{
		public static readonly XF.BindableProperty GroupNameProperty = Dp.Register<RadioButton>(nameof(GroupName), typeof(string), string.Empty);

		public string GroupName
		{
			get => Get<string>(GroupNameProperty);
			set => SetValue(GroupNameProperty, value);
		}

		/// <summary>A radio button only checks: clicking a checked one leaves it checked.</summary>
		protected internal override void OnToggle() => IsChecked = true;

		internal override XF.View CreateNativeView()
		{
			var radio = new XF.RadioButton();
			radio.CheckedChanged += (s, e) =>
			{
				if (Syncing)
					return;

				if (e.Value)
					PerformClick();
				else
					IsChecked = false;
			};
			return radio;
		}

		internal override XF.View TextView => NativeView as XF.RadioButton;

		internal override void ApplyContent()
		{
			if (NativeView is XF.RadioButton radio)
				radio.Content = ContentText;
		}

		internal override void ApplyBorder()
		{
		}

		internal override void ApplyChecked()
		{
			if (!(NativeView is XF.RadioButton radio))
				return;

			Syncing = true;
			try
			{
				radio.IsChecked = IsChecked == true;
			}
			finally
			{
				Syncing = false;
			}
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			((XF.RadioButton)NativeView).GroupName = string.IsNullOrEmpty(GroupName) ? null : GroupName;
		}

		/// <summary>Checking one unchecks the rest of its group, here as in WPF, whatever the view does.</summary>
		protected override void OnChecked(RoutedEventArgs e)
		{
			foreach (var other in Group())
				other.IsChecked = false;

			base.OnChecked(e);
		}

		System.Collections.Generic.IEnumerable<RadioButton> Group()
		{
			var name = GroupName;
			if (!string.IsNullOrEmpty(name))
			{
				var root = (DependencyObject)Window.GetWindow(this) ?? this;
				foreach (var radio in Descendants(root))
				{
					if (radio != this && radio.GroupName == name && radio.IsChecked == true)
						yield return radio;
				}

				yield break;
			}

			if (LogicalParent == null)
				yield break;

			foreach (var child in LogicalParent.LogicalChildrenCore)
			{
				if (child is RadioButton radio && radio != this && string.IsNullOrEmpty(radio.GroupName) && radio.IsChecked == true)
					yield return radio;
			}
		}

		static System.Collections.Generic.IEnumerable<RadioButton> Descendants(DependencyObject root)
		{
			foreach (var child in root.LogicalChildrenCore)
			{
				if (child is RadioButton radio)
					yield return radio;
				if (child is DependencyObject d)
				{
					foreach (var nested in Descendants(d))
						yield return nested;
				}
			}
		}
	}
}
