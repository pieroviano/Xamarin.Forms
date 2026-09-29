using System.Collections;
using System.Linq;
using System.Windows.Threading;
using XF = Xamarin.Forms;

namespace System.Windows
{
	/// <summary>
	/// The root of the WPF object model, over a Xamarin.Forms <see cref="XF.Element"/>: an element is what the
	/// Xamarin.Forms XAML compiler, name scopes, bindings and resources work on, and it carries no layout of its own -
	/// a <see cref="UIElement"/> puts a Xamarin.Forms view on screen and keeps this object for its WPF identity.
	/// </summary>
	/// <remarks>
	/// Values follow WPF's precedence: a local value, else the value of the element's <see cref="Style"/>, else -
	/// for an inherited property such as a font - the nearest ancestor's, else the default.
	/// </remarks>
	public class DependencyObject : XF.Element
	{
		DependencyObject _logicalParent;

		public DependencyObject() => Dispatcher = Dispatcher.CurrentDispatcher;

		public new Dispatcher Dispatcher { get; }

		public bool IsSealed => false;

		public bool CheckAccess() => Dispatcher.CheckAccess();

		public void VerifyAccess() => Dispatcher.VerifyAccess();

		public object GetValue(DependencyProperty dp)
		{
			if (dp == null)
				throw new ArgumentNullException(nameof(dp));

			return GetEffectiveValue(dp.Bindable);
		}

		/// <remarks>
		/// <c>base.</c>, not a plain call: C# prefers an overload declared in the most derived class, and the
		/// implicit conversion from <see cref="XF.BindableProperty"/> would make this method call itself.
		/// </remarks>
		public void SetValue(DependencyProperty dp, object value)
		{
			if (dp == null)
				throw new ArgumentNullException(nameof(dp));

			base.SetValue(dp.Bindable, value);
		}

		public void SetCurrentValue(DependencyProperty dp, object value) => SetValue(dp, value);

		public void ClearValue(DependencyProperty dp)
		{
			if (dp == null)
				throw new ArgumentNullException(nameof(dp));

			base.ClearValue(dp.Bindable);
		}

		/// <summary>The local value of <paramref name="dp"/>, or <see cref="DependencyProperty.UnsetValue"/> when it has none.</summary>
		public object ReadLocalValue(DependencyProperty dp)
		{
			if (dp == null)
				throw new ArgumentNullException(nameof(dp));

			return IsSet(dp.Bindable) ? base.GetValue(dp.Bindable) : DependencyProperty.UnsetValue;
		}

		public void InvalidateProperty(DependencyProperty dp)
		{
			if (dp == null)
				throw new ArgumentNullException(nameof(dp));

			var value = GetEffectiveValue(dp.Bindable);
			Notify(dp, value, value);
		}

		public void CoerceValue(DependencyProperty dp) => InvalidateProperty(dp);

		protected virtual void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		/// <summary>The value WPF would answer: local, style, inherited, default - in that order.</summary>
		internal object GetEffectiveValue(XF.BindableProperty property)
		{
			if (IsSet(property))
				return base.GetValue(property);

			if (TryGetStyleValue(property, out var styled))
				return styled;

			if (((DependencyProperty)property).Inherits)
			{
				for (var ancestor = _logicalParent; ancestor != null; ancestor = ancestor._logicalParent)
				{
					if (ancestor.IsSet(property))
						return ancestor.GetBaseValue(property);

					if (ancestor.TryGetStyleValue(property, out styled))
						return styled;
				}
			}

			return base.GetValue(property);
		}

		/// <summary>The effective value, typed: what every CLR property of this library reads.</summary>
		internal T Get<T>(XF.BindableProperty property) => (T)GetEffectiveValue(property);

		object GetBaseValue(XF.BindableProperty property) => base.GetValue(property);

		/// <summary>The value this element's style gives <paramref name="property"/>, if any.</summary>
		internal virtual bool TryGetStyleValue(XF.BindableProperty property, out object value)
		{
			value = null;
			return false;
		}

