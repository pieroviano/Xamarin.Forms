using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows.Controls.Primitives
{
	public abstract class MultiSelector : Selector
	{
		readonly ObservableCollection<object> _selectedItems = new ObservableCollection<object>();

		public IList SelectedItems => _selectedItems;

		protected bool CanSelectMultipleItems { get; set; } = true;

		public void SelectAll()
		{
			foreach (var item in Items)
			{
				if (!_selectedItems.Contains(item))
					_selectedItems.Add(item);
			}
		}

		public void UnselectAll() => _selectedItems.Clear();
	}
}

namespace System.Windows.Controls
{
	[Flags]
	public enum DataGridHeadersVisibility
	{
		None = 0,
		Column = 1,
		Row = 2,
		All = Column | Row,
	}

	public enum DataGridSelectionUnit
	{
		Cell,
		FullRow,
		CellOrRowHeader,
	}

	public enum DataGridSelectionMode
	{
		Single,
		Extended,
	}

	[Flags]
	public enum DataGridGridLinesVisibility
	{
		None = 0,
		Horizontal = 1,
		Vertical = 2,
		All = Horizontal | Vertical,
	}

	public enum DataGridLengthUnitType
	{
		Auto,
		Pixel,
		SizeToCells,
		SizeToHeader,
		Star,
	}

	public struct DataGridLength : IEquatable<DataGridLength>
	{
		public DataGridLength(double pixels) : this(pixels, DataGridLengthUnitType.Pixel)
		{
		}

		public DataGridLength(double value, DataGridLengthUnitType type)
		{
			Value = type == DataGridLengthUnitType.Auto || type == DataGridLengthUnitType.SizeToCells || type == DataGridLengthUnitType.SizeToHeader ? 1 : value;
			UnitType = type;
			DesiredValue = double.NaN;
			DisplayValue = double.NaN;
		}

		public static DataGridLength Auto => new DataGridLength(1, DataGridLengthUnitType.Auto);

		public static DataGridLength SizeToCells => new DataGridLength(1, DataGridLengthUnitType.SizeToCells);

		public static DataGridLength SizeToHeader => new DataGridLength(1, DataGridLengthUnitType.SizeToHeader);

		public double Value { get; }

		public DataGridLengthUnitType UnitType { get; }

		public double DesiredValue { get; }

		public double DisplayValue { get; }

		public bool IsAbsolute => UnitType == DataGridLengthUnitType.Pixel;

		public bool IsAuto => UnitType == DataGridLengthUnitType.Auto;

		public bool IsStar => UnitType == DataGridLengthUnitType.Star;

		public bool IsSizeToCells => UnitType == DataGridLengthUnitType.SizeToCells;

		public bool IsSizeToHeader => UnitType == DataGridLengthUnitType.SizeToHeader;

		public static implicit operator DataGridLength(double value) => new DataGridLength(value);

		public bool Equals(DataGridLength other) => Value.Equals(other.Value) && UnitType == other.UnitType;

		public override bool Equals(object obj) => obj is DataGridLength other && Equals(other);

		public override int GetHashCode() => Value.GetHashCode() ^ (int)UnitType;

		public static bool operator ==(DataGridLength a, DataGridLength b) => a.Equals(b);

		public static bool operator !=(DataGridLength a, DataGridLength b) => !a.Equals(b);

		internal XF.GridLength ToForms() =>
			IsAbsolute ? new XF.GridLength(Value, XF.GridUnitType.Absolute) : IsStar ? new XF.GridLength(Value, XF.GridUnitType.Star) : XF.GridLength.Auto;
	}

	/// <summary>A cell: the item of its row and its column.</summary>
	public struct DataGridCellInfo : IEquatable<DataGridCellInfo>
	{
		public DataGridCellInfo(object item, DataGridColumn column)
		{
			Item = item;
			Column = column ?? throw new ArgumentNullException(nameof(column));
		}

		public DataGridCellInfo(DataGridCell cell) : this(cell.RowItem, cell.Column)
		{
		}

		public object Item { get; }

		public DataGridColumn Column { get; }

		public bool IsValid => Column != null && Item != null;

		public bool Equals(DataGridCellInfo other) => Equals(Item, other.Item) && ReferenceEquals(Column, other.Column);

		public override bool Equals(object obj) => obj is DataGridCellInfo other && Equals(other);

		public override int GetHashCode() => (Item?.GetHashCode() ?? 0) ^ (Column?.GetHashCode() ?? 0);

		public static bool operator ==(DataGridCellInfo a, DataGridCellInfo b) => a.Equals(b);

		public static bool operator !=(DataGridCellInfo a, DataGridCellInfo b) => !a.Equals(b);
	}

	public class SelectedCellsChangedEventArgs : EventArgs
	{
		public SelectedCellsChangedEventArgs(List<DataGridCellInfo> addedCells, List<DataGridCellInfo> removedCells)
		{
			AddedCells = addedCells ?? throw new ArgumentNullException(nameof(addedCells));
			RemovedCells = removedCells ?? throw new ArgumentNullException(nameof(removedCells));
		}

		public IList<DataGridCellInfo> AddedCells { get; }

		public IList<DataGridCellInfo> RemovedCells { get; }
	}

