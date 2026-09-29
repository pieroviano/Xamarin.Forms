using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows.Controls.Primitives
{
	/// <summary>A value between a minimum and a maximum; <see cref="ValueChanged"/> is raised for every change.</summary>
	public abstract class RangeBase : Control
	{
		public static readonly RoutedEvent ValueChangedEvent =
			EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<double>), typeof(RangeBase));

		public static readonly XF.BindableProperty MinimumProperty = Dp.Register<RangeBase>(nameof(Minimum), typeof(double), 0.0);
		public static readonly XF.BindableProperty MaximumProperty = Dp.Register<RangeBase>(nameof(Maximum), typeof(double), 1.0);
		public static readonly XF.BindableProperty ValueProperty = Dp.Register<RangeBase>(nameof(Value), typeof(double), 0.0, mode: XF.BindingMode.TwoWay);
		public static readonly XF.BindableProperty SmallChangeProperty = Dp.Register<RangeBase>(nameof(SmallChange), typeof(double), 0.1);
		public static readonly XF.BindableProperty LargeChangeProperty = Dp.Register<RangeBase>(nameof(LargeChange), typeof(double), 1.0);

		/// <summary>Set while this control pushes its range to its view, so the view's echo is not a user change.</summary>
		internal bool Syncing;

		public double Minimum
		{
			get => Get<double>(MinimumProperty);
			set => SetValue(MinimumProperty, value);
		}

		/// <summary>The maximum, never below <see cref="Minimum"/> (WPF coerces it the same way).</summary>
		public double Maximum
		{
			get => Math.Max(Minimum, Get<double>(MaximumProperty));
			set => SetValue(MaximumProperty, value);
		}

		/// <summary>The value, kept within the range.</summary>
		public double Value
		{
			get => Math.Max(Minimum, Math.Min(Maximum, Get<double>(ValueProperty)));
			set => SetValue(ValueProperty, value);
		}

		public double SmallChange
		{
			get => Get<double>(SmallChangeProperty);
			set => SetValue(SmallChangeProperty, value);
		}

		public double LargeChange
		{
			get => Get<double>(LargeChangeProperty);
			set => SetValue(LargeChangeProperty, value);
		}

		public event RoutedPropertyChangedEventHandler<double> ValueChanged { add => AddHandler(ValueChangedEvent, value); remove => RemoveHandler(ValueChangedEvent, value); }

		protected virtual void OnValueChanged(double oldValue, double newValue) =>
			RaiseEvent(new RoutedPropertyChangedEventArgs<double>(oldValue, newValue, ValueChangedEvent));

		protected virtual void OnMinimumChanged(double oldMinimum, double newMinimum)
		{
		}

		protected virtual void OnMaximumChanged(double oldMaximum, double newMaximum)
		{
		}

		double _lastValue;

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (p != MinimumProperty && p != MaximumProperty && p != ValueProperty && p != SmallChangeProperty && p != LargeChangeProperty)
				return;

			if (p == MinimumProperty)
				OnMinimumChanged(e.OldValue is double o ? o : 0, Minimum);
			else if (p == MaximumProperty)
				OnMaximumChanged(e.OldValue is double o ? o : 0, Maximum);

			if (HasNativeView)
				ApplyRange();

			// The value moves when it changes, or when a new range clamps it.
			var value = Value;
			if (!value.Equals(_lastValue))
			{
				var old = _lastValue;
				_lastValue = value;
				OnValueChanged(old, value);
			}
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			_lastValue = Value;
			ApplyRange();
		}

		/// <summary>The range into the view: a Xamarin.Forms slider or progress bar.</summary>
		internal virtual void ApplyRange()
		{
			if (!(NativeView is XF.Slider slider))
				return;

			Syncing = true;
			try
			{
				SetSliderRange(slider, Minimum, Maximum, Value);
			}
			finally
			{
				Syncing = false;
			}

			ApplyIncrements();
		}

		/// <summary>
		/// A Xamarin.Forms slider throws when its maximum is not above its minimum, and clamps its value to whatever
		/// range it has at the moment: the three are set in the order that never passes through an invalid range.
		/// </summary>
		internal static void SetSliderRange(XF.Slider slider, double min, double max, double value)
		{
			if (max <= min)
				max = min + 1e-9;

			if (min >= slider.Maximum)
			{
				slider.Maximum = max;
				slider.Minimum = min;
			}
			else
			{
				slider.Minimum = min;
				slider.Maximum = max;
			}

			slider.Value = value;
		}

		/// <summary>Arrow keys and clicks on the trough move by SmallChange and LargeChange: GTK's own step and page.</summary>
		internal void ApplyIncrements()
		{
			foreach (var range in GtkWidgets.Descendants<Gtk.Range>(NativeInput.WidgetOf(this)))
				range.SetIncrements(Math.Max(SmallChange, 1e-9), Math.Max(LargeChange, 1e-9));
		}

		internal override void OnWidgetAttached(Gtk.Widget widget)
		{
			base.OnWidgetAttached(widget);
			ApplyIncrements();
		}
	}

	public enum ScrollEventType
	{
		EndScroll,
		First,
		LargeDecrement,
		LargeIncrement,
		Last,
		SmallDecrement,
		SmallIncrement,
		ThumbPosition,
		ThumbTrack,
	}

	public class ScrollEventArgs : RoutedEventArgs
	{
		public ScrollEventArgs(ScrollEventType scrollEventType, double newValue)
		{
			ScrollEventType = scrollEventType;
			NewValue = newValue;
		}

		public ScrollEventType ScrollEventType { get; }

		public double NewValue { get; }

		protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
		{
			if (genericHandler is ScrollEventHandler typed)
				typed(genericTarget, this);
			else
				base.InvokeEventHandler(genericHandler, genericTarget);
		}
	}

	public delegate void ScrollEventHandler(object sender, ScrollEventArgs e);

	/// <summary>A scroll bar over a Xamarin.Forms slider (a GTK scale, turned upright for a vertical one).</summary>
	public class ScrollBar : RangeBase
	{
		public static readonly RoutedEvent ScrollEvent =
			EventManager.RegisterRoutedEvent("Scroll", RoutingStrategy.Bubble, typeof(ScrollEventHandler), typeof(ScrollBar));

		public static readonly XF.BindableProperty OrientationProperty = Dp.Register<ScrollBar>(nameof(Orientation), typeof(Orientation), Orientation.Vertical);
		public static readonly XF.BindableProperty ViewportSizeProperty = Dp.Register<ScrollBar>(nameof(ViewportSize), typeof(double), 0.0);

		public ScrollBar()
		{
			SetValue(MaximumProperty, 1.0);
			SetValue(SmallChangeProperty, 0.1);
		}

		public Orientation Orientation
		{
			get => Get<Orientation>(OrientationProperty);
			set => SetValue(OrientationProperty, value);
		}

		public double ViewportSize
		{
			get => Get<double>(ViewportSizeProperty);
			set => SetValue(ViewportSizeProperty, value);
		}

		public event ScrollEventHandler Scroll { add => AddHandler(ScrollEvent, value); remove => RemoveHandler(ScrollEvent, value); }

		internal override XF.View CreateNativeView()
		{
			var slider = new XF.Slider();
			slider.ValueChanged += (s, e) =>
			{
				if (Syncing)
					return;

				Value = e.NewValue;
				RaiseEvent(new ScrollEventArgs(ScrollEventType.ThumbTrack, Value) { RoutedEvent = ScrollEvent });
			};
			return slider;
		}

		internal override void OnWidgetAttached(Gtk.Widget widget)
		{
			base.OnWidgetAttached(widget);
			ApplyOrientation();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property.Bindable == OrientationProperty)
				ApplyOrientation();
		}

		void ApplyOrientation()
		{
			foreach (var scale in GtkWidgets.Descendants<Gtk.Scale>(NativeInput.WidgetOf(this)))
			{
				scale.Orientation = Orientation == Orientation.Vertical ? Gtk.Orientation.Vertical : Gtk.Orientation.Horizontal;
				scale.DrawValue = false;
			}
		}
	}

	public enum TickPlacement
	{
		None,
		TopLeft,
		BottomRight,
		Both,
	}

	public enum PlacementMode
	{
		Absolute,
		Relative,
		Bottom,
		Center,
		Right,
		AbsolutePoint,
		RelativePoint,
		Mouse,
		MousePoint,
		Left,
		Top,
		Custom,
	}
}

