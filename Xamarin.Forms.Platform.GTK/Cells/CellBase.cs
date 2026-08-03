using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Gtk;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Cells
{
	public abstract class CellBase : EventBox
	{
		// Which widget currently shows a given Forms Cell - i.e. which one owes it a Disappearing.
		// It has to live on the Cell, not on the widget: a ListView refresh throws away every cell
		// widget and builds a fresh one (CellRenderer.GetCell is always called with reusableView:
		// null - see ListViewRenderer.UpdateItems and Controls/TableView), while the Forms Cell
		// survives. Held on the widget, "is this cell on screen?" was false again on every refresh,
		// so the setter below re-sent Appearing for all rows and never sent Disappearing: an
		// ItemAppearing-driven paging handler re-triggered itself forever, and anything pairing the
		// two events leaked one "appeared" per row per collection change. Recording the owner - not
		// just a bool - is what lets a discarded widget stay quiet instead of sending Disappearing
		// for the cell its own replacement is already showing.
		static readonly BindableProperty AppearedInProperty =
			BindableProperty.CreateAttached("AppearedIn", typeof(CellBase), typeof(Cell), null);

		private Cell _cell;
		private int _desiredHeight;
		private IList<MenuItem> _contextActions;

		public Action<object, PropertyChangedEventArgs> PropertyChanged;

		protected CellBase()
		{
			ButtonReleaseEvent += OnClick;
		}

		public Cell Cell
		{
			get { return _cell; }
			set
			{
				if (_cell == value)
					return;

				if (_cell != null)
					ReleaseAppearance(_cell);

				_cell = value;
				UpdateCell();
				_contextActions = Cell.ContextActions;

				if (_cell != null)
					ClaimAppearance(_cell);
			}
		}

		public object Item => Cell?.BindingContext;

		protected bool ParentHasUnevenRows
		{
			get
			{
				var table = Cell.RealParent as TableView;
				if (table != null)
					return table.HasUnevenRows;

				var list = Cell.RealParent as ListView;
				if (list != null)
					return list.HasUnevenRows;

				return false;
			}
		}

		public int DesiredHeight
		{
			get
			{
				return _desiredHeight;
			}

			set
			{
				_desiredHeight = value;
			}
		}

		public void SetDesiredHeight(int height)
		{
			DesiredHeight = height;

			if (IsRealized)
			{
				HeightRequest = DesiredHeight;
			}
		}

		public void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			PropertyChanged?.Invoke(this, e);
		}

		protected override void OnRealized()
		{
			base.OnRealized();

			HeightRequest = DesiredHeight;
		}

		protected override void OnParentSet(Gtk.Widget previousParent)
		{
			base.OnParentSet(previousParent);

			if (_cell == null)
				return;

			// Being unparented is the only notice this widget gets that the list threw it away:
			// Controls/ListView.ClearList and Controls/TableView.RefreshSource simply Remove() their
			// children, nobody clears Cell first. Without this a row dropped from the ItemsSource
			// would never disappear at all. Re-parenting is the symmetric case, and claiming is a
			// no-op unless the slot is free, so the usual pack-after-create order stays silent.
			if (Parent == null)
				ReleaseAppearance(_cell);
			else
				ClaimAppearance(_cell);
		}

		protected override void OnDestroyed()
		{
			base.OnDestroyed();

			ButtonReleaseEvent -= OnClick;

			// Covers a widget destroyed while unparented, which OnParentSet never hears about. The
			// two are idempotent between them: whichever runs first releases, the other sees that
			// this widget no longer owns the slot and does nothing.
			if (_cell != null)
				ReleaseAppearance(_cell);
		}

		protected virtual void UpdateCell()
		{
		}

		// Takes over the on-screen slot of `cell` for this widget. If a (by now discarded) widget
		// still held it, the row never left the screen, so the slot changes hands silently - raising
		// a second Appearing there is exactly what made incremental paging loop.
		void ClaimAppearance(Cell cell)
		{
			var owner = (CellBase)cell.GetValue(AppearedInProperty);

			if (ReferenceEquals(owner, this))
				return;

			cell.SetValue(AppearedInProperty, this);

			if (owner == null)
				Device.BeginInvokeOnMainThread(cell.SendAppearing);
		}

		// The mirror of ClaimAppearance: only the widget holding the slot may give it up, so a widget
		// being torn down after its cell was handed to a newer widget stays quiet.
		void ReleaseAppearance(Cell cell)
		{
			var owner = (CellBase)cell.GetValue(AppearedInProperty);

			if (!ReferenceEquals(owner, this))
				return;

			cell.SetValue(AppearedInProperty, null);

			Device.BeginInvokeOnMainThread(cell.SendDisappearing);
		}

		private void OnClick(object o, ButtonReleaseEventArgs args)
		{
			if (args.Event.Button != 3)  // Right button
			{
				return;
			}

			if (_contextActions.Any())
			{
				OpenContextMenu();
			}
		}

		private void OpenContextMenu()
		{
			var menu = new Gtk.Menu();

			SetupMenuItems(menu);
			menu.ShowAll();
			menu.Popup();
		}

		private void SetupMenuItems(Gtk.Menu menu)
		{
			foreach (MenuItem item in Cell.ContextActions)
			{
				// GTK3 deprecates Gtk.ImageMenuItem and its Image property: a menu item is now a
				// plain MenuItem whose child is whatever box of widgets you want. Build that box
				// up front so the async icon load only has to fill in the Pixbuf.
				var menuItem = new Gtk.MenuItem();
				var menuItemImage = new Gtk.Image();
				var menuItemBox = new Gtk.Box(Gtk.Orientation.Horizontal, 6);

				menuItemBox.PackStart(menuItemImage, false, false, 0);
				menuItemBox.PackStart(new Gtk.Label(item.Text) { Xalign = 0 }, true, true, 0);
				menuItem.Add(menuItemBox);

				_ = item.ApplyNativeImageAsync(MenuItem.IconImageSourceProperty, icon =>
				{
					if (icon != null)
					{
						menuItemImage.Pixbuf = icon;
						menuItemImage.Show();
					}
				});

				menuItem.ButtonPressEvent += (sender, args) =>
				{
					((IMenuItemController)item).Activate();
				};

				menu.Add(menuItem);
			}
		}
	}
}