	public delegate void SelectedCellsChangedEventHandler(object sender, SelectedCellsChangedEventArgs e);

	public class DataGridRowEventArgs : EventArgs
	{
		public DataGridRowEventArgs(DataGridRow row) => Row = row;

		public DataGridRow Row { get; }
	}

	/// <summary>A column: its width, header and cell style; each kind of column makes its cells' content.</summary>
	public abstract class DataGridColumn : DependencyObject
	{
		public static readonly XF.BindableProperty HeaderProperty = Dp.Register<DataGridColumn>(nameof(Header), typeof(object), null);
		public static readonly XF.BindableProperty WidthProperty = Dp.Register<DataGridColumn>(nameof(Width), typeof(DataGridLength), DataGridLength.SizeToHeader);
		public static readonly XF.BindableProperty MinWidthProperty = Dp.Register<DataGridColumn>(nameof(MinWidth), typeof(double), 20.0);
		public static readonly XF.BindableProperty MaxWidthProperty = Dp.Register<DataGridColumn>(nameof(MaxWidth), typeof(double), double.PositiveInfinity);
		public static readonly XF.BindableProperty VisibilityProperty = Dp.Register<DataGridColumn>(nameof(Visibility), typeof(Visibility), Visibility.Visible);
		public static readonly XF.BindableProperty CellStyleProperty = Dp.Register<DataGridColumn>(nameof(CellStyle), typeof(Style), null);
		public static readonly XF.BindableProperty IsReadOnlyProperty = Dp.Register<DataGridColumn>(nameof(IsReadOnly), typeof(bool), false);
		public static readonly XF.BindableProperty DisplayIndexProperty = Dp.Register<DataGridColumn>(nameof(DisplayIndex), typeof(int), -1);
		public static readonly XF.BindableProperty CanUserSortProperty = Dp.Register<DataGridColumn>(nameof(CanUserSort), typeof(bool), true);
		public static readonly XF.BindableProperty CanUserResizeProperty = Dp.Register<DataGridColumn>(nameof(CanUserResize), typeof(bool), true);

		public object Header
		{
			get => Get<object>(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public DataGridLength Width
		{
			get => Get<DataGridLength>(WidthProperty);
			set => SetValue(WidthProperty, value);
		}

		public double MinWidth
		{
			get => Get<double>(MinWidthProperty);
			set => SetValue(MinWidthProperty, value);
		}

		public double MaxWidth
		{
			get => Get<double>(MaxWidthProperty);
			set => SetValue(MaxWidthProperty, value);
		}

		public Visibility Visibility
		{
			get => Get<Visibility>(VisibilityProperty);
			set => SetValue(VisibilityProperty, value);
		}

		public Style CellStyle
		{
			get => Get<Style>(CellStyleProperty);
			set => SetValue(CellStyleProperty, value);
		}

		public bool IsReadOnly
		{
			get => Get<bool>(IsReadOnlyProperty);
			set => SetValue(IsReadOnlyProperty, value);
		}

		public int DisplayIndex
		{
			get => DataGridOwner == null ? Get<int>(DisplayIndexProperty) : DataGridOwner.Columns.IndexOf(this);
			set => SetValue(DisplayIndexProperty, value);
		}

		public bool CanUserSort
		{
			get => Get<bool>(CanUserSortProperty);
			set => SetValue(CanUserSortProperty, value);
		}

		public bool CanUserResize
		{
			get => Get<bool>(CanUserResizeProperty);
			set => SetValue(CanUserResizeProperty, value);
		}

		/// <summary>The width the column takes on screen: its absolute width, else what its widest cell asks for.</summary>
		public double ActualWidth
		{
			get
			{
				if (Visibility != Visibility.Visible)
					return 0;
				if (Width.IsAbsolute)
					return Math.Max(MinWidth, Width.Value);

				var measured = DataGridOwner?.MeasureColumn(this) ?? 0;
				return Math.Max(MinWidth, measured);
			}
		}

		protected internal DataGrid DataGridOwner { get; internal set; }

		/// <summary>The content of <paramref name="row"/>'s cell in this column.</summary>
		public FrameworkElement GetCellContent(DataGridRow row) => row?.CellFor(this)?.Content as FrameworkElement;

		public FrameworkElement GetCellContent(object dataItem) =>
			GetCellContent(DataGridOwner?.ItemContainerGenerator.ContainerFromItem(dataItem) as DataGridRow);

		protected abstract FrameworkElement GenerateElement(DataGridCell cell, object dataItem);

		/// <summary>Fills a cell's existing content with <paramref name="dataItem"/>'s value again (the row changed).</summary>
		internal virtual void Refresh(FrameworkElement content, object dataItem)
		{
		}

		internal FrameworkElement Generate(DataGridCell cell, object dataItem) => GenerateElement(cell, dataItem);

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			DataGridOwner?.OnColumnChanged(this, e.Property.Bindable);
		}
	}

	public abstract class DataGridBoundColumn : DataGridColumn
	{
		public static readonly XF.BindableProperty ElementStyleProperty = Dp.Register<DataGridBoundColumn>(nameof(ElementStyle), typeof(Style), null);
		public static readonly XF.BindableProperty EditingElementStyleProperty = Dp.Register<DataGridBoundColumn>(nameof(EditingElementStyle), typeof(Style), null);

