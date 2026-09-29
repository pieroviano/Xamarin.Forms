using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows.Controls.Primitives
{
	public abstract class MenuBase : ItemsControl
	{
	}
}

namespace System.Windows.Controls
{
	/// <summary>
	/// A menu bar: its top-level items side by side; clicking one opens its items as a GTK popover menu (which draws
	/// separators, check marks and shortcut texts the platform's way).
	/// </summary>
	public class Menu : MenuBase
	{
		public Menu()
		{
			SetValue(FocusableProperty, false);
			SetValue(IsTabStopProperty, false);
			SetValue(BackgroundProperty, SystemColors.MenuBarBrush);
		}

		internal override XF.View CreateNativeView() =>
			new NativeShell(new XF.StackLayout { Orientation = XF.StackOrientation.Horizontal, Spacing = 0 });

		internal override void ApplyItems(NotifyCollectionChangedEventArgs e)
		{
			if (!(((NativeShell)NativeView).Inner is XF.StackLayout bar))
				return;

			bar.Children.Clear();
			foreach (var item in Items)
			{
				if (item is UIElement element)
					bar.Children.Add(element.NativeView);
			}
		}
	}

	/// <summary>
	/// A menu item. In a menu bar it is a caption that opens its items; in a popover it is an entry, whose click runs
	/// <see cref="Click"/> (checking or unchecking a checkable item first) and its command.
	/// </summary>
	public class MenuItem : HeaderedItemsControl, ICommandSource
	{
		public static readonly RoutedEvent ClickEvent =
			EventManager.RegisterRoutedEvent("Click", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuItem));