namespace System.Windows.Controls
{
	/// <summary>A slider over a Xamarin.Forms slider; ticks are GTK scale marks.</summary>
	public class Slider : RangeBase
	{
		public static readonly XF.BindableProperty OrientationProperty = Dp.Register<Slider>(nameof(Orientation), typeof(Orientation), Orientation.Horizontal);
		public static readonly XF.BindableProperty TickFrequencyProperty = Dp.Register<Slider>(nameof(TickFrequency), typeof(double), 1.0);
		public static readonly XF.BindableProperty TickPlacementProperty = Dp.Register<Slider>(nameof(TickPlacement), typeof(TickPlacement), TickPlacement.None);
		public static readonly XF.BindableProperty IsSnapToTickEnabledProperty = Dp.Register<Slider>(nameof(IsSnapToTickEnabled), typeof(bool), false);
		public static readonly XF.BindableProperty IsDirectionReversedProperty = Dp.Register<Slider>(nameof(IsDirectionReversed), typeof(bool), false);

		public Slider()
		{
			SetValue(MaximumProperty, 10.0);
			SetValue(SmallChangeProperty, 0.1);
		}

		public Orientation Orientation
		{
			get => Get<Orientation>(OrientationProperty);
			set => SetValue(OrientationProperty, value);
		}