		BindingBase _binding;

		public virtual BindingBase Binding
		{
			get => _binding;
			set
			{
				_binding = value;
				DataGridOwner?.OnColumnChanged(this, null);
			}
		}

		public Style ElementStyle
		{
			get => Get<Style>(ElementStyleProperty);
			set => SetValue(ElementStyleProperty, value);
		}

		public Style EditingElementStyle
		{
			get => Get<Style>(EditingElementStyleProperty);
			set => SetValue(EditingElementStyleProperty, value);
		}

		/// <summary>The value the binding reads from an item: its path, formatted.</summary>
		internal string ValueText(object dataItem)
		{
			var binding = _binding as Binding;
			var value = ItemsControl.PropertyOf(dataItem, binding?.Path?.Path);
			if (value == DBNull.Value)
				value = null;
			if (binding?.Converter != null)
				value = binding.Converter.Convert(value, typeof(string), binding.ConverterParameter, binding.ConverterCulture ?? CultureInfo.CurrentCulture);
			return string.IsNullOrEmpty(binding?.StringFormat)
				? Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty
				: string.Format(CultureInfo.CurrentCulture, binding.StringFormat, value);
		}
	}

	public class DataGridTextColumn : DataGridBoundColumn
	{
		public static readonly XF.BindableProperty FontFamilyProperty = Documents.TextElement.FontFamilyProperty;
		public static readonly XF.BindableProperty FontSizeProperty = Documents.TextElement.FontSizeProperty;
		public static readonly XF.BindableProperty FontWeightProperty = Documents.TextElement.FontWeightProperty;
		public static readonly XF.BindableProperty ForegroundProperty = Documents.TextElement.ForegroundProperty;

		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
			var text = new TextBlock { Text = ValueText(dataItem), Padding = new Thickness(2, 0, 2, 0) };
			if (ElementStyle != null)
				text.Style = ElementStyle;
			return text;
		}

		internal override void Refresh(FrameworkElement content, object dataItem)
		{
			if (content is TextBlock text)
			{
				text.Text = ValueText(dataItem);
				text.Style = ElementStyle;
			}
		}
	}

	/// <summary>A row: the container of an item, with a cell per column.</summary>
	public class DataGridRow : Control
	{
		readonly List<DataGridCell> _cells = new List<DataGridCell>();

		public static readonly XF.BindableProperty HeaderProperty = Dp.Register<DataGridRow>(nameof(Header), typeof(object), null);
		public static readonly XF.BindableProperty ItemProperty = Dp.Register<DataGridRow>(nameof(Item), typeof(object), null);
		public static readonly XF.BindableProperty IsSelectedProperty = Dp.Register<DataGridRow>(nameof(IsSelected), typeof(bool), false);

		public DataGridRow() => SetValue(FocusableProperty, false);

