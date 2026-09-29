using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XF = Xamarin.Forms;

namespace System.Windows
{
	/// <summary>An object that can be made immutable, and so shared: brushes, images.</summary>
	public abstract class Freezable : DependencyObject
	{
		public bool IsFrozen { get; private set; }

		public bool CanFreeze => true;

		public event EventHandler Changed;

		public void Freeze() => IsFrozen = true;

		public Freezable Clone()
		{
			var clone = CreateInstanceCore();
			clone.IsFrozen = false;
			return clone;
		}

		public Freezable CloneCurrentValue() => Clone();

		public Freezable GetAsFrozen()
		{
			if (IsFrozen)
				return this;

			var frozen = Clone();
			frozen.Freeze();
			return frozen;
		}

		protected abstract Freezable CreateInstanceCore();

		protected virtual void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			OnChanged();
		}

		internal void ThrowIfFrozen()
		{
			if (IsFrozen)
				throw new InvalidOperationException($"Cannot set a property on object '{this}' because it is in a read-only state.");
		}
	}

	/// <summary>WPF's base of the non-visual elements of a document (<c>Run</c>, <c>Paragraph</c>).</summary>
	public class ContentElement : DependencyObject
	{
	}

	public class FrameworkContentElement : ContentElement
	{
		public string Name
		{
			get => StyleId;
			set => StyleId = value;
		}

		public object Tag { get; set; }

		public object DataContext
		{
			get => BindingContext;
			set => BindingContext = value;
		}

		public new DependencyObject Parent => LogicalParent;
	}

	/// <summary>The key of a resource WPF itself defines, such as a system color.</summary>
	public abstract class ResourceKey
	{
		/// <summary>
		/// The key as Xamarin.Forms resources hold it. A XAML <c>{DynamicResource {x:Static SystemColors.XBrushKey}}</c>
		/// hands a key to a Xamarin.Forms markup extension that takes a string; this conversion is what bridges them.
		/// </summary>
		public static implicit operator string(ResourceKey key) => key?.ToString();
	}

	internal sealed class SystemResourceKey : ResourceKey
	{
		readonly string _name;

		internal SystemResourceKey(string name) => _name = name;

		public override string ToString() => _name;
	}

	public class ResourceDictionary : IDictionary, IEnumerable<KeyValuePair<object, object>>
	{
		readonly Dictionary<object, object> _values = new Dictionary<object, object>();

		public IList<ResourceDictionary> MergedDictionaries { get; } = new List<ResourceDictionary>();

		public Uri Source { get; set; }

		public object this[object key]
		{
			get => TryGet(key, out var value) ? value : null;
			set => _values[key] = value;
		}

		public ICollection Keys => _values.Keys;

		public ICollection Values => _values.Values;

		public int Count => _values.Count;

		public bool IsReadOnly => false;

		public bool IsFixedSize => false;

		public bool IsSynchronized => false;

		public object SyncRoot => this;

		public void Add(object key, object value) => _values.Add(key, value);

		public void Clear() => _values.Clear();

		public bool Contains(object key) => TryGet(key, out _);

		public void Remove(object key) => _values.Remove(key);

		public void CopyTo(Array array, int index) => ((ICollection)_values).CopyTo(array, index);

		internal bool TryGet(object key, out object value)
		{
			if (key != null && _values.TryGetValue(key, out value))
				return true;

			for (var i = MergedDictionaries.Count - 1; i >= 0; i--)
			{
				if (MergedDictionaries[i].TryGet(key, out value))
					return true;
			}

			value = null;
			return false;
		}

		public IDictionaryEnumerator GetEnumerator() => _values.GetEnumerator();

		IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator() => _values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
	}

	public abstract class SetterBase
	{
		public bool IsSealed { get; internal set; }
	}

	public class Setter : SetterBase
	{
		public Setter()
		{
		}

		public Setter(DependencyProperty property, object value)
		{
			Property = property ?? throw new ArgumentNullException(nameof(property));
			Value = value;
		}

		public Setter(DependencyProperty property, object value, string targetName) : this(property, value) => TargetName = targetName;

		public DependencyProperty Property { get; set; }

		public object Value { get; set; }

		public string TargetName { get; set; }
	}

	public sealed class SetterBaseCollection : List<SetterBase>
	{
	}

	/// <summary>Property values shared by every element that uses the style: below a local value, above inheritance.</summary>
	public class Style
	{
		public Style()
		{
		}

		public Style(Type targetType) => TargetType = targetType;

		public Style(Type targetType, Style basedOn) : this(targetType) => BasedOn = basedOn;

		public Type TargetType { get; set; }

		public Style BasedOn { get; set; }

		public SetterBaseCollection Setters { get; } = new SetterBaseCollection();

		public ResourceDictionary Resources { get; set; } = new ResourceDictionary();

		public bool IsSealed { get; private set; }

		public void Seal()
		{
			IsSealed = true;
			foreach (var setter in Setters)
				setter.IsSealed = true;
		}

		/// <summary>The value the style (or the one it is based on) gives <paramref name="property"/>.</summary>
		internal bool TryGetValue(XF.BindableProperty property, out object value)
		{
			for (var i = Setters.Count - 1; i >= 0; i--)
			{
				if (Setters[i] is Setter s && s.TargetName == null && s.Property?.Bindable == property)
				{
					value = s.Value;
					return true;
				}
			}

			if (BasedOn != null)
				return BasedOn.TryGetValue(property, out value);

			value = null;
			return false;
		}

		/// <summary>Every property the style sets.</summary>
		internal IEnumerable<DependencyProperty> Properties =>
			Setters.OfType<Setter>().Where(s => s.TargetName == null && s.Property != null).Select(s => s.Property)
				.Concat(BasedOn?.Properties ?? Enumerable.Empty<DependencyProperty>()).Distinct();
	}

	public static class LogicalTreeHelper
	{
		public static DependencyObject GetParent(DependencyObject current) =>
			(current ?? throw new ArgumentNullException(nameof(current))).LogicalParent;

		public static IEnumerable GetChildren(DependencyObject current) =>
			(current ?? throw new ArgumentNullException(nameof(current))).LogicalChildrenCore.Cast<object>().ToList();

		public static DependencyObject FindLogicalNode(DependencyObject logicalTreeNode, string elementName)
		{
			if (logicalTreeNode == null)
				throw new ArgumentNullException(nameof(logicalTreeNode));

			if (logicalTreeNode is FrameworkElement fe && fe.Name == elementName)
				return logicalTreeNode;

			foreach (var child in logicalTreeNode.LogicalChildrenCore.OfType<DependencyObject>())
			{
				var found = FindLogicalNode(child, elementName);
				if (found != null)
					return found;
			}

			return null;
		}
	}
}
