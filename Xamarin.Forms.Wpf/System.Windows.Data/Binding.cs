using System.Globalization;
using XF = Xamarin.Forms;

namespace System.Windows
{
	/// <summary>The path of a binding: a property name, or dotted names.</summary>
	public sealed class PropertyPath
	{
		public PropertyPath(string path, params object[] pathParameters) => Path = path ?? string.Empty;

		public PropertyPath(object parameter) => Path = parameter?.ToString() ?? string.Empty;

		public string Path { get; set; }

		public override string ToString() => Path;
	}
}

namespace System.Windows.Data
{
	public enum BindingMode
	{
		TwoWay,
		OneWay,
		OneTime,
		OneWayToSource,
		Default,
	}

	public enum UpdateSourceTrigger
	{
		Default,
		PropertyChanged,
		LostFocus,
		Explicit,
	}

	public interface IValueConverter
	{
		object Convert(object value, Type targetType, object parameter, CultureInfo culture);

		object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
	}

	public abstract class BindingBase
	{
		public object FallbackValue { get; set; }

		public object TargetNullValue { get; set; }

		public string StringFormat { get; set; }

		public string BindingGroupName { get; set; }

		/// <summary>The same binding for Xamarin.Forms, for <paramref name="target"/>.</summary>
		internal abstract XF.BindingBase ToForms(FrameworkElement target);
	}

	/// <summary>A binding to a property path, of the data context or of a named element.</summary>
	public class Binding : BindingBase
	{
		public Binding()
		{
		}

		public Binding(string path) => Path = new PropertyPath(path);

		public PropertyPath Path { get; set; }

		public BindingMode Mode { get; set; } = BindingMode.Default;

		public UpdateSourceTrigger UpdateSourceTrigger { get; set; }

		public IValueConverter Converter { get; set; }

		public object ConverterParameter { get; set; }

		public CultureInfo ConverterCulture { get; set; }

		public object Source { get; set; }

		public string ElementName { get; set; }

		public string XPath { get; set; }

		public bool NotifyOnSourceUpdated { get; set; }

		public bool NotifyOnTargetUpdated { get; set; }

		public bool ValidatesOnDataErrors { get; set; }

		public bool ValidatesOnExceptions { get; set; }

		public static readonly object DoNothing = new object();

		internal override XF.BindingBase ToForms(FrameworkElement target)
		{
			var source = Source;
			if (source == null && !string.IsNullOrEmpty(ElementName))
				source = target?.FindName(ElementName);

			return new XF.Binding(
				Path?.Path ?? ".",
				ToForms(Mode),
				Converter == null ? null : new ConverterAdapter(Converter, ConverterCulture),
				ConverterParameter,
				StringFormat,
				source)
			{
				FallbackValue = FallbackValue,
				TargetNullValue = TargetNullValue,
			};
		}

		static XF.BindingMode ToForms(BindingMode mode)
		{
			switch (mode)
			{
				case BindingMode.TwoWay:
					return XF.BindingMode.TwoWay;
				case BindingMode.OneWay:
					return XF.BindingMode.OneWay;
				case BindingMode.OneTime:
					return XF.BindingMode.OneTime;
				case BindingMode.OneWayToSource:
					return XF.BindingMode.OneWayToSource;
				default:
					return XF.BindingMode.Default;
			}
		}

		sealed class ConverterAdapter : XF.IValueConverter
		{
			readonly IValueConverter _converter;
			readonly CultureInfo _culture;

			public ConverterAdapter(IValueConverter converter, CultureInfo culture)
			{
				_converter = converter;
				_culture = culture;
			}

			public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
				_converter.Convert(value, targetType, parameter, _culture ?? culture);

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
				_converter.ConvertBack(value, targetType, parameter, _culture ?? culture);
		}
	}

	/// <summary>A binding in place on a target property.</summary>
	public class BindingExpressionBase : Expression
	{
		internal BindingExpressionBase(DependencyObject target, DependencyProperty property, BindingBase binding)
		{
			Target = target;
			TargetProperty = property;
			ParentBindingBase = binding;
		}

		public DependencyObject Target { get; }

		public DependencyProperty TargetProperty { get; }

		public BindingBase ParentBindingBase { get; }

		public void UpdateTarget()
		{
		}

		public void UpdateSource()
		{
		}
	}

	public sealed class BindingExpression : BindingExpressionBase
	{
		internal BindingExpression(DependencyObject target, DependencyProperty property, BindingBase binding) : base(target, property, binding)
		{
		}

		public Binding ParentBinding => ParentBindingBase as Binding;

		public object DataItem => Target is FrameworkElement fe ? fe.DataContext : null;
	}

	public static class BindingOperations
	{
		public static BindingExpressionBase SetBinding(DependencyObject target, DependencyProperty dp, BindingBase binding)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));
			if (dp == null)
				throw new ArgumentNullException(nameof(dp));
			if (binding == null)
				throw new ArgumentNullException(nameof(binding));

			target.SetBinding(dp.Bindable, binding.ToForms(target as FrameworkElement));
			return new BindingExpression(target, dp, binding);
		}

		public static void ClearBinding(DependencyObject target, DependencyProperty dp) =>
			target.RemoveBinding(dp.Bindable);

		public static void ClearAllBindings(DependencyObject target)
		{
		}

		public static bool IsDataBound(DependencyObject target, DependencyProperty dp) => false;
	}
}

namespace System.Windows
{
	public partial class FrameworkElement
	{
		public Data.BindingExpressionBase SetBinding(DependencyProperty dp, Data.BindingBase binding) =>
			Data.BindingOperations.SetBinding(this, dp, binding);

		public Data.BindingExpression SetBinding(DependencyProperty dp, string path) =>
			(Data.BindingExpression)Data.BindingOperations.SetBinding(this, dp, new Data.Binding(path));
	}
}
