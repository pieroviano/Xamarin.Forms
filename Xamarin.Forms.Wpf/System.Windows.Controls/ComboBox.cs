using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows.Controls
{
	/// <summary>
	/// A drop-down list over a Xamarin.Forms picker; an editable one is an entry beside a narrow picker, whose
	/// choice fills the entry. Typing text that names an item selects it, as WPF's text search does.
	/// </summary>
	public class ComboBox : Selector
	{
		readonly ObservableCollection<string> _texts = new ObservableCollection<string>();
		XF.Picker _picker;
		XF.Entry _entry;
		bool _syncing;

		public static readonly XF.BindableProperty IsEditableProperty = Dp.Register<ComboBox>(nameof(IsEditable), typeof(bool), false);
		public static readonly XF.BindableProperty IsReadOnlyProperty = Dp.Register<ComboBox>(nameof(IsReadOnly), typeof(bool), false);
		public static readonly XF.BindableProperty TextProperty = Dp.Register<ComboBox>(nameof(Text), typeof(string), string.Empty, mode: XF.BindingMode.TwoWay);
		public static readonly XF.BindableProperty IsDropDownOpenProperty = Dp.Register<ComboBox>(nameof(IsDropDownOpen), typeof(bool), false);
		public static readonly XF.BindableProperty MaxDropDownHeightProperty = Dp.Register<ComboBox>(nameof(MaxDropDownHeight), typeof(double), 360.0);
		public static readonly XF.BindableProperty StaysOpenOnEditProperty = Dp.Register<ComboBox>(nameof(StaysOpenOnEdit), typeof(bool), false);
		public static readonly XF.BindableProperty IsTextSearchEnabledProperty = Dp.Register<ComboBox>(nameof(IsTextSearchEnabled), typeof(bool), true);

		public ComboBox() => SetValue(BorderThicknessProperty, new Thickness(1));

		public bool IsEditable
		{
			get => Get<bool>(IsEditableProperty);
			set => SetValue(IsEditableProperty, value);
		}

		public bool IsReadOnly
		{
			get => Get<bool>(IsReadOnlyProperty);
			set => SetValue(IsReadOnlyProperty, value);
		}

		/// <summary>The text of the selection, or what the user typed into an editable combo box.</summary>
		public string Text
		{
			get => Get<string>(TextProperty);
			set => SetValue(TextProperty, value ?? string.Empty);
		}

		public bool IsDropDownOpen
		{
			get => Get<bool>(IsDropDownOpenProperty);
			set => SetValue(IsDropDownOpenProperty, value);
		}

		public double MaxDropDownHeight
		{
			get => Get<double>(MaxDropDownHeightProperty);
			set => SetValue(MaxDropDownHeightProperty, value);
		}

		public bool StaysOpenOnEdit
		{
			get => Get<bool>(StaysOpenOnEditProperty);
			set => SetValue(StaysOpenOnEditProperty, value);
		}

		public bool IsTextSearchEnabled
		{
			get => Get<bool>(IsTextSearchEnabledProperty);
			set => SetValue(IsTextSearchEnabledProperty, value);
		}

		public object SelectionBoxItem => SelectedItem is ComboBoxItem item ? item.Content : SelectedItem;

		public event EventHandler DropDownOpened;

		public event EventHandler DropDownClosed;

		protected virtual void OnDropDownOpened(EventArgs e) => DropDownOpened?.Invoke(this, e);

		protected virtual void OnDropDownClosed(EventArgs e) => DropDownClosed?.Invoke(this, e);

		internal override XF.View CreateNativeView()
		{
			_picker = new XF.Picker { ItemsSource = _texts };
			_picker.SelectedIndexChanged += (s, e) =>
			{
				if (_syncing)
					return;

				Select(_picker.SelectedIndex, true);
			};

			var grid = new XF.Grid
			{
				RowSpacing = 0,
				ColumnSpacing = 0,
				ColumnDefinitions = { new XF.ColumnDefinition { Width = XF.GridLength.Star }, new XF.ColumnDefinition { Width = XF.GridLength.Auto } },
			};
			return grid;
		}

		/// <summary>The picker alone, or an entry with a narrow picker beside it: rebuilt when IsEditable changes.</summary>
		void ArrangeViews()
		{
			var grid = (XF.Grid)NativeView;
			grid.Children.Clear();
			if (IsEditable)
			{
				if (_entry == null)
				{
					_entry = new XF.Entry();
					_entry.TextChanged += (s, e) =>
					{
						if (!_syncing)
							Text = e.NewTextValue ?? string.Empty;
					};
				}

				_picker.WidthRequest = 28;
				grid.Children.Add(_entry, 0, 0);
				grid.Children.Add(_picker, 1, 0);
			}
			else
			{
				_picker.WidthRequest = -1;
				grid.Children.Add(_picker, 0, 0);
				XF.Grid.SetColumnSpan(_picker, 2);
			}

			ApplyText();
		}

		internal override XF.View TextView => IsEditable ? (XF.View)_entry : _picker;

		internal override void ApplyText()
		{
			base.ApplyText();
			if (_entry != null)
			{
				NativeText.ApplyFont(_picker, this);
				NativeText.ApplyForeground(_picker, this);
			}
		}

		internal override void ApplyBorder()
		{
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			ArrangeViews();
			ApplyItems(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			PushText();
		}

		internal override void ApplyItems(NotifyCollectionChangedEventArgs e)
		{
			if (_picker == null)
				return;

			_syncing = true;
			try
			{
				switch (e.Action)
				{
					case NotifyCollectionChangedAction.Add when e.NewItems != null && e.NewStartingIndex >= 0:
						for (var i = 0; i < e.NewItems.Count; i++)
							_texts.Insert(e.NewStartingIndex + i, ItemText(e.NewItems[i]));
						break;
					case NotifyCollectionChangedAction.Remove when e.OldItems != null && e.OldStartingIndex >= 0:
						for (var i = 0; i < e.OldItems.Count; i++)
							_texts.RemoveAt(e.OldStartingIndex);
						break;
					default:
						_texts.Clear();
						foreach (var item in Items)
							_texts.Add(ItemText(item));
						break;
				}

				_picker.SelectedIndex = SelectedIndex;
			}
			finally
			{
				_syncing = false;
			}
		}

		internal override void ApplySelection()
		{
			if (_picker == null)
				return;

			_syncing = true;
			try
			{
				_picker.SelectedIndex = SelectedIndex;
			}
			finally
			{
				_syncing = false;
			}
		}

		/// <summary>A new selection is the new text; for an editable combo box, too.</summary>
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			var text = SelectedItem == null ? (IsEditable ? Text : string.Empty) : ItemText(SelectedItem);
			if (Text != text)
			{
				_textFromSelection = true;
				try
				{
					Text = text;
				}
				finally
				{
					_textFromSelection = false;
				}
			}

			base.OnSelectionChanged(e);
		}

		bool _textFromSelection;

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (p == TextProperty)
			{
				PushText();
				if (IsEditable)
					RaiseEvent(new TextChangedEventArgs(TextBoxBase.TextChangedEvent, UndoAction.None));

				// Text set from code (or typed) selects the item it names, if any.
				if (!_textFromSelection && IsTextSearchEnabled)
				{
					var index = Enumerable.Range(0, Items.Count).FirstOrDefault(i => string.Equals(ItemText(Items[i]), Text, StringComparison.CurrentCultureIgnoreCase));
					var found = Items.Count > 0 && string.Equals(ItemText(Items[index]), Text, StringComparison.CurrentCultureIgnoreCase);
					if (found && index != SelectedIndex)
						Select(index, false);
					else if (!found && !IsEditable && SelectedIndex >= 0)
						Select(-1, false);
				}
			}
			else if (HasNativeView && p == IsEditableProperty)
			{
				ArrangeViews();
				PushText();
			}
			else if (p == IsDropDownOpenProperty)
			{
				if (IsDropDownOpen)
				{
					_picker?.Focus();
					OnDropDownOpened(EventArgs.Empty);
				}
				else
				{
					OnDropDownClosed(EventArgs.Empty);
				}
			}
		}

		void PushText()
		{
			if (_entry == null || !IsEditable || _entry.Text == Text)
				return;

			_syncing = true;
			try
			{
				_entry.Text = Text;
			}
			finally
			{
				_syncing = false;
			}
		}

		/// <summary>The GTK combo box behind the picker reports when its list opens and closes.</summary>
		internal override void OnWidgetAttached(Gtk.Widget widget)
		{
			base.OnWidgetAttached(widget);
			foreach (var combo in GtkWidgets.Descendants<Gtk.ComboBox>(widget))
			{
				combo.AddNotification("popup-shown", (o, a) =>
				{
					if (combo.PopupShown != IsDropDownOpen)
						IsDropDownOpen = combo.PopupShown;
				});
			}
		}
	}
}
