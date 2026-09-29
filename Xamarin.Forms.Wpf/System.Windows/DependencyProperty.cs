using System.Collections.Generic;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows
{
	/// <summary>
	/// A WPF dependency property. It is a view of a Xamarin.Forms <see cref="XF.BindableProperty"/>, which is where
	/// the value lives, so XAML, bindings, styles and dynamic resources work through the Xamarin.Forms machinery.
	/// </summary>
	/// <remarks>
	/// The <c>XxxProperty</c> fields of this library are declared as <see cref="XF.BindableProperty"/>, because that
	/// is the type the Xamarin.Forms XAML compiler looks for; the implicit conversion is what lets WPF code hold them
	/// as <see cref="DependencyProperty"/>. Each bindable property has exactly one wrapper, so wrappers compare by
	/// reference the way WPF's own do.
	/// </remarks>
	public sealed class DependencyProperty
	{
		static readonly Dictionary<XF.BindableProperty, DependencyProperty> s_wrappers = new Dictionary<XF.BindableProperty, DependencyProperty>();

		/// <summary>The value <see cref="DependencyObject.ReadLocalValue"/> answers for a property with no local value.</summary>
		public static readonly object UnsetValue = new NamedObject("DependencyProperty.UnsetValue");

		DependencyProperty(XF.BindableProperty bindable, PropertyMetadata metadata)
		{
			Bindable = bindable;
			DefaultMetadata = metadata ?? new PropertyMetadata(bindable.DefaultValue);
		}

		internal XF.BindableProperty Bindable { get; }

		public string Name => Bindable.PropertyName;

		public Type PropertyType => Bindable.ReturnType;

		public Type OwnerType => Bindable.DeclaringType;

		public PropertyMetadata DefaultMetadata { get; }

		public bool ReadOnly => Bindable.IsReadOnly;

		/// <summary>Whether a descendant without a value of its own takes its parent's (WPF's <c>Inherits</c> flag).</summary>
		internal bool Inherits => DefaultMetadata is FrameworkPropertyMetadata f && f.Inherits;

		public static implicit operator DependencyProperty(XF.BindableProperty property)
		{
			if (property == null)
				return null;

			lock (s_wrappers)
			{
				if (!s_wrappers.TryGetValue(property, out var wrapper))
					s_wrappers.Add(property, wrapper = new DependencyProperty(property, null));

				return wrapper;
			}
		}

		public static DependencyProperty Register(string name, Type propertyType, Type ownerType) =>
			Register(name, propertyType, ownerType, null);

		public static DependencyProperty Register(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata) =>
			Create(name, propertyType, ownerType, typeMetadata);

		public static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType) =>
			RegisterAttached(name, propertyType, ownerType, null);

		public static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType, PropertyMetadata defaultMetadata) =>
			Create(name, propertyType, ownerType, defaultMetadata);

		/// <summary>The property itself: WPF's per-owner metadata is not modelled.</summary>
		public DependencyProperty AddOwner(Type ownerType) => this;

		public DependencyProperty AddOwner(Type ownerType, PropertyMetadata typeMetadata) => this;

		public PropertyMetadata GetMetadata(Type forType) => DefaultMetadata;

		public override string ToString() => Name;

		static DependencyProperty Create(string name, Type propertyType, Type ownerType, PropertyMetadata metadata)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (propertyType == null)
				throw new ArgumentNullException(nameof(propertyType));
			if (ownerType == null)
				throw new ArgumentNullException(nameof(ownerType));

			var defaultValue = metadata?.DefaultValue;
			if (defaultValue == null && propertyType.IsValueType && Nullable.GetUnderlyingType(propertyType) == null)
				defaultValue = Activator.CreateInstance(propertyType);

			var bindable = Dp.CreateBindable(name, propertyType, ownerType, defaultValue);

			Declare(bindable, metadata ?? new PropertyMetadata(defaultValue));
			return bindable;
		}

		/// <summary>Registers the wrapper of a property this library declares, with its metadata.</summary>
		internal static void Declare(XF.BindableProperty bindable, PropertyMetadata metadata)
		{
			lock (s_wrappers)
			{
				var wrapper = new DependencyProperty(bindable, metadata);
				s_wrappers[bindable] = wrapper;
				if (wrapper.Inherits)
					s_inherited.Add(wrapper);
			}
		}

		static readonly List<DependencyProperty> s_inherited = new List<DependencyProperty>();

		/// <summary>Every property whose value descendants inherit.</summary>
		internal static DependencyProperty[] InheritedProperties
		{
			get
			{
				lock (s_wrappers)
					return s_inherited.ToArray();
			}
		}

		sealed class NamedObject
		{
			readonly string _name;

			public NamedObject(string name) => _name = name;

			public override string ToString() => "{" + _name + "}";
		}
	}

	public delegate void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e);

	public delegate object CoerceValueCallback(DependencyObject d, object baseValue);

	public delegate bool ValidateValueCallback(object value);

	public class PropertyMetadata
	{
		public PropertyMetadata()
		{
		}

		public PropertyMetadata(object defaultValue) => DefaultValue = defaultValue;

		public PropertyMetadata(PropertyChangedCallback propertyChangedCallback) => PropertyChangedCallback = propertyChangedCallback;

		public PropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback)
		{
			DefaultValue = defaultValue;
			PropertyChangedCallback = propertyChangedCallback;
		}

		public PropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback)
			: this(defaultValue, propertyChangedCallback) => CoerceValueCallback = coerceValueCallback;

		public object DefaultValue { get; set; }

		public PropertyChangedCallback PropertyChangedCallback { get; set; }

		public CoerceValueCallback CoerceValueCallback { get; set; }
	}

	[Flags]
	public enum FrameworkPropertyMetadataOptions
	{
		None = 0,
		AffectsMeasure = 1,
		AffectsArrange = 2,
		AffectsParentMeasure = 4,
		AffectsParentArrange = 8,
		AffectsRender = 16,
		Inherits = 32,
		OverridesInheritanceBehavior = 64,
		NotDataBindable = 128,
		BindsTwoWayByDefault = 256,
		Journal = 1024,
		SubPropertiesDoNotAffectRender = 2048,
	}

	public class FrameworkPropertyMetadata : PropertyMetadata
	{
		public FrameworkPropertyMetadata()
		{
		}

		public FrameworkPropertyMetadata(object defaultValue) : base(defaultValue)
		{
		}

		public FrameworkPropertyMetadata(PropertyChangedCallback propertyChangedCallback) : base(propertyChangedCallback)
		{
		}

		public FrameworkPropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback)
			: base(defaultValue, propertyChangedCallback)
		{
		}

		public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions flags) : base(defaultValue) => Apply(flags);

		public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions flags, PropertyChangedCallback propertyChangedCallback)
			: base(defaultValue, propertyChangedCallback) => Apply(flags);

		public bool Inherits { get; set; }

		public bool AffectsMeasure { get; set; }

		public bool AffectsArrange { get; set; }

		public bool AffectsRender { get; set; }

		public bool BindsTwoWayByDefault { get; set; }

		void Apply(FrameworkPropertyMetadataOptions flags)
		{
			Inherits = (flags & FrameworkPropertyMetadataOptions.Inherits) != 0;
			AffectsMeasure = (flags & FrameworkPropertyMetadataOptions.AffectsMeasure) != 0;
			AffectsArrange = (flags & FrameworkPropertyMetadataOptions.AffectsArrange) != 0;
			AffectsRender = (flags & FrameworkPropertyMetadataOptions.AffectsRender) != 0;
			BindsTwoWayByDefault = (flags & FrameworkPropertyMetadataOptions.BindsTwoWayByDefault) != 0;
		}
	}

	public readonly struct DependencyPropertyChangedEventArgs
	{
		public DependencyPropertyChangedEventArgs(DependencyProperty property, object oldValue, object newValue)
		{
			Property = property;
			OldValue = oldValue;
			NewValue = newValue;
		}

		public DependencyProperty Property { get; }

		public object OldValue { get; }

		public object NewValue { get; }
	}

	public delegate void DependencyPropertyChangedEventHandler(object sender, DependencyPropertyChangedEventArgs e);

	/// <summary>A value computed from other values: WPF's base of bindings and resource references.</summary>
	public class Expression
	{
		internal Expression()
		{
		}
	}
}
