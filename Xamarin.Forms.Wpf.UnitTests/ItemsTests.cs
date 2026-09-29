using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Xamarin.Forms.Wpf;
using Xunit;
using XF = Xamarin.Forms;

namespace Wpf.UnitTests
{
	/// <summary>Items controls: the item collection, selection, and each control's view of its items.</summary>
	public class ItemsTests : WpfTestBase
	{
		[Fact]
		public void AnItemAddedToASortedCollectionGoesWhereTheOrderPutsIt()
		{
			Run(() =>
			{
				var list = new ListBox();
				list.Items.SortDescriptions.Add(new SortDescription(string.Empty, ListSortDirection.Ascending));

				list.Items.Add("b");
				list.Items.Add("d");
				var at = list.Items.Add("c");

				Assert.Equal(1, at);
				Assert.Equal(new object[] { "b", "c", "d" }, list.Items.Cast<object>());
			});
		}

		[Fact]
		public void ItemsFromASourceCannotBeChangedDirectly()
		{
			Run(() =>
			{
				var list = new ListBox { ItemsSource = new[] { "a", "b" } };

				Assert.Equal(2, list.Items.Count);
				Assert.Throws<InvalidOperationException>(() => list.Items.Add("c"));
			});
		}

		[Fact]
		public void ADataViewRowChangeIsReportedForThatRowAlone()
		{
			Run(() =>
			{
				var table = new DataTable();
				table.Columns.Add("C0", typeof(string));
				table.Rows.Add("x");
				table.Rows.Add("y");
				var list = new ListBox { ItemsSource = table.DefaultView };
				var actions = new List<NotifyCollectionChangedAction>();
				list.Items.CollectionChanged += (s, e) => actions.Add(e.Action);

				table.Rows[1][0] = "z";
				table.Rows.Add("w");

				Assert.Equal(new[] { NotifyCollectionChangedAction.Replace, NotifyCollectionChangedAction.Add }, actions);
				Assert.Equal(3, list.Items.Count);
			});
		}

		[Fact]
		public void SelectedIndexAndSelectedItemMoveTogetherWithOneEvent()
		{
			Run(() =>
			{
				var list = new ListBox { Items = { "a", "b", "c" } };
				var events = new List<(object Removed, object Added)>();
				list.SelectionChanged += (s, e) => events.Add((e.RemovedItems.Cast<object>().FirstOrDefault(), e.AddedItems.Cast<object>().FirstOrDefault()));

				list.SelectedIndex = 1;
				list.SelectedItem = "c";

				Assert.Equal(2, list.SelectedIndex);
				Assert.Equal(new[] { ((object)null, (object)"b"), ("b", "c") }, events);
			});
		}

		[Fact]
		public void RemovingTheSelectedItemLeavesNoSelection()
		{
			Run(() =>
			{
				var list = new ListBox { Items = { "a", "b" }, SelectedIndex = 1 };

				list.Items.RemoveAt(1);

				Assert.Equal(-1, list.SelectedIndex);
				Assert.Null(list.SelectedItem);
			});
		}

		[Fact]
		public void TheCollectionViewShowsTheItemsTextAndTheSelection()
		{
			Run(() =>
			{
				var list = new ListBox { Items = { "a", new ListBoxItem { Content = "_b" } } };
				var view = (XF.CollectionView)((NativeShell)list.NativeView).Inner;
				var boxes = ((IEnumerable<ItemBox>)view.ItemsSource).ToList();

				Assert.Equal(new[] { "a", "b" }, boxes.Select(b => b.Text));
				list.SelectedIndex = 1;
				Assert.Same(boxes[1], view.SelectedItem);
				Assert.True(((ListBoxItem)list.Items[1]).IsSelected);
			});
		}

		[Fact]
		public void SelectingInTheViewSelectsTheItem()
		{
			Run(() =>
			{
				var list = new ListBox { Items = { "a", "b" } };
				var view = (XF.CollectionView)((NativeShell)list.NativeView).Inner;

				view.SelectedItem = ((IEnumerable<ItemBox>)view.ItemsSource).Last();

				Assert.Equal(1, list.SelectedIndex);
			});
		}