		/// <summary>Whether <paramref name="property"/> has a value of this element's own: local or from its style.</summary>
		internal bool HasOwnValue(XF.BindableProperty property) => IsSet(property) || TryGetStyleValue(property, out _);

		/// <summary>
		/// Whether anything gives <paramref name="property"/> a value here - this element, or for an inherited
		/// property an ancestor. When nothing does, the native default (the GTK theme's font, say) is left alone
		/// rather than replaced by WPF's.
		/// </summary>
		internal bool HasEffectiveValue(XF.BindableProperty property)
		{
			if (HasOwnValue(property))
				return true;

			if (!((DependencyProperty)property).Inherits)
				return false;

			for (var ancestor = _logicalParent; ancestor != null; ancestor = ancestor._logicalParent)
			{
				if (ancestor.HasOwnValue(property))
					return true;
			}

			return false;
		}

		/// <summary>Raised by every property this library declares, and by those WPF code registers.</summary>
		internal void OnBindableChanged(XF.BindableProperty property, object oldValue, object newValue) =>
			Notify(property, oldValue, GetEffectiveValue(property));

		internal void Notify(DependencyProperty property, object oldValue, object newValue)
		{
			var e = new DependencyPropertyChangedEventArgs(property, oldValue, newValue);
			property.DefaultMetadata.PropertyChangedCallback?.Invoke(this, e);
			OnPropertyChanged(e);

			if (property.Inherits)
				PropagateInherited(property, newValue);
		}

		/// <summary>Tells every descendant that takes <paramref name="property"/> from here that it changed.</summary>
		void PropagateInherited(DependencyProperty property, object value)
		{
			foreach (var child in LogicalChildrenCore.OfType<DependencyObject>())
			{
				if (child.HasOwnValue(property.Bindable))
					continue;

				var e = new DependencyPropertyChangedEventArgs(property, value, value);
				child.OnPropertyChanged(e);
				child.PropagateInherited(property, value);
			}
		}

		// ---- logical tree -------------------------------------------------------------------------------------

		/// <summary>The parent in the logical tree (WPF's <c>LogicalTreeHelper.GetParent</c>).</summary>
		internal DependencyObject LogicalParent => _logicalParent;

		/// <summary>The children in the logical tree: elements, and the plain content objects of content controls.</summary>
		internal virtual IEnumerable LogicalChildrenCore => Array.Empty<object>();

		/// <summary>Makes this object the logical parent of <paramref name="child"/>, when it is a dependency object.</summary>
		internal void AddLogicalChild(object child)
		{
			if (child is DependencyObject d)
				d.SetLogicalParent(this);
		}

		internal void RemoveLogicalChild(object child)
		{
			if (child is DependencyObject d && d._logicalParent == this)
				d.SetLogicalParent(null);
		}

		void SetLogicalParent(DependencyObject parent)
		{
			if (_logicalParent == parent)
				return;

			var oldParent = _logicalParent;
			_logicalParent = parent;

			// The Xamarin.Forms parent too: it is what resources, dynamic resources and the binding context
			// (WPF's DataContext) follow down the tree.
			Parent = parent;

			OnLogicalParentChanged(oldParent);
		}

		/// <summary>
		/// The binding context (WPF's DataContext) flows down the WPF logical tree: Xamarin.Forms pushes it only to the
		/// children it tracks itself, and these are not among them.
		/// </summary>
		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();
			foreach (var child in LogicalChildrenCore.OfType<DependencyObject>())
				SetInheritedBindingContext(child, BindingContext);
		}

		/// <summary>Re-reads what this element inherits, which a new parent may change.</summary>
		internal virtual void OnLogicalParentChanged(DependencyObject oldParent)
		{
			foreach (var property in DependencyProperty.InheritedProperties)
			{
				if (HasOwnValue(property.Bindable))
					continue;

				var value = GetEffectiveValue(property.Bindable);
				OnPropertyChanged(new DependencyPropertyChangedEventArgs(property, value, value));
				PropagateInherited(property, value);
			}
		}
	}
}