		public static readonly RoutedEvent CheckedEvent =
			EventManager.RegisterRoutedEvent("Checked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuItem));

		public static readonly RoutedEvent UncheckedEvent =
			EventManager.RegisterRoutedEvent("Unchecked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuItem));

		public static readonly RoutedEvent SubmenuOpenedEvent =
			EventManager.RegisterRoutedEvent("SubmenuOpened", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuItem));

		public static readonly RoutedEvent SubmenuClosedEvent =
			EventManager.RegisterRoutedEvent("SubmenuClosed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuItem));

		public static readonly XF.BindableProperty CommandProperty = Dp.Register<MenuItem>(nameof(Command), typeof(ICommand), null);
		public static readonly XF.BindableProperty CommandParameterProperty = Dp.Register<MenuItem>(nameof(CommandParameter), typeof(object), null);
		public static readonly XF.BindableProperty CommandTargetProperty = Dp.Register<MenuItem>(nameof(CommandTarget), typeof(IInputElement), null);
		public static readonly XF.BindableProperty IsCheckableProperty = Dp.Register<MenuItem>(nameof(IsCheckable), typeof(bool), false);
		public static readonly XF.BindableProperty IsCheckedProperty = Dp.Register<MenuItem>(nameof(IsChecked), typeof(bool), false, mode: XF.BindingMode.TwoWay);
		public static readonly XF.BindableProperty InputGestureTextProperty = Dp.Register<MenuItem>(nameof(InputGestureText), typeof(string), string.Empty);
		public static readonly XF.BindableProperty IconProperty = Dp.Register<MenuItem>(nameof(Icon), typeof(object), null);
		public static readonly XF.BindableProperty IsSubmenuOpenProperty = Dp.Register<MenuItem>(nameof(IsSubmenuOpen), typeof(bool), false);
		public static readonly XF.BindableProperty StaysOpenOnClickProperty = Dp.Register<MenuItem>(nameof(StaysOpenOnClick), typeof(bool), false);

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

		public bool IsCheckable
		{
			get => Get<bool>(IsCheckableProperty);
			set => SetValue(IsCheckableProperty, value);
		}

		public bool IsChecked
		{
			get => Get<bool>(IsCheckedProperty);
			set => SetValue(IsCheckedProperty, value);
		}

		public string InputGestureText
		{
			get => Get<string>(InputGestureTextProperty);
			set => SetValue(InputGestureTextProperty, value);
		}

		public object Icon
		{
			get => Get<object>(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public bool IsSubmenuOpen
		{
			get => Get<bool>(IsSubmenuOpenProperty);
			set => SetValue(IsSubmenuOpenProperty, value);
		}

		public bool StaysOpenOnClick
		{
			get => Get<bool>(StaysOpenOnClickProperty);
			set => SetValue(StaysOpenOnClickProperty, value);
		}

		public bool IsHighlighted => false;

		public bool IsPressed => false;

		public MenuItemRole Role =>
			LogicalParent is Menu ? (HasItems ? MenuItemRole.TopLevelHeader : MenuItemRole.TopLevelItem) : HasItems ? MenuItemRole.SubmenuHeader : MenuItemRole.SubmenuItem;

		public event RoutedEventHandler Click { add => AddHandler(ClickEvent, value); remove => RemoveHandler(ClickEvent, value); }

		public event RoutedEventHandler Checked { add => AddHandler(CheckedEvent, value); remove => RemoveHandler(CheckedEvent, value); }

		public event RoutedEventHandler Unchecked { add => AddHandler(UncheckedEvent, value); remove => RemoveHandler(UncheckedEvent, value); }

		public event RoutedEventHandler SubmenuOpened { add => AddHandler(SubmenuOpenedEvent, value); remove => RemoveHandler(SubmenuOpenedEvent, value); }

		public event RoutedEventHandler SubmenuClosed { add => AddHandler(SubmenuClosedEvent, value); remove => RemoveHandler(SubmenuClosedEvent, value); }

		protected virtual void OnClick()
		{
			if (IsCheckable)
				IsChecked = !IsChecked;

			RaiseEvent(new RoutedEventArgs(ClickEvent, this));
			CommandHelpers.Execute(Command, CommandParameter, CommandTarget ?? this);
		}

		protected virtual void OnChecked(RoutedEventArgs e) => RaiseEvent(e);

		protected virtual void OnUnchecked(RoutedEventArgs e) => RaiseEvent(e);

		protected virtual void OnSubmenuOpened(RoutedEventArgs e) => RaiseEvent(e);

		protected virtual void OnSubmenuClosed(RoutedEventArgs e) => RaiseEvent(e);

		internal void PerformClick()
		{
			if (IsEnabled)
				OnClick();
		}

		protected override bool IsEnabledCore => Command == null || CommandHelpers.CanExecute(Command, CommandParameter, CommandTarget ?? this);

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (p == IsCheckedProperty)
			{
				if (IsChecked)
					OnChecked(new RoutedEventArgs(CheckedEvent, this));
				else
					OnUnchecked(new RoutedEventArgs(UncheckedEvent, this));
			}
			else if (p == IsSubmenuOpenProperty)
			{
				if (IsSubmenuOpen)
				{
					OnSubmenuOpened(new RoutedEventArgs(SubmenuOpenedEvent, this));
					if (!_openingSubmenu)
						OpenSubmenu();
				}
				else
				{
					OnSubmenuClosed(new RoutedEventArgs(SubmenuClosedEvent, this));
				}
			}
			else if (p == CommandProperty)
			{
				if (e.OldValue is ICommand old)
					old.CanExecuteChanged -= OnCanExecuteChanged;
				if (e.NewValue is ICommand command)
					command.CanExecuteChanged += OnCanExecuteChanged;
				CoerceIsEnabled();
			}
		}

		void OnCanExecuteChanged(object sender, EventArgs e) => CoerceIsEnabled();

		internal override void OnHeaderChanged()
		{
			if (HasNativeView && NativeView is XF.Label label)
				label.Text = HeaderText;
		}

		// ---- a top-level item: a caption in the menu bar ---------------------------------------------------------

		bool _openingSubmenu;

		internal override XF.View CreateNativeView()
		{
			var label = new XF.Label { Padding = new XF.Thickness(7, 3), VerticalTextAlignment = XF.TextAlignment.Center };
			var tap = new XF.TapGestureRecognizer();
			tap.Tapped += (s, e) =>
			{
				if (HasItems)
					OpenSubmenu();
				else
					PerformClick();
			};
			label.GestureRecognizers.Add(tap);
			return label;
		}

		internal override XF.View TextView => NativeView as XF.Label;

		internal override void SyncNative()
		{
			base.SyncNative();
			if (NativeView is XF.Label label)
				label.Text = HeaderText;
		}

		internal override void ApplyItems(NotifyCollectionChangedEventArgs e)
		{
		}

		internal override void ApplyBorder()
		{
		}

		/// <summary>Opens this item's items as a popover under its caption.</summary>
		internal void OpenSubmenu()
		{
			var anchor = NativeInput.WidgetOf(this);
			if (anchor == null || !HasItems)
				return;

			_openingSubmenu = true;
			IsSubmenuOpen = true;
			_openingSubmenu = false;
			MenuPopover.Show(Items, anchor, null, () => IsSubmenuOpen = false);
		}
	}

	public enum MenuItemRole
	{
		TopLevelItem,
		TopLevelHeader,
		SubmenuItem,
		SubmenuHeader,
	}

	public class Separator : Control
	{
		public Separator()
		{
			SetValue(FocusableProperty, false);
			SetValue(IsTabStopProperty, false);
		}

		internal override XF.View CreateNativeView() =>
			new XF.BoxView { Color = SystemColors.ControlDarkBrush.ToFormsColor(), WidthRequest = 1, HeightRequest = 1, Margin = new XF.Thickness(2) };

		internal override void ApplyBackground()
		{
		}
	}

	/// <summary>A pop-up menu: opened at the pointer (or its target) when <see cref="IsOpen"/> becomes true.</summary>
	public class ContextMenu : MenuBase
	{
		public static readonly XF.BindableProperty IsOpenProperty = Dp.Register<ContextMenu>(nameof(IsOpen), typeof(bool), false);
		public static readonly XF.BindableProperty PlacementTargetProperty = Dp.Register<ContextMenu>(nameof(PlacementTarget), typeof(UIElement), null);
		public static readonly XF.BindableProperty PlacementProperty = Dp.Register<ContextMenu>(nameof(Placement), typeof(PlacementMode), PlacementMode.MousePoint);
		public static readonly XF.BindableProperty HorizontalOffsetProperty = Dp.Register<ContextMenu>(nameof(HorizontalOffset), typeof(double), 0.0);
		public static readonly XF.BindableProperty VerticalOffsetProperty = Dp.Register<ContextMenu>(nameof(VerticalOffset), typeof(double), 0.0);
		public static readonly XF.BindableProperty StaysOpenProperty = Dp.Register<ContextMenu>(nameof(StaysOpen), typeof(bool), true);

		public static readonly RoutedEvent OpenedEvent =
			EventManager.RegisterRoutedEvent("Opened", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ContextMenu));

		public static readonly RoutedEvent ClosedEvent =
			EventManager.RegisterRoutedEvent("Closed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ContextMenu));

		bool _showing;

		public bool IsOpen
		{
			get => Get<bool>(IsOpenProperty);
			set => SetValue(IsOpenProperty, value);
		}

		public UIElement PlacementTarget
		{
			get => Get<UIElement>(PlacementTargetProperty);
			set => SetValue(PlacementTargetProperty, value);
		}

		public PlacementMode Placement
		{
			get => Get<PlacementMode>(PlacementProperty);
			set => SetValue(PlacementProperty, value);
		}

		public double HorizontalOffset
		{
			get => Get<double>(HorizontalOffsetProperty);
			set => SetValue(HorizontalOffsetProperty, value);
		}

		public double VerticalOffset
		{
			get => Get<double>(VerticalOffsetProperty);
			set => SetValue(VerticalOffsetProperty, value);
		}

		public bool StaysOpen
		{
			get => Get<bool>(StaysOpenProperty);
			set => SetValue(StaysOpenProperty, value);
		}

		public event RoutedEventHandler Opened { add => AddHandler(OpenedEvent, value); remove => RemoveHandler(OpenedEvent, value); }

		public event RoutedEventHandler Closed { add => AddHandler(ClosedEvent, value); remove => RemoveHandler(ClosedEvent, value); }

		protected virtual void OnOpened(RoutedEventArgs e) => RaiseEvent(e);

		protected virtual void OnClosed(RoutedEventArgs e) => RaiseEvent(e);

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property.Bindable != IsOpenProperty)
				return;

			if (IsOpen && !_showing)
				Show();
		}

		void Show()
		{
			var target = PlacementTarget;
			var anchor = NativeInput.WidgetOf(target) ?? (target == null ? null : NativeInput.WidgetOf(Window.GetWindow(target)));
			if (anchor == null)
			{
				IsOpen = false;
				return;
			}

			Gdk.Rectangle? at = null;
			if (Placement == PlacementMode.MousePoint || Placement == PlacementMode.Mouse)
			{
				var p = Mouse.GetPosition(target);
				at = new Gdk.Rectangle((int)(p.X + HorizontalOffset), (int)(p.Y + VerticalOffset), 1, 1);
			}

			_showing = true;
			OnOpened(new RoutedEventArgs(OpenedEvent, this));
			MenuPopover.Show(Items, anchor, at, () =>
			{
				_showing = false;
				IsOpen = false;
				OnClosed(new RoutedEventArgs(ClosedEvent, this));
			});
		}

		/// <summary>The right button was released over <paramref name="target"/>: opens the context menu nearest to it, if any.</summary>
		internal static bool OpenFor(UIElement target)
		{
			for (DependencyObject d = target; d != null; d = d.LogicalParent)
			{
				if (d is FrameworkElement fe && fe.ContextMenu is ContextMenu menu)
				{
					menu.PlacementTarget = fe;
					menu.Placement = PlacementMode.MousePoint;
					menu.IsOpen = true;
					return true;
				}
			}

			return false;
		}
	}
}