		[Fact]
		public void AMultipleSelectionIsTheSelectedItems()
		{
			Run(() =>
			{
				var list = new ListBox { SelectionMode = SelectionMode.Multiple, Items = { "a", "b", "c" } };
				var added = new List<object>();
				list.SelectionChanged += (s, e) => added.AddRange(e.AddedItems.Cast<object>());

				list.SelectedItems.Add("c");
				list.SelectedItems.Add("a");

				Assert.Equal(new object[] { "c", "a" }, added);
				Assert.Equal("c", list.SelectedItem);
				Assert.Equal(2, list.SelectedIndex);
				list.UnselectAll();
				Assert.Equal(-1, list.SelectedIndex);
			});
		}

		[Fact]
		public void AComboBoxPickerHasTheItemsAndTheTextFollowsTheSelection()
		{
			Run(() =>
			{
				var combo = new ComboBox { Items = { "Light", "Dark" } };
				var grid = (XF.Grid)combo.NativeView;
				var picker = grid.Children.OfType<XF.Picker>().Single();

				combo.SelectedIndex = 1;

				Assert.Equal(new[] { "Light", "Dark" }, ((IEnumerable<string>)picker.ItemsSource).ToArray());
				Assert.Equal(1, picker.SelectedIndex);
				Assert.Equal("Dark", combo.Text);

				picker.SelectedIndex = 0;
				Assert.Equal(0, combo.SelectedIndex);
			});
		}

		[Fact]
		public void TypingAnItemsTextIntoAnEditableComboBoxSelectsIt()
		{
			Run(() =>
			{
				var combo = new ComboBox { IsEditable = true, Items = { "one", "two" } };
				var changes = 0;
				combo.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new TextChangedEventHandler((s, e) => changes++));
				var entry = ((XF.Grid)combo.NativeView).Children.OfType<XF.Entry>().Single();

				entry.Text = "two";

				Assert.Equal(1, combo.SelectedIndex);
				Assert.Equal(1, changes);
			});
		}

		[Fact]
		public void TheFirstTabIsSelectedAndItsContentShown()
		{
			Run(() =>
			{
				var first = new Grid();
				var tabs = new TabControl { Items = { new TabItem { Header = "_One", Content = first }, new TabItem { Header = "Two", Content = "text" } } };
				object source = null;
				tabs.SelectionChanged += (s, e) => source = e.OriginalSource;
				var root = (XF.Grid)tabs.NativeView;
				var content = (XF.ContentView)((XF.Frame)root.Children[1]).Content;

				Assert.Equal(0, tabs.SelectedIndex);
				Assert.True(((TabItem)tabs.Items[0]).IsSelected);
				Assert.Same(first.NativeView, content.Content);

				((TabItem)tabs.Items[1]).IsSelected = true;

				Assert.Equal(1, tabs.SelectedIndex);
				Assert.Same(tabs, source);
				Assert.False(((TabItem)tabs.Items[0]).IsSelected);
			});
		}

		[Fact]
		public void ExpandingANodeShowsItsChildrenAndSelectingRaisesSelectedItemChanged()
		{
			Run(() =>
			{
				var child = new TreeViewItem { Header = "child" };
				var root = new TreeViewItem { Header = "root", Items = { child } };
				var tree = new TreeView { Items = { root } };
				var view = (XF.CollectionView)((NativeShell)tree.NativeView).Inner;
				object selected = null;
				tree.SelectedItemChanged += (s, e) => selected = e.NewValue;

				Assert.Single((IEnumerable<object>)view.ItemsSource);
				root.IsExpanded = true;
				Assert.Equal(2, ((IEnumerable<object>)view.ItemsSource).Count());

				child.IsSelected = true;
				Assert.Same(child, selected);
				Assert.Same(child, tree.SelectedItem);
			});
		}