		public object Header
		{
			get => Get<object>(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public object Item
		{
			get => Get<object>(ItemProperty);
			set => SetValue(ItemProperty, value);
		}

		public bool IsSelected
		{
			get => Get<bool>(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		internal DataGrid DataGridOwner { get; set; }

		internal IReadOnlyList<DataGridCell> Cells => _cells;

		/// <summary>The index of the row's item, -1 when it is no longer in the grid.</summary>
		public int GetIndex() => DataGridOwner?.Items.IndexOf(Item) ?? -1;

		public static DataGridRow GetRowContainingElement(FrameworkElement element)
		{
			for (DependencyObject d = element; d != null; d = d.LogicalParent)
			{
				if (d is DataGridRow row)
					return row;
			}

			return null;
		}

		internal override IEnumerable LogicalChildrenCore => _cells.ToArray();

		internal DataGridCell CellFor(DataGridColumn column) => _cells.FirstOrDefault(c => c.Column == column);

		/// <summary>A cell per column of the grid, made anew.</summary>
		internal void BuildCells()
		{
			foreach (var cell in _cells)
				RemoveLogicalChild(cell);
			_cells.Clear();

			foreach (var column in DataGridOwner.Columns)
			{
				var cell = new DataGridCell { Column = column, RowItem = Item };
				_cells.Add(cell);
				AddLogicalChild(cell);
				cell.Content = column.Generate(cell, Item);
				if (column.CellStyle != null)
					cell.Style = column.CellStyle;
			}
		}

		internal void RefreshCells()
		{
			foreach (var cell in _cells)
			{
				cell.RowItem = Item;
				cell.Column.Refresh(cell.Content as FrameworkElement, Item);
			}
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (p == HeightProperty || p == VisibilityProperty || p == MinHeightProperty)
				DataGridOwner?.OnRowLayoutChanged(this);
			else if (p == BackgroundProperty)
			{
				foreach (var cell in _cells)
					cell.ApplyCellBackground();
			}
		}

		/// <summary>A row draws nothing itself: its cells are placed in the grid's layout.</summary>
		internal override XF.View CreateNativeView() => new XF.ContentView { IsVisible = false };
	}

	/// <summary>A cell: its content (the column's element), colors, and whether it is selected.</summary>
	public class DataGridCell : ContentControl
	{
		public static readonly XF.BindableProperty IsSelectedProperty = Dp.Register<DataGridCell>(nameof(IsSelected), typeof(bool), false);
		public static readonly XF.BindableProperty IsReadOnlyProperty = Dp.Register<DataGridCell>(nameof(IsReadOnly), typeof(bool), true);
		public static readonly XF.BindableProperty IsEditingProperty = Dp.Register<DataGridCell>(nameof(IsEditing), typeof(bool), false);

		public DataGridCell()
		{
			SetValue(FocusableProperty, false);
			SetValue(IsTabStopProperty, false);
		}

		public DataGridColumn Column { get; internal set; }

		internal object RowItem { get; set; }

		public bool IsSelected
		{
			get => Get<bool>(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		public bool IsReadOnly => Get<bool>(IsReadOnlyProperty);

		public bool IsEditing
		{
			get => Get<bool>(IsEditingProperty);
			set => SetValue(IsEditingProperty, value);
		}

		internal DataGridRow Row => LogicalParent as DataGridRow;

		internal override void OnNativeCreated()
		{
			base.OnNativeCreated();
			var tap = new XF.TapGestureRecognizer();
			tap.Tapped += (s, e) => Row?.DataGridOwner?.OnCellClicked(this);
			NativeView.GestureRecognizers.Add(tap);
		}

		internal override void ApplyBackground() => ApplyCellBackground();

		/// <summary>Selected, the highlight; else the cell's background, else its row's, else the window color.</summary>
		internal void ApplyCellBackground()
		{
			if (!HasNativeView)
				return;

			XF.Color color;
			if (IsSelected)
				color = SystemColors.HighlightBrush.ToFormsColor();
			else
				color = (Background ?? Row?.Background ?? SystemColors.WindowBrush).ToFormsColor();

			NativeView.BackgroundColor = color;
			if (Content is TextBlock text && text.HasNativeView)
			{
				var label = (XF.Label)text.NativeView;
				if (IsSelected)
					label.TextColor = SystemColors.HighlightTextBrush.ToFormsColor();
				else
					NativeText.ApplyForeground(label, text);
			}
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property.Bindable == IsSelectedProperty || e.Property.Bindable == ForegroundProperty)
				ApplyCellBackground();
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplyCellBackground();
		}
	}

	/// <summary>
	/// A grid of rows and columns: a row per item, a cell per column, laid out by a Xamarin.Forms grid (one-pixel
	/// gaps over a colored background draw the grid lines) in a scroll view. Every row is realized: containers are
	/// always there for <see cref="ItemContainerGenerator"/>.
	/// </summary>
	public class DataGrid : MultiSelector
	{
		readonly List<DataGridRow> _rows = new List<DataGridRow>();
		readonly ObservableCollection<DataGridColumn> _columns = new ObservableCollection<DataGridColumn>();
		readonly SelectedCellsCollection _selectedCells;
		XF.Grid _grid;
		XF.ScrollView _scroll;
		bool _rowsValid;
		DataGridCellInfo _currentCell;

		public static readonly XF.BindableProperty AutoGenerateColumnsProperty = Dp.Register<DataGrid>(nameof(AutoGenerateColumns), typeof(bool), true);
		public static readonly XF.BindableProperty HeadersVisibilityProperty = Dp.Register<DataGrid>(nameof(HeadersVisibility), typeof(DataGridHeadersVisibility), DataGridHeadersVisibility.All);
		public static readonly XF.BindableProperty CanUserAddRowsProperty = Dp.Register<DataGrid>(nameof(CanUserAddRows), typeof(bool), true);
		public static readonly XF.BindableProperty CanUserDeleteRowsProperty = Dp.Register<DataGrid>(nameof(CanUserDeleteRows), typeof(bool), true);
		public static readonly XF.BindableProperty CanUserSortColumnsProperty = Dp.Register<DataGrid>(nameof(CanUserSortColumns), typeof(bool), true);
		public static readonly XF.BindableProperty CanUserReorderColumnsProperty = Dp.Register<DataGrid>(nameof(CanUserReorderColumns), typeof(bool), true);
		public static readonly XF.BindableProperty CanUserResizeRowsProperty = Dp.Register<DataGrid>(nameof(CanUserResizeRows), typeof(bool), true);
		public static readonly XF.BindableProperty CanUserResizeColumnsProperty = Dp.Register<DataGrid>(nameof(CanUserResizeColumns), typeof(bool), true);
		public static readonly XF.BindableProperty IsReadOnlyProperty = Dp.Register<DataGrid>(nameof(IsReadOnly), typeof(bool), false);
		public static readonly XF.BindableProperty SelectionUnitProperty = Dp.Register<DataGrid>(nameof(SelectionUnit), typeof(DataGridSelectionUnit), DataGridSelectionUnit.FullRow);
		public static readonly XF.BindableProperty SelectionModeProperty = Dp.Register<DataGrid>(nameof(SelectionMode), typeof(DataGridSelectionMode), DataGridSelectionMode.Extended);
		public static readonly XF.BindableProperty RowHeightProperty = Dp.Register<DataGrid>(nameof(RowHeight), typeof(double), double.NaN);
		public static readonly XF.BindableProperty ColumnHeaderHeightProperty = Dp.Register<DataGrid>(nameof(ColumnHeaderHeight), typeof(double), double.NaN);
		public static readonly XF.BindableProperty FrozenColumnCountProperty = Dp.Register<DataGrid>(nameof(FrozenColumnCount), typeof(int), 0);
		public static readonly XF.BindableProperty GridLinesVisibilityProperty = Dp.Register<DataGrid>(nameof(GridLinesVisibility), typeof(DataGridGridLinesVisibility), DataGridGridLinesVisibility.All);
		public static readonly XF.BindableProperty HorizontalGridLinesBrushProperty = Dp.Register<DataGrid>(nameof(HorizontalGridLinesBrush), typeof(Brush), Brushes.Silver);
		public static readonly XF.BindableProperty VerticalGridLinesBrushProperty = Dp.Register<DataGrid>(nameof(VerticalGridLinesBrush), typeof(Brush), Brushes.Silver);
		public static readonly XF.BindableProperty RowBackgroundProperty = Dp.Register<DataGrid>(nameof(RowBackground), typeof(Brush), null);

		public DataGrid()
		{
			_selectedCells = new SelectedCellsCollection(this);
			_columns.CollectionChanged += OnColumnsChanged;
			SetValue(BorderThicknessProperty, new Thickness(1));
		}

		public ObservableCollection<DataGridColumn> Columns => _columns;

		public IList<DataGridCellInfo> SelectedCells => _selectedCells;

		public bool AutoGenerateColumns
		{
			get => Get<bool>(AutoGenerateColumnsProperty);
			set => SetValue(AutoGenerateColumnsProperty, value);
		}

		public DataGridHeadersVisibility HeadersVisibility
		{
			get => Get<DataGridHeadersVisibility>(HeadersVisibilityProperty);
			set => SetValue(HeadersVisibilityProperty, value);
		}

		public bool CanUserAddRows
		{
			get => Get<bool>(CanUserAddRowsProperty);
			set => SetValue(CanUserAddRowsProperty, value);
		}

		public bool CanUserDeleteRows
		{
			get => Get<bool>(CanUserDeleteRowsProperty);
			set => SetValue(CanUserDeleteRowsProperty, value);
		}

		public bool CanUserSortColumns
		{
			get => Get<bool>(CanUserSortColumnsProperty);
			set => SetValue(CanUserSortColumnsProperty, value);
		}

		public bool CanUserReorderColumns
		{
			get => Get<bool>(CanUserReorderColumnsProperty);
			set => SetValue(CanUserReorderColumnsProperty, value);
		}

		public bool CanUserResizeRows
		{
			get => Get<bool>(CanUserResizeRowsProperty);
			set => SetValue(CanUserResizeRowsProperty, value);
		}

		public bool CanUserResizeColumns
		{
			get => Get<bool>(CanUserResizeColumnsProperty);
			set => SetValue(CanUserResizeColumnsProperty, value);
		}

		public bool IsReadOnly
		{
			get => Get<bool>(IsReadOnlyProperty);
			set => SetValue(IsReadOnlyProperty, value);
		}

		public DataGridSelectionUnit SelectionUnit
		{
			get => Get<DataGridSelectionUnit>(SelectionUnitProperty);
			set => SetValue(SelectionUnitProperty, value);
		}

		public DataGridSelectionMode SelectionMode
		{
			get => Get<DataGridSelectionMode>(SelectionModeProperty);
			set => SetValue(SelectionModeProperty, value);
		}

		/// <summary>The height of every row that sets none (NaN: as tall as its content).</summary>
		public double RowHeight
		{
			get => Get<double>(RowHeightProperty);
			set => SetValue(RowHeightProperty, value);
		}

		public double ColumnHeaderHeight
		{
			get => Get<double>(ColumnHeaderHeightProperty);
			set => SetValue(ColumnHeaderHeightProperty, value);
		}

		/// <summary>Kept: the columns scroll together here.</summary>
		public int FrozenColumnCount
		{
			get => Get<int>(FrozenColumnCountProperty);
			set => SetValue(FrozenColumnCountProperty, value);
		}

		public DataGridGridLinesVisibility GridLinesVisibility
		{
			get => Get<DataGridGridLinesVisibility>(GridLinesVisibilityProperty);
			set => SetValue(GridLinesVisibilityProperty, value);
		}

		public Brush HorizontalGridLinesBrush
		{
			get => Get<Brush>(HorizontalGridLinesBrushProperty);
			set => SetValue(HorizontalGridLinesBrushProperty, value);
		}

		public Brush VerticalGridLinesBrush
		{
			get => Get<Brush>(VerticalGridLinesBrushProperty);
			set => SetValue(VerticalGridLinesBrushProperty, value);
		}

		public Brush RowBackground
		{
			get => Get<Brush>(RowBackgroundProperty);
			set => SetValue(RowBackgroundProperty, value);
		}

		/// <summary>The current cell; setting it moves it and raises <see cref="CurrentCellChanged"/>.</summary>
		public DataGridCellInfo CurrentCell
		{
			get => _currentCell;
			set
			{
				if (_currentCell == value)
					return;

				_currentCell = value;
				OnCurrentCellChanged(EventArgs.Empty);
			}
		}

		public object CurrentItem => _currentCell.Item;

		public DataGridColumn CurrentColumn => _currentCell.Column;

		public event EventHandler<DataGridRowEventArgs> LoadingRow;

		public event EventHandler<DataGridRowEventArgs> UnloadingRow;

		public event EventHandler<EventArgs> CurrentCellChanged;

		public event SelectedCellsChangedEventHandler SelectedCellsChanged;

		protected virtual void OnLoadingRow(DataGridRowEventArgs e) => LoadingRow?.Invoke(this, e);

		protected virtual void OnUnloadingRow(DataGridRowEventArgs e) => UnloadingRow?.Invoke(this, e);

		protected virtual void OnCurrentCellChanged(EventArgs e) => CurrentCellChanged?.Invoke(this, e);

		protected virtual void OnSelectedCellsChanged(SelectedCellsChangedEventArgs e) => SelectedCellsChanged?.Invoke(this, e);

		public void ScrollIntoView(object item) => ScrollIntoView(item, null);

		public void ScrollIntoView(object item, DataGridColumn column)
		{
			if (_scroll == null)
				return;

			var row = ContainerFor(Items.IndexOf(item)) as DataGridRow;
			var cell = column == null ? row?.Cells.FirstOrDefault() : row?.CellFor(column);
			if (cell != null && cell.HasNativeView)
				_ = _scroll.ScrollToAsync(cell.NativeView, XF.ScrollToPosition.MakeVisible, false);
		}

		public void UnselectAllCells() => _selectedCells.Clear();

		public void SelectAllCells()
		{
			_selectedCells.Replace(_rows.SelectMany(r => r.Cells.Select(c => new DataGridCellInfo(r.Item, c.Column))).ToList());
		}

		// ---- rows ----------------------------------------------------------------------------------------------------

		internal override DependencyObject ContainerFor(int index)
		{
			EnsureRows();
			return index >= 0 && index < _rows.Count ? _rows[index] : null;
		}

		internal override IEnumerable LogicalChildrenCore => _rows.ToArray();

		/// <summary>A row per item, each with its cells, made (and announced by LoadingRow) the first time they are needed.</summary>
		void EnsureRows()
		{
			if (_rowsValid)
				return;

			_rowsValid = true;
			foreach (var row in _rows)
			{
				OnUnloadingRow(new DataGridRowEventArgs(row));
				RemoveLogicalChild(row);
			}

			_rows.Clear();
			foreach (var item in Items)
				_rows.Add(CreateRow(item));
			foreach (var row in _rows.ToArray())
				OnLoadingRow(new DataGridRowEventArgs(row));
		}

		DataGridRow CreateRow(object item)
		{
			var row = new DataGridRow { DataGridOwner = this, Item = item };
			AddLogicalChild(row);
			row.BuildCells();
			return row;
		}

		protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);
			if (!_rowsValid)
			{
				if (_grid != null)
					Relayout();
				return;
			}

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Replace when e.NewStartingIndex >= 0 && e.NewStartingIndex < _rows.Count:
					var changed = _rows[e.NewStartingIndex];
					changed.Item = Items[e.NewStartingIndex];
					changed.RefreshCells();
					return;
				case NotifyCollectionChangedAction.Add when e.NewStartingIndex >= 0 && e.NewStartingIndex <= _rows.Count:
					var added = CreateRow(Items[e.NewStartingIndex]);
					_rows.Insert(e.NewStartingIndex, added);
					OnLoadingRow(new DataGridRowEventArgs(added));
					break;
				case NotifyCollectionChangedAction.Remove when e.OldStartingIndex >= 0 && e.OldStartingIndex < _rows.Count:
					var removed = _rows[e.OldStartingIndex];
					_rows.RemoveAt(e.OldStartingIndex);
					OnUnloadingRow(new DataGridRowEventArgs(removed));
					RemoveLogicalChild(removed);
					break;
				default:
					_rowsValid = false;
					break;
			}

			if (_grid != null)
				Relayout();
		}

		void OnColumnsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			foreach (DataGridColumn column in (IEnumerable)e.OldItems ?? Array.Empty<object>())
				column.DataGridOwner = null;
			foreach (DataGridColumn column in (IEnumerable)e.NewItems ?? Array.Empty<object>())
				column.DataGridOwner = this;
			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				foreach (var column in _columns)
					column.DataGridOwner = this;
			}