namespace Xamarin.Forms.Wpf
{
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using WpfMenuItem = System.Windows.Controls.MenuItem;

	/// <summary>WPF menu items as a GTK popover menu: a GIO menu model, and an action per item that clicks it.</summary>
	internal static class MenuPopover
	{
		/// <summary>Popovers that have closed, kept so their wrappers are never finalized after the widget is gone.</summary>
		static readonly List<Gtk.Popover> s_closed = new List<Gtk.Popover>();

		internal static void Show(IEnumerable items, Gtk.Widget anchor, Gdk.Rectangle? pointingTo, Action closed)
		{
			var group = new GLib.SimpleActionGroup();
			var counter = 0;
			var model = Build(items, group, ref counter);
			anchor.InsertActionGroup("wpf", group);

			var popover = new Gtk.PopoverMenu(model) { HasArrow = false, Position = Gtk.PositionType.Bottom };
			if (pointingTo.HasValue)
				popover.PointingTo = pointingTo.Value;
			popover.Parent = anchor;
			popover.Closed += (s, e) =>
			{
				closed?.Invoke();

				// Not from inside the popover's own signal: unparent it once GTK is done with it.
				GLib.Idle.Add(() =>
				{
					popover.Unparent();
					s_closed.Add(popover);
					return false;
				});
			};
			popover.Popup();
		}