		static DataGrid Flex(out DataTable table)
		{
			table = new DataTable();
			table.Columns.Add("C0", typeof(string));
			table.Columns.Add("C1", typeof(string));
			table.Rows.Add("a", "b");
			table.Rows.Add("c", "d");
			var grid = new DataGrid { AutoGenerateColumns = false, ItemsSource = table.DefaultView, SelectionUnit = DataGridSelectionUnit.Cell };
			grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("C0") });
			grid.Columns.Add(new DataGridTextColumn { Binding = new Binding("C1") });
			return grid;
		}

		[Fact]
		public void ADataGridHasARowPerItemAndACellPerColumn()
		{
			Run(() =>
			{
				var grid = Flex(out _);
				var loading = 0;
				grid.LoadingRow += (s, e) => loading++;

				var row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(1);

				Assert.Equal(2, loading);
				Assert.Equal(1, row.GetIndex());
				var content = (TextBlock)grid.Columns[1].GetCellContent(row);
				Assert.Equal("d", content.Text);
				Assert.IsType<DataGridCell>(content.Parent);
				Assert.Same(row, ((DataGridCell)content.Parent).Parent);
			});
		}

		[Fact]
		public void ChangingATableCellUpdatesItsTextInPlace()
		{
			Run(() =>
			{
				var grid = Flex(out var table);
				var row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
				var content = (TextBlock)grid.Columns[0].GetCellContent(row);

				table.Rows[0][0] = "changed";

				Assert.Same(row, grid.ItemContainerGenerator.ContainerFromIndex(0));
				Assert.Equal("changed", content.Text);
			});
		}

		[Fact]
		public void AColumnsElementStyleAlignsItsCells()
		{
			Run(() =>
			{
				var grid = Flex(out _);
				var style = new Style(typeof(TextBlock)) { Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right) } };

				((DataGridTextColumn)grid.Columns[0]).ElementStyle = style;

				var row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(0);
				Assert.Equal(TextAlignment.Right, ((TextBlock)grid.Columns[0].GetCellContent(row)).TextAlignment);
			});
		}

		[Fact]
		public void SelectedCellsSelectTheirCells()
		{
			Run(() =>
			{
				var grid = Flex(out var table);
				var changes = 0;
				grid.SelectedCellsChanged += (s, e) => changes++;
				var info = new DataGridCellInfo(table.DefaultView[1], grid.Columns[0]);

				grid.SelectedCells.Add(info);

				var row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(1);
				Assert.True(((DataGridCell)grid.Columns[0].GetCellContent(row).Parent).IsSelected);
				Assert.Equal(1, changes);
			});
		}

		[Fact]
		public void TheDataGridViewLaysTheCellsOut()
		{
			Run(() =>
			{
				var grid = Flex(out _);
				grid.HeadersVisibility = DataGridHeadersVisibility.None;
				grid.Columns[1].Width = new DataGridLength(55);
				var layout = (XF.Grid)((XF.ScrollView)((NativeShell)grid.NativeView).Inner).Content;

				Assert.Equal(4, layout.Children.Count);
				Assert.Equal(2, layout.RowDefinitions.Count);
				Assert.Equal(new XF.GridLength(55), layout.ColumnDefinitions[1].Width);

				((DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(1)).Visibility = Visibility.Collapsed;
				Assert.Equal(new XF.GridLength(0), layout.RowDefinitions[1].Height);
			});
		}

		[Fact]
		public void AGridViewListViewShowsAColumnPerMember()
		{
			Run(() =>
			{
				var view = new GridView { Columns = { new GridViewColumn { Header = "Name", DisplayMemberBinding = new Binding("Name") }, new GridViewColumn { Header = "Size", DisplayMemberBinding = new Binding("Size") } } };
				var list = new ListView { View = view, ItemsSource = new[] { new { Name = "a.txt", Size = 3 } } };
				var outer = (XF.Grid)((NativeShell)list.NativeView).Inner;
				var collection = outer.Children.OfType<XF.CollectionView>().Single();

				var box = ((IEnumerable<ItemBox>)collection.ItemsSource).Single();
				Assert.Equal(new[] { "a.txt", "3" }, box.Cells);
			});
		}

		[Fact]
		public void ACheckableMenuItemTogglesAndItsClickBubbles()
		{
			Run(() =>
			{
				var item = new MenuItem { Header = "_Check", IsCheckable = true };
				var menu = new Menu { Items = { new MenuItem { Header = "_File", Items = { item } } } };
				object clicked = null;
				menu.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler((s, e) => clicked = e.OriginalSource));

				item.PerformClick();

				Assert.True(item.IsChecked);
				Assert.Same(item, clicked);
			});
		}

		[Fact]
		public void ClickEventRaisedByCodeReachesTheHandlersWithoutToggling()
		{
			Run(() =>
			{
				var item = new MenuItem { Header = "x", IsCheckable = true };
				var clicks = 0;
				item.Click += (s, e) => clicks++;

				item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, item));

				Assert.Equal(1, clicks);
				Assert.False(item.IsChecked);
			});
		}

		[Theory]
		[InlineData("Ctrl+O", "<Control>o")]
		[InlineData("Ctrl+Shift+F5", "<Control><Shift>F5")]
		[InlineData("Alt+X", "<Alt>x")]
		[InlineData("Del", "Delete")]
		[InlineData("", null)]
		public void GestureTextsAreGtkAccelerators(string gesture, string expected) =>
			Assert.Equal(expected, MenuPopover.Accelerator(gesture));
	}
}