		public double TickFrequency
		{
			get => Get<double>(TickFrequencyProperty);
			set => SetValue(TickFrequencyProperty, value);
		}

		public TickPlacement TickPlacement
		{
			get => Get<TickPlacement>(TickPlacementProperty);
			set => SetValue(TickPlacementProperty, value);
		}

		public bool IsSnapToTickEnabled
		{
			get => Get<bool>(IsSnapToTickEnabledProperty);
			set => SetValue(IsSnapToTickEnabledProperty, value);
		}

		public bool IsDirectionReversed
		{
			get => Get<bool>(IsDirectionReversedProperty);
			set => SetValue(IsDirectionReversedProperty, value);
		}

		internal override XF.View CreateNativeView()
		{
			var slider = new XF.Slider();
			slider.ValueChanged += (s, e) =>
			{
				if (Syncing)
					return;

				var value = e.NewValue;
				if (IsSnapToTickEnabled && TickFrequency > 0)
					value = Minimum + Math.Round((value - Minimum) / TickFrequency) * TickFrequency;
				Value = value;
			};
			return slider;
		}

		internal override void OnWidgetAttached(Gtk.Widget widget)
		{
			base.OnWidgetAttached(widget);
			ApplyScale();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (p == OrientationProperty || p == TickFrequencyProperty || p == TickPlacementProperty || p == IsDirectionReversedProperty
				|| p == MinimumProperty || p == MaximumProperty)
				ApplyScale();
		}

		void ApplyScale()
		{
			foreach (var scale in GtkWidgets.Descendants<Gtk.Scale>(NativeInput.WidgetOf(this)))
			{
				scale.Orientation = Orientation == Orientation.Vertical ? Gtk.Orientation.Vertical : Gtk.Orientation.Horizontal;
				scale.Inverted = IsDirectionReversed ^ (Orientation == Orientation.Vertical);
				scale.ClearMarks();
				if (TickPlacement == TickPlacement.None || TickFrequency <= 0)
					continue;

				var position = TickPlacement == TickPlacement.TopLeft ? Gtk.PositionType.Top : Gtk.PositionType.Bottom;
				var count = (Maximum - Minimum) / TickFrequency;
				if (count > 1000)
					continue;

				for (var v = Minimum; v <= Maximum + 1e-9; v += TickFrequency)
					scale.AddMark(v, position, null);
			}
		}
	}

	/// <summary>A progress bar: the value as a fraction of the range, over a Xamarin.Forms progress bar.</summary>
	public class ProgressBar : RangeBase
	{
		public static readonly XF.BindableProperty IsIndeterminateProperty = Dp.Register<ProgressBar>(nameof(IsIndeterminate), typeof(bool), false);
		public static readonly XF.BindableProperty OrientationProperty = Dp.Register<ProgressBar>(nameof(Orientation), typeof(Orientation), Orientation.Horizontal);

		public ProgressBar()
		{
			SetValue(MaximumProperty, 100.0);
			SetValue(FocusableProperty, false);
			SetValue(IsTabStopProperty, false);
		}

		public bool IsIndeterminate
		{
			get => Get<bool>(IsIndeterminateProperty);
			set => SetValue(IsIndeterminateProperty, value);
		}

		public Orientation Orientation
		{
			get => Get<Orientation>(OrientationProperty);
			set => SetValue(OrientationProperty, value);
		}

		internal override XF.View CreateNativeView() => new XF.ProgressBar();

		internal override void ApplyRange()
		{
			var range = Maximum - Minimum;
			((XF.ProgressBar)NativeView).Progress = range <= 0 ? 0 : (Value - Minimum) / range;
		}

		internal override void OnWidgetAttached(Gtk.Widget widget)
		{
			base.OnWidgetAttached(widget);
			ApplyOrientation();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property.Bindable == OrientationProperty)
				ApplyOrientation();
		}

		/// <summary>A vertical bar fills from the bottom, as WPF's does.</summary>
		void ApplyOrientation()
		{
			foreach (var bar in GtkWidgets.Descendants<Gtk.ProgressBar>(NativeInput.WidgetOf(this)))
			{
				bar.Orientation = Orientation == Orientation.Vertical ? Gtk.Orientation.Vertical : Gtk.Orientation.Horizontal;
				bar.Inverted = Orientation == Orientation.Vertical;
			}
		}
	}
}