			// Cells follow the columns: every row makes its cells again.
			foreach (var row in _rows)
				row.BuildCells();

			if (_grid != null)
				Relayout();
		}

		/// <summary>A column's width or visibility, style or binding changed.</summary>
		internal void OnColumnChanged(DataGridColumn column, XF.BindableProperty property)
		{
			if (property == DataGridColumn.CellStyleProperty)
			{
				foreach (var row in _rows)
				{
					var cell = row.CellFor(column);
					if (cell != null)
						cell.Style = column.CellStyle;
				}

				return;
			}

			if (property == null || property == DataGridBoundColumn.ElementStyleProperty)
			{
				foreach (var row in _rows)
				{
					var cell = row.CellFor(column);
					if (cell != null)
						column.Refresh(cell.Content as FrameworkElement, row.Item);
				}

				return;
			}

			if (_grid != null && (property == DataGridColumn.WidthProperty || property == DataGridColumn.VisibilityProperty || property == DataGridColumn.MinWidthProperty))
				ApplyColumnDefinition(column);
		}

		internal void OnRowLayoutChanged(DataGridRow row)
		{
			if (_grid != null)
				ApplyRowDefinition(row);
		}

		/// <summary>The width of the column's widest cell content.</summary>
		internal double MeasureColumn(DataGridColumn column)
		{
			double width = 0;
			foreach (var row in _rows)
			{
				if (row.CellFor(column)?.Content is FrameworkElement content)
					width = Math.Max(width, content.DesiredSize.Width);
			}

			return width;
		}

