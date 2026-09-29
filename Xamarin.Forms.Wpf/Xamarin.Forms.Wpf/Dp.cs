using System;
using System.Windows;

namespace Xamarin.Forms.Wpf
{
	/// <summary>
	/// Declares the dependency properties of this library: a <see cref="BindableProperty"/> whose changes reach
	/// <see cref="DependencyObject.OnPropertyChanged(DependencyPropertyChangedEventArgs)"/>, plus the WPF metadata
	/// its <see cref="DependencyProperty"/> wrapper answers with.
	/// </summary>
	internal static class Dp
	{
		internal static BindableProperty Register<TOwner>(string name, Type type, object defaultValue, bool inherits = false, BindingMode mode = BindingMode.OneWay)
		{
			var bindable = CreateBindable(name, type, typeof(TOwner), defaultValue, mode);
			Declare(bindable, defaultValue, inherits);
			return bindable;
		}

		/// <summary>
		/// An attached property. Every one this library declares is a panel's (row, column, dock, position, z-order),
		/// so a change also tells the panel the element is in, which places it again.
		/// </summary>
		internal static BindableProperty Attached<TOwner>(string name, Type type, object defaultValue, bool inherits = false)
		{
			BindableProperty bindable = null;
			bindable = BindableProperty.CreateAttached(name, type, typeof(TOwner), defaultValue,
				propertyChanged: (b, o, n) =>
				{
					if (!(b is DependencyObject d))
						return;

					d.OnBindableChanged(bindable, o, n);
					System.Windows.Controls.Panel.OnAttachedChanged(d);
				});
			Declare(bindable, defaultValue, inherits);
			return bindable;
		}

		internal static BindableProperty CreateBindable(string name, Type type, Type owner, object defaultValue, BindingMode mode = BindingMode.OneWay)
		{
			BindableProperty bindable = null;
			bindable = BindableProperty.Create(name, type, owner, defaultValue, mode,
				propertyChanged: (b, o, n) => (b as DependencyObject)?.OnBindableChanged(bindable, o, n));
			return bindable;
		}

		static void Declare(BindableProperty bindable, object defaultValue, bool inherits) =>
			DependencyProperty.Declare(bindable, inherits
				? new FrameworkPropertyMetadata(defaultValue, FrameworkPropertyMetadataOptions.Inherits)
				: new FrameworkPropertyMetadata(defaultValue));
	}
}