		/// <summary>Sections split at separators; an item with items is a submenu; hidden items are left out.</summary>
		static GLib.Menu Build(IEnumerable items, GLib.SimpleActionGroup group, ref int counter)
		{
			var menu = new GLib.Menu();
			var section = new GLib.Menu();
			var sectionHasItems = false;

			foreach (var item in items)
			{
				switch (item)
				{
					case Separator separator when separator.Visibility == Visibility.Visible:
						if (sectionHasItems)
						{
							menu.AppendSection(null, section);
							section = new GLib.Menu();
							sectionHasItems = false;
						}

						break;
					case WpfMenuItem mi when mi.Visibility == Visibility.Visible:
						var label = mi.Header as string ?? mi.HeaderText;
						if (mi.Items.Cast<object>().Any(i => !(i is UIElement u) || u.Visibility == Visibility.Visible))
						{
							section.AppendSubmenu(label, Build(mi.Items, group, ref counter));
						}
						else
						{
							var name = "i" + counter++;
							var action = mi.IsCheckable ? new GLib.SimpleAction(name, null, new GLib.Variant(mi.IsChecked)) : new GLib.SimpleAction(name, null);
							action.Enabled = mi.IsEnabled;
							var target = mi;
							action.Activated += (o, a) => target.PerformClick();
							group.AddAction(action);

							var entry = new GLib.MenuItem(label, "wpf." + name);
							var accel = Accelerator(mi.InputGestureText);
							if (accel != null)
								entry.SetAttributeValue("accel", new GLib.Variant(accel));
							section.AppendItem(entry);
						}

						sectionHasItems = true;
						break;
					case string text:
						section.Append(text, null);
						sectionHasItems = true;
						break;
				}
			}

			if (sectionHasItems)
				menu.AppendSection(null, section);
			return menu;
		}

		/// <summary>WPF's gesture text ("Ctrl+Shift+S") as a GTK accelerator ("&lt;Control&gt;&lt;Shift&gt;s"), for display.</summary>
		internal static string Accelerator(string gesture)
		{
			if (string.IsNullOrWhiteSpace(gesture))
				return null;

			var parts = gesture.Split('+').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
			if (parts.Length == 0)
				return null;

			var sb = new System.Text.StringBuilder();
			for (var i = 0; i < parts.Length - 1; i++)
			{
				switch (parts[i].ToLowerInvariant())
				{
					case "ctrl":
					case "control":
						sb.Append("<Control>");
						break;
					case "shift":
						sb.Append("<Shift>");
						break;
					case "alt":
						sb.Append("<Alt>");
						break;
					case "win":
					case "windows":
						sb.Append("<Super>");
						break;
				}
			}

			var key = parts[parts.Length - 1];
			switch (key.ToLowerInvariant())
			{
				case "del":
				case "delete":
					key = "Delete";
					break;
				case "ins":
				case "insert":
					key = "Insert";
					break;
				case "back":
				case "backspace":
					key = "BackSpace";
					break;
				case "enter":
				case "return":
					key = "Return";
					break;
				case "esc":
				case "escape":
					key = "Escape";
					break;
				case "space":
					key = "space";
					break;
				case "pageup":
				case "prior":
					key = "Page_Up";
					break;
				case "pagedown":
				case "next":
					key = "Page_Down";
					break;
				default:
					if (key.Length == 1)
						key = key.ToLowerInvariant();
					break;
			}

			return sb.Append(key).ToString();
		}
	}
}