		// ---- the Xamarin.Forms view ----------------------------------------------------------------------------------

		int HeaderRows => (HeadersVisibility & DataGridHeadersVisibility.Column) != 0 ? 1 : 0;

		internal override XF.View CreateNativeView()
		{
			_grid = new XF.Grid { RowSpacing = 1, ColumnSpacing = 1, HorizontalOptions = XF.LayoutOptions.Start, VerticalOptions = XF.LayoutOptions.Start };
			_scroll = new XF.ScrollView { Orientation = XF.ScrollOrientation.Both, Content = _grid };
			return new NativeShell(_scroll);
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			Relayout();
		}

		internal override void ApplyItems(NotifyCollectionChangedEventArgs e)
		{
		}

		/// <summary>Every cell's view into the grid, with a definition per column and row.</summary>
		void Relayout()
		{
			EnsureRows();
			_grid.Children.Clear();
			_grid.ColumnDefinitions.Clear();
			_grid.RowDefinitions.Clear();
			_grid.BackgroundColor = (VerticalGridLinesBrush ?? Brushes.Silver).ToFormsColor();

			foreach (var column in _columns)
				_grid.ColumnDefinitions.Add(new XF.ColumnDefinition());
			foreach (var column in _columns)
				ApplyColumnDefinition(column);

			if (HeaderRows > 0)
			{
				_grid.RowDefinitions.Add(new XF.RowDefinition { Height = double.IsNaN(ColumnHeaderHeight) ? XF.GridLength.Auto : new XF.GridLength(ColumnHeaderHeight) });
				for (var c = 0; c < _columns.Count; c++)
				{
					var header = new XF.Label
					{
						Text = _columns[c].Header?.ToString() ?? string.Empty,
						BackgroundColor = SystemColors.ControlBrush.ToFormsColor(),
						Padding = new XF.Thickness(2, 1),
						FontAttributes = XF.FontAttributes.Bold,
					};
					NativeText.ApplyFont(header, this);
					_grid.Children.Add(header, c, 0);
				}
			}

			for (var r = 0; r < _rows.Count; r++)
			{
				var row = _rows[r];
				_grid.RowDefinitions.Add(new XF.RowDefinition());
				ApplyRowDefinition(row);
				for (var c = 0; c < row.Cells.Count; c++)
				{
					var view = row.Cells[c].NativeView;
					if (view.Parent is XF.Layout<XF.View> previous)
						previous.Children.Remove(view);
					_grid.Children.Add(view, c, r + HeaderRows);
				}
			}
		}

