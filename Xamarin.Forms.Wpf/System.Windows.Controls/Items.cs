using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Controls.Primitives;

using System.Windows.Input;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.ComponentModel
{
	public struct SortDescription : IEquatable<SortDescription>
	{
		public SortDescription(string propertyName, ListSortDirection direction)
		{
			PropertyName = propertyName;
			Direction = direction;
		}

		public string PropertyName { get; set; }

		public ListSortDirection Direction { get; set; }

		public bool IsSealed => false;

		public bool Equals(SortDescription other) => PropertyName == other.PropertyName && Direction == other.Direction;

		public override bool Equals(object obj) => obj is SortDescription other && Equals(other);

		public override int GetHashCode() => (PropertyName?.GetHashCode() ?? 0) ^ (int)Direction;

		public static bool operator ==(SortDescription a, SortDescription b) => a.Equals(b);

		public static bool operator !=(SortDescription a, SortDescription b) => !a.Equals(b);
	}

	public class SortDescriptionCollection : Collection<SortDescription>, INotifyCollectionChanged
	{
		public static readonly SortDescriptionCollection Empty = new SortDescriptionCollection();

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		protected override void InsertItem(int index, SortDescription item)
		{
			base.InsertItem(index, item);
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		protected override void RemoveItem(int index)
		{
			base.RemoveItem(index);
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		protected override void SetItem(int index, SortDescription item)
		{
			base.SetItem(index, item);
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		protected override void ClearItems()
		{
			base.ClearItems();
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}
	}

	/// <summary>Whether code runs in a designer: never, here.</summary>
	public static class DesignerProperties
	{
		public static readonly XF.BindableProperty IsInDesignModeProperty = Dp.Attached<System.Windows.DependencyObject>("IsInDesignMode", typeof(bool), false);

		public static bool GetIsInDesignMode(System.Windows.DependencyObject element) => false;

		public static void SetIsInDesignMode(System.Windows.DependencyObject element, bool value)
		{
		}
	}
}

namespace System.Windows.Controls
{
	/// <summary>
	/// The items of an items control: its own list, or a view of <see cref="ItemsControl.ItemsSource"/>. Sort
	/// descriptions order it, and an item added to a sorted collection goes where the order puts it.
	/// </summary>
	public sealed class ItemCollection : IList, INotifyCollectionChanged
	{
		readonly List<object> _items = new List<object>();
		IEnumerable _source;
		int _currentPosition = -1;

		internal ItemCollection()
		{
			SortDescriptions.CollectionChanged += (s, e) => Refresh();
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		public event EventHandler CurrentChanged;

		public SortDescriptionCollection SortDescriptions { get; } = new SortDescriptionCollection();

		public Predicate<object> Filter { get; set; }

		public bool CanSort => true;

		public bool CanFilter => true;

		public int Count => _items.Count;

		public bool IsEmpty => _items.Count == 0;

		public bool IsReadOnly => _source != null;

		public bool IsFixedSize => _source != null;

		public bool NeedsRefresh => false;

		public object CurrentItem => _currentPosition >= 0 && _currentPosition < _items.Count ? _items[_currentPosition] : null;

		public int CurrentPosition => _currentPosition;

		public IEnumerable SourceCollection => _source ?? _items;

		public object this[int index]
		{
			get => _items[index];
			set
			{
				ThrowIfSourced();
				var old = _items[index];
				_items[index] = value;
				Raise(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, old, index));
			}
		}

		public int Add(object newItem)
		{
			ThrowIfSourced();
			var index = SortedIndexOf(newItem);
			_items.Insert(index, newItem);
			Raise(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, index));
			return index;
		}

		public void Insert(int insertIndex, object insertItem)
		{
			ThrowIfSourced();
			if (SortDescriptions.Count > 0)
				throw new InvalidOperationException("Operation is not valid on a sorted collection: use Add.");

			_items.Insert(insertIndex, insertItem);
			Raise(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, insertItem, insertIndex));
		}

		public void Remove(object removeItem)
		{
			ThrowIfSourced();
			var index = _items.IndexOf(removeItem);
			if (index >= 0)
				RemoveAt(index);
		}

		public void RemoveAt(int removeIndex)
		{
			ThrowIfSourced();
			var item = _items[removeIndex];
			_items.RemoveAt(removeIndex);
			Raise(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, removeIndex));
		}

		public void Clear()
		{
			ThrowIfSourced();
			_items.Clear();
			Raise(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public bool Contains(object containItem) => _items.Contains(containItem);

		public int IndexOf(object item) => _items.IndexOf(item);

		public void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

		public IEnumerator GetEnumerator() => _items.ToArray().GetEnumerator();

		public bool PassesFilter(object item) => Filter == null || Filter(item);

		public bool MoveCurrentTo(object item)
		{
			_currentPosition = _items.IndexOf(item);
			CurrentChanged?.Invoke(this, EventArgs.Empty);
			return _currentPosition >= 0;
		}

		public bool MoveCurrentToPosition(int position)
		{
			_currentPosition = position;
			CurrentChanged?.Invoke(this, EventArgs.Empty);
			return position >= 0 && position < _items.Count;
		}

		public bool MoveCurrentToFirst() => MoveCurrentToPosition(0);

		public bool MoveCurrentToLast() => MoveCurrentToPosition(_items.Count - 1);

		public bool MoveCurrentToNext() => MoveCurrentToPosition(_currentPosition + 1);

		public bool MoveCurrentToPrevious() => MoveCurrentToPosition(_currentPosition - 1);

		/// <summary>Re-reads the source (if any) and applies the filter and the sort order again.</summary>
		public void Refresh()
		{
			if (_source != null)
			{
				_items.Clear();
				foreach (var item in _source)
				{
					if (PassesFilter(item))
						_items.Add(item);
				}
			}

			if (SortDescriptions.Count > 0)
			{
				var sorted = _items.Select((item, i) => (item, i)).OrderBy(t => t.item, Comparer.Create(SortDescriptions)).ThenBy(t => t.i).Select(t => t.item).ToList();
				_items.Clear();
				_items.AddRange(sorted);
			}

			Raise(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public IDisposable DeferRefresh() => new DeferredRefresh(this);

		/// <summary>Makes the collection a view of <paramref name="source"/> (null: back to its own, empty list).</summary>
		internal void SetSource(IEnumerable source)
		{
			switch (_source)
			{
				case INotifyCollectionChanged ncc:
					ncc.CollectionChanged -= OnSourceChanged;
					break;
				case IBindingList bl:
					bl.ListChanged -= OnSourceListChanged;
					break;
			}

			_source = source;
			switch (source)
			{
				case INotifyCollectionChanged ncc:
					ncc.CollectionChanged += OnSourceChanged;
					break;
				case IBindingList bl:
					bl.ListChanged += OnSourceListChanged;
					break;
			}

			if (source == null)
				_items.Clear();
			Refresh();
		}

		void OnSourceChanged(object sender, NotifyCollectionChangedEventArgs e) => Refresh();

		/// <summary>
		/// A binding list (a data view) reports one row at a time: the change is applied to that row alone, so filling a
		/// table cell by cell is not a full refresh per cell. With a sort or a filter the order may move: refresh.
		/// </summary>
		void OnSourceListChanged(object sender, ListChangedEventArgs e)
		{
			var list = (IList)sender;
			if (SortDescriptions.Count > 0 || Filter != null || list.Count != _items.Count + Delta(e.ListChangedType))
			{
				Refresh();
				return;
			}

			var index = e.NewIndex;
			switch (e.ListChangedType)
			{
				case ListChangedType.ItemChanged when index >= 0 && index < _items.Count:
					var old = _items[index];
					_items[index] = list[index];
					Raise(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, _items[index], old, index));
					break;
				case ListChangedType.ItemAdded when index >= 0 && index <= _items.Count:
					_items.Insert(index, list[index]);
					Raise(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _items[index], index));
					break;
				case ListChangedType.ItemDeleted when index >= 0 && index < _items.Count:
					var removed = _items[index];
					_items.RemoveAt(index);
					Raise(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, index));
					break;
				default:
					Refresh();
					break;
			}
		}

		static int Delta(ListChangedType type) => type == ListChangedType.ItemAdded ? 1 : type == ListChangedType.ItemDeleted ? -1 : 0;

		int SortedIndexOf(object item)
		{
			if (SortDescriptions.Count == 0)
				return _items.Count;

			var comparer = Comparer.Create(SortDescriptions);
			var index = _items.Count;
			while (index > 0 && comparer.Compare(_items[index - 1], item) > 0)
				index--;
			return index;
		}

		void ThrowIfSourced()
		{
			if (_source != null)
				throw new InvalidOperationException("Operation is not valid while ItemsSource is in use. Access and modify elements with ItemsControl.ItemsSource instead.");
		}

		void Raise(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => this;

		sealed class DeferredRefresh : IDisposable
		{
			readonly ItemCollection _owner;

			public DeferredRefresh(ItemCollection owner) => _owner = owner;

			public void Dispose() => _owner.Refresh();
		}

		/// <summary>WPF's comparison of sort keys: text by the current culture, everything else by its own order.</summary>
		sealed class Comparer : IComparer<object>
		{
			readonly SortDescription[] _sorts;

			Comparer(SortDescription[] sorts) => _sorts = sorts;

			internal static Comparer Create(IEnumerable<SortDescription> sorts) => new Comparer(sorts.ToArray());

			public int Compare(object x, object y)
			{
				foreach (var sort in _sorts)
				{
					var a = ItemsControl.PropertyOf(x, sort.PropertyName);
					var b = ItemsControl.PropertyOf(y, sort.PropertyName);
					int result;
					if (a is string sa && b is string sb)
						result = string.Compare(sa, sb, CultureInfo.CurrentCulture, CompareOptions.None);
					else if (a == null || b == null)
						result = a == null ? (b == null ? 0 : -1) : 1;
					else if (a is IComparable ca && a.GetType() == b.GetType())
						result = ca.CompareTo(b);
					else
						result = string.Compare(Convert.ToString(a, CultureInfo.CurrentCulture), Convert.ToString(b, CultureInfo.CurrentCulture), CultureInfo.CurrentCulture, CompareOptions.None);

					if (result != 0)
						return sort.Direction == ListSortDirection.Descending ? -result : result;
				}

				return 0;
			}
		}
	}

	public enum GeneratorStatus
	{
		NotStarted,
		GeneratingContainers,
		ContainersGenerated,
		Error,
	}

	/// <summary>
	/// The containers of an items control's items. Items that are containers themselves (a <c>ListBoxItem</c>, a
	/// <c>TabItem</c>) are their own; a control that makes containers for plain items (a data grid's rows) says so.
	/// </summary>
	public sealed class ItemContainerGenerator
	{
		readonly ItemsControl _owner;

		internal ItemContainerGenerator(ItemsControl owner) => _owner = owner;

		public GeneratorStatus Status => GeneratorStatus.ContainersGenerated;

		public ItemCollection Items => _owner.Items;

		public event EventHandler StatusChanged;

		public DependencyObject ContainerFromIndex(int index) =>
			index >= 0 && index < _owner.Items.Count ? _owner.ContainerFor(index) : null;

		public DependencyObject ContainerFromItem(object item)
		{
			var index = _owner.Items.IndexOf(item);
			return index < 0 ? null : ContainerFromIndex(index);
		}

		public int IndexFromContainer(DependencyObject container)
		{
			for (var i = 0; i < _owner.Items.Count; i++)
			{
				if (ReferenceEquals(ContainerFromIndex(i), container))
					return i;
			}

			return -1;
		}

		public object ItemFromContainer(DependencyObject container)
		{
			var index = IndexFromContainer(container);
			return index < 0 ? DependencyProperty.UnsetValue : _owner.Items[index];
		}

		internal void RaiseStatusChanged() => StatusChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>A control that shows a list of items, its own or those of <see cref="ItemsSource"/>.</summary>
	[XF.ContentProperty(nameof(Items))]
	public class ItemsControl : Control
	{
		public static readonly XF.BindableProperty ItemsSourceProperty = Dp.Register<ItemsControl>(nameof(ItemsSource), typeof(IEnumerable), null);
		public static readonly XF.BindableProperty DisplayMemberPathProperty = Dp.Register<ItemsControl>(nameof(DisplayMemberPath), typeof(string), string.Empty);
		public static readonly XF.BindableProperty ItemTemplateProperty = Dp.Register<ItemsControl>(nameof(ItemTemplate), typeof(DataTemplate), null);
		public static readonly XF.BindableProperty ItemStringFormatProperty = Dp.Register<ItemsControl>(nameof(ItemStringFormat), typeof(string), null);
		public static readonly XF.BindableProperty AlternationCountProperty = Dp.Register<ItemsControl>(nameof(AlternationCount), typeof(int), 0);

		public ItemsControl()
		{
			Items = new ItemCollection();
			Items.CollectionChanged += OnItemsCollectionChanged;
			ItemContainerGenerator = new ItemContainerGenerator(this);
		}

		public ItemCollection Items { get; }

		public ItemContainerGenerator ItemContainerGenerator { get; }

		public IEnumerable ItemsSource
		{
			get => Get<IEnumerable>(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
		}

		public string DisplayMemberPath
		{
			get => Get<string>(DisplayMemberPathProperty);
			set => SetValue(DisplayMemberPathProperty, value);
		}

		public DataTemplate ItemTemplate
		{
			get => Get<DataTemplate>(ItemTemplateProperty);
			set => SetValue(ItemTemplateProperty, value);
		}

		public string ItemStringFormat
		{
			get => Get<string>(ItemStringFormatProperty);
			set => SetValue(ItemStringFormatProperty, value);
		}

		public int AlternationCount
		{
			get => Get<int>(AlternationCountProperty);
			set => SetValue(AlternationCountProperty, value);
		}

		public bool HasItems => Items.Count > 0;

		public static ItemsControl ItemsControlFromItemContainer(DependencyObject container) => container?.LogicalParent as ItemsControl;

		protected virtual void OnItemsChanged(NotifyCollectionChangedEventArgs e)
		{
		}

		protected virtual void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
		}

		/// <summary>The container of the item at <paramref name="index"/>: the item itself when it is one.</summary>
		internal virtual DependencyObject ContainerFor(int index) => Items[index] as DependencyObject;

		internal override IEnumerable LogicalChildrenCore =>
			ItemsSource != null ? Array.Empty<object>() : Items.Cast<object>().Where(i => i is DependencyObject).ToArray();

		/// <summary>An item's text: its content (a container), its <see cref="DisplayMemberPath"/>, else ToString.</summary>
		internal string ItemText(object item)
		{
			switch (item)
			{
				case null:
					return string.Empty;
				case HeaderedContentControl h:
					return h.HeaderText;
				case ContentControl c:
					return c.ContentText;
				case HeaderedItemsControl hi:
					return hi.HeaderText;
				case TextBlock t:
					return t.Text;
			}

			var value = string.IsNullOrEmpty(DisplayMemberPath) ? item : PropertyOf(item, DisplayMemberPath);
			return string.IsNullOrEmpty(ItemStringFormat) ? Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty : string.Format(CultureInfo.CurrentCulture, ItemStringFormat, value);
		}

		/// <summary>The value of a property path of an item (a data row's column, an object's property); the item for an empty path.</summary>
		internal static object PropertyOf(object item, string path)
		{
			if (string.IsNullOrEmpty(path) || item == null)
				return item;

			foreach (var part in path.Split('.'))
			{
				if (item == null)
					return null;

				if (item is ICustomTypeDescriptor descriptor)
				{
					var property = descriptor.GetProperties().Find(part, true);
					if (property != null)
					{
						item = property.GetValue(item);
						continue;
					}
				}

				var info = item.GetType().GetProperty(part);
				item = info?.GetValue(item, null);
			}

			return item;
		}

		void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (ItemsSource == null)
			{
				if (e.OldItems != null)
				{
					foreach (var item in e.OldItems)
						RemoveLogicalChild(item);
				}

				if (e.NewItems != null)
				{
					foreach (var item in e.NewItems)
						AddLogicalChild(item);
				}

				if (e.Action == NotifyCollectionChangedAction.Reset)
				{
					foreach (var item in Items)
						AddLogicalChild(item);
				}
			}

			OnItemsChanged(e);
			if (HasNativeView)
				ApplyItems(e);
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (p == ItemsSourceProperty)
			{
				Items.SetSource((IEnumerable)e.NewValue);
				OnItemsSourceChanged((IEnumerable)e.OldValue, (IEnumerable)e.NewValue);
			}
			else if (HasNativeView && (p == DisplayMemberPathProperty || p == ItemStringFormatProperty))
			{
				ApplyItems(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}
		}

		internal override XF.View CreateNativeView() => new NativeShell(new XF.StackLayout { Spacing = 0 });

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplyItems(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		/// <summary>The items into the view; by default a stack of the items' own views, or their text.</summary>
		internal virtual void ApplyItems(NotifyCollectionChangedEventArgs e)
		{
			if (!(NativeView is NativeShell shell) || !(shell.Inner is XF.StackLayout stack))
				return;

			stack.Children.Clear();
			foreach (var item in Items)
				stack.Children.Add(ViewOf(item));
		}

		/// <summary>An item's view: an element's own, anything else as a label.</summary>
		internal XF.View ViewOf(object item)
		{
			if (item is UIElement element)
				return element.NativeView;

			var label = new XF.Label { Text = ItemText(item) };
			NativeText.ApplyFont(label, this);
			NativeText.ApplyForeground(label, this);
			return label;
		}
	}

	[XF.ContentProperty(nameof(Items))]
	public class HeaderedItemsControl : ItemsControl
	{
		public static readonly XF.BindableProperty HeaderProperty = Dp.Register<HeaderedItemsControl>(nameof(Header), typeof(object), null);

		public object Header
		{
			get => Get<object>(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public bool HasHeader => Header != null;

		/// <summary>The header as text; access keys stripped.</summary>
		internal string HeaderText => Header is string s ? NativeText.StripAccessKey(s) : Header is TextBlock t ? t.Text : Header?.ToString() ?? string.Empty;

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.Property.Bindable == HeaderProperty)
			{
				RemoveLogicalChild(e.OldValue);
				AddLogicalChild(e.NewValue);
			}

			base.OnPropertyChanged(e);
			if (e.Property.Bindable == HeaderProperty)
				OnHeaderChanged();
		}

		internal virtual void OnHeaderChanged()
		{
		}
	}

	public class SelectionChangedEventArgs : RoutedEventArgs
	{
		public SelectionChangedEventArgs(RoutedEvent id, IList removedItems, IList addedItems) : base(id)
		{
			RemovedItems = removedItems ?? throw new ArgumentNullException(nameof(removedItems));
			AddedItems = addedItems ?? throw new ArgumentNullException(nameof(addedItems));
		}

		public IList AddedItems { get; }

		public IList RemovedItems { get; }

		protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
		{
			if (genericHandler is SelectionChangedEventHandler typed)
				typed(genericTarget, this);
			else
				base.InvokeEventHandler(genericHandler, genericTarget);
		}
	}

	public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs e);
}

namespace System.Windows.Controls.Primitives
{
	/// <summary>An items control with a selected item: <see cref="SelectedIndex"/> and <see cref="SelectedItem"/> move together.</summary>
	public abstract class Selector : ItemsControl
	{
		public static readonly RoutedEvent SelectionChangedEvent =
			EventManager.RegisterRoutedEvent("SelectionChanged", RoutingStrategy.Bubble, typeof(SelectionChangedEventHandler), typeof(Selector));

		/// <summary>Coerced as WPF's is: an index outside the items is -1.</summary>
		public static readonly XF.BindableProperty SelectedIndexProperty = Dp.Register<Selector>(nameof(SelectedIndex), typeof(int), -1, mode: XF.BindingMode.TwoWay,
			coerce: (b, v) => b is Selector s ? s.CoerceIndex((int)v) : v);

		/// <summary>Coerced as WPF's is: an item that is not among the items is no selection.</summary>
		public static readonly XF.BindableProperty SelectedItemProperty = Dp.Register<Selector>(nameof(SelectedItem), typeof(object), null, mode: XF.BindingMode.TwoWay,
			coerce: (b, v) => b is Selector s && v != null && !s.Items.Contains(v) ? null : v);
		public static readonly XF.BindableProperty SelectedValuePathProperty = Dp.Register<Selector>(nameof(SelectedValuePath), typeof(string), string.Empty);
		public static readonly XF.BindableProperty IsSynchronizedWithCurrentItemProperty = Dp.Register<Selector>(nameof(IsSynchronizedWithCurrentItem), typeof(bool?), null);

		/// <summary>Set while the selection is being changed, so the two properties do not chase each other.</summary>
		bool _selecting;

		/// <summary>Set while a change comes from the view, so it is not pushed back to it.</summary>
		internal bool FromNative;

		public int SelectedIndex
		{
			get => Get<int>(SelectedIndexProperty);
			set => SetValue(SelectedIndexProperty, value);
		}

		public object SelectedItem
		{
			get => Get<object>(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		public object SelectedValue
		{
			get => PropertyOf(SelectedItem, SelectedValuePath);
			set
			{
				for (var i = 0; i < Items.Count; i++)
				{
					if (Equals(PropertyOf(Items[i], SelectedValuePath), value))
					{
						SelectedIndex = i;
						return;
					}
				}

				SelectedIndex = -1;
			}
		}

		public string SelectedValuePath
		{
			get => Get<string>(SelectedValuePathProperty);
			set => SetValue(SelectedValuePathProperty, value);
		}

		public bool? IsSynchronizedWithCurrentItem
		{
			get => Get<bool?>(IsSynchronizedWithCurrentItemProperty);
			set => SetValue(IsSynchronizedWithCurrentItemProperty, value);
		}

		public event SelectionChangedEventHandler SelectionChanged { add => AddHandler(SelectionChangedEvent, value); remove => RemoveHandler(SelectionChangedEvent, value); }

		protected virtual void OnSelectionChanged(SelectionChangedEventArgs e) => RaiseEvent(e);

		internal int CoerceIndex(int index) => index >= 0 && index < Items.Count ? index : -1;

		/// <summary>Selects the item at <paramref name="index"/> (-1: none), from code or from the view.</summary>
		internal void Select(int index, bool fromNative)
		{
			FromNative = fromNative;
			try
			{
				SelectedIndex = CoerceIndex(index);
			}
			finally
			{
				FromNative = false;
			}
		}

		/// <summary>
		/// One property of the pair changed: the other follows, and the change is announced.
		/// </summary>
		/// <remarks>
		/// Never by setting the property that changed, from its own change notification: Xamarin.Forms queues that
		/// set and notifies again after it, so a handler that "reverted" the raw value and then applied the real
		/// one ran forever. Out-of-range values are the properties' coercion's business instead.
		/// </remarks>
		void OnSelectionPropertyChanged(bool indexChanged, object oldItem)
		{
			object newItem;
			_selecting = true;
			try
			{
				if (indexChanged)
				{
					var index = SelectedIndex;
					newItem = index < 0 ? null : Items[index];
					SetValue(SelectedItemProperty, newItem);
				}
				else
				{
					newItem = SelectedItem;
					SetValue(SelectedIndexProperty, newItem == null ? -1 : Items.IndexOf(newItem));
				}
			}
			finally
			{
				_selecting = false;
			}

			if (ReferenceEquals(oldItem, newItem))
				return;

			SyncContainers(oldItem, newItem);
			if (HasNativeView && !FromNative)
				ApplySelection();

			OnSelectionChanged(new SelectionChangedEventArgs(SelectionChangedEvent,
				oldItem == null ? (IList)Array.Empty<object>() : new[] { oldItem },
				newItem == null ? (IList)Array.Empty<object>() : new[] { newItem }));
		}

		/// <summary>Sets SelectedIndex and SelectedItem without a selection change: a multiple selection moving its first item.</summary>
		internal void SetSelectionQuietly(int index, object item)
		{
			_selecting = true;
			try
			{
				SetValue(SelectedIndexProperty, index);
				SetValue(SelectedItemProperty, item);
			}
			finally
			{
				_selecting = false;
			}
		}

		/// <summary>An item that is its own container (a list box item) knows whether it is selected.</summary>
		internal virtual void SyncContainers(object oldItem, object newItem)
		{
			if (oldItem is ListBoxItem oldContainer && !ReferenceEquals(oldItem, newItem))
				oldContainer.SetSelectedFromSelector(false);
			if (newItem is ListBoxItem newContainer)
				newContainer.SetSelectedFromSelector(true);
		}

		/// <summary>The selection into the view.</summary>
		internal virtual void ApplySelection()
		{
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (_selecting)
				return;

			var p = e.Property.Bindable;
			if (p == SelectedIndexProperty)
				OnSelectionPropertyChanged(true, SelectedItem);
			else if (p == SelectedItemProperty)
				OnSelectionPropertyChanged(false, e.OldValue);
		}

		/// <summary>Keeps the selection on the same item when items come and go; a removed selected item leaves none.</summary>
		protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);
			var selected = SelectedItem;
			if (selected == null)
				return;

			var index = Items.IndexOf(selected);
			if (index < 0)
			{
				Select(-1, false);
				return;
			}

			if (index != SelectedIndex)
			{
				_selecting = true;
				SetValue(SelectedIndexProperty, index);
				_selecting = false;
			}
		}

		public static bool GetIsSelected(DependencyObject element) => element is ListBoxItem item && item.IsSelected;

		public static void SetIsSelected(DependencyObject element, bool isSelected)
		{
			if (element is ListBoxItem item)
				item.IsSelected = isSelected;
		}
	}
}

namespace System.Windows.Controls
{
	public enum SelectionMode
	{
		Single,
		Multiple,
		Extended,
	}

	/// <summary>An item of a list box: its content, and whether it is selected.</summary>
	public class ListBoxItem : ContentControl
	{
		public static readonly XF.BindableProperty IsSelectedProperty = Dp.Register<ListBoxItem>(nameof(IsSelected), typeof(bool), false);

		public static readonly RoutedEvent SelectedEvent =
			EventManager.RegisterRoutedEvent("Selected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ListBoxItem));

		public static readonly RoutedEvent UnselectedEvent =
			EventManager.RegisterRoutedEvent("Unselected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ListBoxItem));

		bool _fromSelector;

		public bool IsSelected
		{
			get => Get<bool>(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		public event RoutedEventHandler Selected { add => AddHandler(SelectedEvent, value); remove => RemoveHandler(SelectedEvent, value); }

		public event RoutedEventHandler Unselected { add => AddHandler(UnselectedEvent, value); remove => RemoveHandler(UnselectedEvent, value); }

		internal void SetSelectedFromSelector(bool selected)
		{
			_fromSelector = true;
			try
			{
				IsSelected = selected;
			}
			finally
			{
				_fromSelector = false;
			}
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property.Bindable == ContentProperty && LogicalParent is ItemsControl owner && owner.HasNativeView)
				owner.ApplyItems(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			if (e.Property.Bindable != IsSelectedProperty)
				return;

			RaiseEvent(new RoutedEventArgs(IsSelected ? SelectedEvent : UnselectedEvent, this));
			if (_fromSelector || !(LogicalParent is Selector selector))
				return;

			if (selector is ListBox list && list.SelectionMode != SelectionMode.Single)
			{
				if (IsSelected && !list.SelectedItems.Contains(this))
					list.SelectedItems.Add(this);
				else if (!IsSelected)
					list.SelectedItems.Remove(this);
			}
			else if (IsSelected)
			{
				selector.SelectedItem = this;
			}
			else if (ReferenceEquals(selector.SelectedItem, this))
			{
				selector.SelectedIndex = -1;
			}
		}
	}

	public class ComboBoxItem : ListBoxItem
	{
		public bool IsHighlighted => false;
	}

	/// <summary>An item in the list view of a list or combo box: the item and the text it shows.</summary>
	internal sealed class ItemBox : INotifyPropertyChanged
	{
		string _text;
		string[] _cells;

		internal ItemBox(object item, string text, string[] cells = null)
		{
			Item = item;
			_text = text;
			_cells = cells;
		}

		public object Item { get; }

		public string Text
		{
			get => _text;
			set
			{
				if (_text == value)
					return;
				_text = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
			}
		}

		/// <summary>The text of each column, for a list view with columns.</summary>
		public string[] Cells
		{
			get => _cells;
			set
			{
				_cells = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Cells)));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public override string ToString() => _text;
	}

	/// <summary>
	/// A list of items over a Xamarin.Forms collection view, with single or multiple selection; a double click
	/// raises <see cref="Control.MouseDoubleClick"/> as it does in WPF.
	/// </summary>
	public class ListBox : Selector
	{
		readonly ObservableCollection<ItemBox> _boxes = new ObservableCollection<ItemBox>();
		readonly ObservableCollection<object> _selectedItems = new ObservableCollection<object>();
		bool _syncingSelection;

		public static readonly XF.BindableProperty SelectionModeProperty = Dp.Register<ListBox>(nameof(SelectionMode), typeof(SelectionMode), SelectionMode.Single);

		public ListBox()
		{
			SetValue(BorderThicknessProperty, new Thickness(1));
			_selectedItems.CollectionChanged += OnSelectedItemsChanged;
		}

		public SelectionMode SelectionMode
		{
			get => Get<SelectionMode>(SelectionModeProperty);
			set => SetValue(SelectionModeProperty, value);
		}

		/// <summary>The selected items; adding or removing one selects or unselects it.</summary>
		public IList SelectedItems => _selectedItems;

		public void SelectAll()
		{
			if (SelectionMode == SelectionMode.Single)
				throw new NotSupportedException("Can only call SelectAll when SelectionMode is Multiple or Extended.");

			foreach (var item in Items)
			{
				if (!_selectedItems.Contains(item))
					_selectedItems.Add(item);
			}
		}

		public void UnselectAll() => _selectedItems.Clear();

		public void ScrollIntoView(object item)
		{
			var index = Items.IndexOf(item);
			if (index >= 0 && Collection is XF.CollectionView view)
				view.ScrollTo(index, position: XF.ScrollToPosition.MakeVisible, animate: false);
		}

		XF.CollectionView Collection => (NativeView as NativeShell)?.Inner as XF.CollectionView;

		internal override XF.View CreateNativeView()
		{
			var view = new XF.CollectionView { ItemsSource = _boxes, SelectionMode = XF.SelectionMode.Single, ItemTemplate = ItemTemplateFor(this) };
			view.SelectionChanged += OnNativeSelectionChanged;
			return new NativeShell(view);
		}

		/// <summary>A label per item, in the list's font and color.</summary>
		internal static XF.DataTemplate ItemTemplateFor(ItemsControl owner) => new XF.DataTemplate(() =>
		{
			var label = new XF.Label { Padding = new XF.Thickness(2, 1), LineBreakMode = XF.LineBreakMode.NoWrap };
			label.SetBinding(XF.Label.TextProperty, new XF.Binding(nameof(ItemBox.Text)));
			NativeText.ApplyFont(label, owner);
			NativeText.ApplyForeground(label, owner);
			return label;
		});

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplySelectionMode();
			ApplySelection();
		}

		internal override void ApplyText()
		{
			base.ApplyText();
			if (Collection is XF.CollectionView view)
				view.ItemTemplate = ItemTemplateFor(this);
		}

		internal override void ApplyBackground()
		{
			base.ApplyBackground();
			if (Collection is XF.CollectionView view)
				view.BackgroundColor = Background?.ToFormsColor() ?? XF.Color.Default;
		}

		/// <summary>The item boxes follow the items: in place for an add or a remove, rebuilt otherwise.</summary>
		internal override void ApplyItems(NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add when e.NewItems != null && e.NewStartingIndex >= 0:
					for (var i = 0; i < e.NewItems.Count; i++)
						_boxes.Insert(e.NewStartingIndex + i, Box(e.NewItems[i]));
					break;
				case NotifyCollectionChangedAction.Remove when e.OldItems != null && e.OldStartingIndex >= 0:
					for (var i = 0; i < e.OldItems.Count; i++)
						_boxes.RemoveAt(e.OldStartingIndex);
					break;
				default:
					_boxes.Clear();
					foreach (var item in Items)
						_boxes.Add(Box(item));
					break;
			}

			if (HasNativeView)
				ApplySelection();
		}

		internal virtual ItemBox Box(object item) => new ItemBox(item, ItemText(item));

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property.Bindable == SelectionModeProperty && HasNativeView)
				ApplySelectionMode();
		}

		void ApplySelectionMode()
		{
			if (Collection is XF.CollectionView view)
				view.SelectionMode = SelectionMode == SelectionMode.Single ? XF.SelectionMode.Single : XF.SelectionMode.Multiple;
		}

		/// <summary>The facade selection into the collection view.</summary>
		internal override void ApplySelection()
		{
			if (!(Collection is XF.CollectionView view))
				return;

			_syncingSelection = true;
			try
			{
				if (SelectionMode == SelectionMode.Single)
				{
					var index = SelectedIndex;
					view.SelectedItem = index >= 0 && index < _boxes.Count ? _boxes[index] : null;
				}
				else
				{
					var selected = _boxes.Where(b => _selectedItems.Contains(b.Item)).Cast<object>().ToList();
					view.SelectedItems = selected;
				}
			}
			finally
			{
				_syncingSelection = false;
			}
		}

		void OnNativeSelectionChanged(object sender, XF.SelectionChangedEventArgs e)
		{
			if (_syncingSelection)
				return;

			if (SelectionMode == SelectionMode.Single)
			{
				var box = e.CurrentSelection.FirstOrDefault() as ItemBox;
				Select(box == null ? -1 : _boxes.IndexOf(box), true);
				return;
			}

			_syncingSelection = true;
			try
			{
				var now = e.CurrentSelection.OfType<ItemBox>().Select(b => b.Item).ToList();
				foreach (var removed in _selectedItems.Except(now).ToList())
					_selectedItems.Remove(removed);
				foreach (var added in now.Except(_selectedItems).ToList())
					_selectedItems.Add(added);
			}
			finally
			{
				_syncingSelection = false;
			}
		}

		/// <summary>
		/// The selected items changed: the first of them is the selected item, each item that is a container knows,
		/// and SelectionChanged reports what came and went.
		/// </summary>
		void OnSelectedItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// Diffed against the last state: a Clear is a Reset, which names no items.
			var current = _selectedItems.ToList();
			var removed = _lastSelected.Where(i => !current.Contains(i)).ToArray();
			var added = current.Where(i => !_lastSelected.Contains(i)).ToArray();
			_lastSelected = current;

			foreach (var item in removed)
				(item as ListBoxItem)?.SetSelectedFromSelector(false);
			foreach (var item in added)
				(item as ListBoxItem)?.SetSelectedFromSelector(true);

			if (_updatingItems || removed.Length == 0 && added.Length == 0)
				return;

			if (SelectionMode == SelectionMode.Single)
			{
				// One item at most: the one just added, or none.
				if (!_syncingSelection)
					Select(added.Length > 0 ? Items.IndexOf(added[added.Length - 1]) : -1, false);
				return;
			}

			// The view already shows it when the change came from there.
			if (!_syncingSelection && HasNativeView)
				ApplySelection();

			// SelectedIndex and SelectedItem follow the first selected item, without a second event.
			var first = current.FirstOrDefault();
			SetSelectionQuietly(first == null ? -1 : Items.IndexOf(first), first);
			base.OnSelectionChanged(new SelectionChangedEventArgs(SelectionChangedEvent, removed, added));
		}

		List<object> _lastSelected = new List<object>();

		bool _updatingItems;

		/// <summary>
		/// A selection made through SelectedIndex or SelectedItem replaces the selected items with that one item -
		/// in every mode, as WPF does.
		/// </summary>
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			_updatingItems = true;
			try
			{
				_selectedItems.Clear();
				if (SelectedItem != null)
					_selectedItems.Add(SelectedItem);
			}
			finally
			{
				_updatingItems = false;
			}

			if (SelectionMode != SelectionMode.Single && HasNativeView && !FromNative)
				ApplySelection();

			base.OnSelectionChanged(e);
		}
	}
}