		void ApplyColumnDefinition(DataGridColumn column)
		{
			var index = _columns.IndexOf(column);
			if (index < 0 || index >= _grid.ColumnDefinitions.Count)
				return;

			var width = column.Visibility != Visibility.Visible ? new XF.GridLength(0) : column.Width.ToForms();
			if (width.IsAbsolute && column.Visibility == Visibility.Visible)
				width = new XF.GridLength(Math.Max(width.Value, column.MinWidth));
			_grid.ColumnDefinitions[index].Width = width;
			foreach (var row in _rows)
			{
				var cell = row.CellFor(column);
				if (cell != null && cell.HasNativeView)
					cell.NativeView.IsVisible = column.Visibility == Visibility.Visible;
			}
		}

		void ApplyRowDefinition(DataGridRow row)
		{
			var index = _rows.IndexOf(row) + HeaderRows;
			if (index < HeaderRows || index >= _grid.RowDefinitions.Count)
				return;

			var visible = row.Visibility == Visibility.Visible;
			var height = !double.IsNaN(row.Height) ? row.Height : RowHeight;
			_grid.RowDefinitions[index].Height = !visible ? new XF.GridLength(0) : double.IsNaN(height) ? XF.GridLength.Auto : new XF.GridLength(height);
			foreach (var cell in row.Cells)
			{
				if (cell.HasNativeView)
					cell.NativeView.IsVisible = visible && cell.Column.Visibility == Visibility.Visible;
			}
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (_grid == null)
				return;

			if (p == RowHeightProperty)
			{
				foreach (var row in _rows)
					ApplyRowDefinition(row);
			}
			else if (p == HeadersVisibilityProperty || p == ColumnHeaderHeightProperty || p == VerticalGridLinesBrushProperty)
			{
				Relayout();
			}
		}

		// ---- selection -----------------------------------------------------------------------------------------------

		/// <summary>A click on a cell makes it current and (cell selection) the selection.</summary>
		internal void OnCellClicked(DataGridCell cell)
		{
			var row = cell.Row;
			if (row == null || !IsEnabled)
				return;

			var info = new DataGridCellInfo(row.Item, cell.Column);
			CurrentCell = info;
			if (SelectionUnit == DataGridSelectionUnit.FullRow)
				_selectedCells.Replace(row.Cells.Select(c => new DataGridCellInfo(row.Item, c.Column)).ToList());
			else
				_selectedCells.Replace(new List<DataGridCellInfo> { info });
		}

		internal void OnSelectedCellsChangedInternal(List<DataGridCellInfo> added, List<DataGridCellInfo> removed)
		{
			foreach (var info in removed)
				CellOf(info)?.SetValue(DataGridCell.IsSelectedProperty, false);
			foreach (var info in added)
				CellOf(info)?.SetValue(DataGridCell.IsSelectedProperty, true);
			OnSelectedCellsChanged(new SelectedCellsChangedEventArgs(added, removed));
		}

		DataGridCell CellOf(DataGridCellInfo info)
		{
			if (!info.IsValid)
				return null;

			var index = Items.IndexOf(info.Item);
			return (ContainerFor(index) as DataGridRow)?.CellFor(info.Column);
		}

		/// <summary>The selected cells: changing them selects and unselects the cells and raises SelectedCellsChanged.</summary>
		sealed class SelectedCellsCollection : IList<DataGridCellInfo>
		{
			readonly DataGrid _owner;
			readonly List<DataGridCellInfo> _cells = new List<DataGridCellInfo>();

			internal SelectedCellsCollection(DataGrid owner) => _owner = owner;

			public DataGridCellInfo this[int index]
			{
				get => _cells[index];
				set
				{
					var old = _cells[index];
					_cells[index] = value;
					_owner.OnSelectedCellsChangedInternal(new List<DataGridCellInfo> { value }, new List<DataGridCellInfo> { old });
				}
			}

			public int Count => _cells.Count;

			public bool IsReadOnly => false;

			public void Add(DataGridCellInfo item)
			{
				if (_cells.Contains(item))
					return;

				_cells.Add(item);
				_owner.OnSelectedCellsChangedInternal(new List<DataGridCellInfo> { item }, new List<DataGridCellInfo>());
			}

			public void Clear()
			{
				if (_cells.Count == 0)
					return;

				var removed = _cells.ToList();
				_cells.Clear();
				_owner.OnSelectedCellsChangedInternal(new List<DataGridCellInfo>(), removed);
			}

			/// <summary>Replaces the whole selection with one change event.</summary>
			internal void Replace(List<DataGridCellInfo> cells)
			{
				var removed = _cells.Where(c => !cells.Contains(c)).ToList();
				var added = cells.Where(c => !_cells.Contains(c)).ToList();
				_cells.Clear();
				_cells.AddRange(cells);
				if (added.Count > 0 || removed.Count > 0)
					_owner.OnSelectedCellsChangedInternal(added, removed);
			}

			public bool Contains(DataGridCellInfo item) => _cells.Contains(item);

			public void CopyTo(DataGridCellInfo[] array, int arrayIndex) => _cells.CopyTo(array, arrayIndex);

			public IEnumerator<DataGridCellInfo> GetEnumerator() => _cells.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => _cells.GetEnumerator();

			public int IndexOf(DataGridCellInfo item) => _cells.IndexOf(item);

			public void Insert(int index, DataGridCellInfo item)
			{
				_cells.Insert(index, item);
				_owner.OnSelectedCellsChangedInternal(new List<DataGridCellInfo> { item }, new List<DataGridCellInfo>());
			}

			public bool Remove(DataGridCellInfo item)
			{
				if (!_cells.Remove(item))
					return false;

				_owner.OnSelectedCellsChangedInternal(new List<DataGridCellInfo>(), new List<DataGridCellInfo> { item });
				return true;
			}

			public void RemoveAt(int index) => Remove(_cells[index]);
		}

		// ---- columns from the items ----------------------------------------------------------------------------------

		/// <summary>With AutoGenerateColumns, a text column per property (a data view's columns) of the new items.</summary>
		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			base.OnItemsSourceChanged(oldValue, newValue);
			if (!AutoGenerateColumns || newValue == null)
				return;

			PropertyDescriptorCollection properties = null;
			if (newValue is ITypedList typed)
				properties = typed.GetItemProperties(null);
			else if (Items.Count > 0)
				properties = TypeDescriptor.GetProperties(Items[0]);

			if (properties == null)
				return;

			foreach (var column in _columns.Where(c => c.IsAutoGenerated()).ToList())
				_columns.Remove(column);
			foreach (PropertyDescriptor property in properties)
				_columns.Add(new DataGridTextColumn { Header = property.DisplayName, Binding = new Binding(property.Name) }.AutoGenerated());
		}
	}

	internal static class AutoGeneratedColumns
	{
		static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DataGridColumn, object> s_generated =
			new System.Runtime.CompilerServices.ConditionalWeakTable<DataGridColumn, object>();

		internal static DataGridColumn AutoGenerated(this DataGridColumn column)
		{
			s_generated.Add(column, null);
			return column;
		}

		internal static bool IsAutoGenerated(this DataGridColumn column) => s_generated.TryGetValue(column, out _);
	}
}
