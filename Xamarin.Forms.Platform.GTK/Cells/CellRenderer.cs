using System;
using System.ComponentModel;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Cells
{
	public abstract class CellRenderer : IRegisterable
	{
		static readonly BindableProperty RealCellProperty =
			BindableProperty.CreateAttached("RealCell", typeof(Gtk.Container),
				typeof(Cell), null);

		// The ForceUpdateSizeRequested handler is a closure over the widget it drives, so it cannot be
		// re-derived from the widget the way HandlePropertyChanged can. Park it on the Forms Cell, which
		// outlives every renderer and widget built for it - see WireUpForceUpdateSizeRequested.
		static readonly BindableProperty ForceUpdateSizeHandlerProperty =
			BindableProperty.CreateAttached("ForceUpdateSizeHandler", typeof(EventHandler),
				typeof(Cell), null);

		protected Cell Cell { get; private set; }

		public virtual CellBase GetCell(Cell item, Gtk.Container reusableView, Controls.ListView listView)
		{
			Cell = item;

			var cell = reusableView as Gtk.Container ?? GetCellWidgetInstance(item);

			var cellBase = cell as CellBase;

			if (cellBase != null)
			{
				// Detach the handler wired for the PREVIOUS widget bound to this Forms Cell.
				// The cellBase.Cell check below cannot do it: every call site passes
				// reusableView: null (ListViewRenderer.UpdateItems, Controls/TableView), so `cell`
				// is always a brand-new widget whose Cell is null and the -= is a guaranteed
				// no-op. Meanwhile the Forms Cell survives the rebuild, so it accumulated one
				// PropertyChanged subscription per refresh, each holding a discarded widget alive
				// and each re-running HandlePropertyChanged on every later property change.
				var previous = GetRealCell(item) as CellBase;

				if (previous != null && !ReferenceEquals(previous, cellBase))
				{
					item.PropertyChanged -= previous.HandlePropertyChanged;
				}

				if (cellBase.Cell != null)
				{
					cellBase.Cell.PropertyChanged -= cellBase.HandlePropertyChanged;
				}

				cellBase.Cell = item;

				item.PropertyChanged += cellBase.HandlePropertyChanged;
				cellBase.PropertyChanged = CellPropertyChanged;
			}

			SetRealCell(item, cell);
			WireUpForceUpdateSizeRequested(item, cell);
			UpdateBackground(cell, item);
			UpdateIsEnabled(cellBase);
			UpdateHeight(cellBase);

			return cellBase;
		}

		protected virtual void CellPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			var viewCell = sender as CellBase;

			if (args.PropertyName == Cell.IsEnabledProperty.PropertyName)
				UpdateIsEnabled(viewCell);
			else if (args.PropertyName == nameof(Cell.RenderHeight))
				UpdateHeight(viewCell);
		}

		protected virtual Gtk.Container GetCellWidgetInstance(Cell item)
		{
			return new Gtk.Container(IntPtr.Zero);
		}

		protected void UpdateBackground(Gtk.Container tableViewCell, Cell cell)
		{
			var bgColor = Color.White.ToGtkColor();
			var element = cell.RealParent as VisualElement;

			if (element != null)
				bgColor = element.BackgroundColor == Color.Default ? bgColor : element.BackgroundColor.ToGtkColor();

			UpdateBackgroundChild(cell, bgColor);

			tableViewCell.SetBackgroundColor(bgColor, Gtk.StateType.Normal);
		}

		protected virtual void OnForceUpdateSizeRequest(Cell cell, Gtk.Container nativeCell)
		{
			nativeCell.HeightRequest = (int)cell.RenderHeight;
			nativeCell.QueueDraw();
		}

		protected void UpdateHeight(CellBase cell)
		{
			if (cell?.Cell != null)
			{
				cell.HeightRequest = (int)cell.Cell.RenderHeight;
			}
		}

		private void WireUpForceUpdateSizeRequested(Cell cell, Gtk.Container nativeCell)
		{
			// Same leak as the PropertyChanged wiring in GetCell, for the same reason: this handler used
			// to be a field on the renderer, but Registrar/DependencyResolver builds a brand-new
			// CellRenderer for every GetCell call (ListViewRenderer.GetCell, Controls/TableView), so the
			// field was always null here and the -= detached nothing. The closure wired by the PREVIOUS
			// renderer stayed subscribed to the surviving Forms Cell, so after N refreshes a single
			// ForceUpdateSize() drove N discarded widgets and kept every one of them alive. The handler
			// now lives on the Cell, which outlives the renderers, so it can always be found and dropped.
			var previous = (EventHandler)cell.GetValue(ForceUpdateSizeHandlerProperty);

			if (previous != null)
			{
				cell.ForceUpdateSizeRequested -= previous;
			}

			EventHandler handler = (sender, e) =>
			{
				OnForceUpdateSizeRequest(cell, nativeCell);
			};

			cell.ForceUpdateSizeRequested += handler;
			cell.SetValue(ForceUpdateSizeHandlerProperty, handler);
		}

		private static void UpdateIsEnabled(CellBase cell)
		{
			if (cell?.Cell != null)
			{
				cell.Sensitive = cell.Cell.IsEnabled;
			}
		}

		internal virtual void UpdateBackgroundChild(Cell cell, Gdk.Color backgroundColor)
		{
			// TODO
		}

		internal static Gtk.Container GetRealCell(BindableObject cell)
		{
			return (Gtk.Container)cell.GetValue(RealCellProperty);
		}

		internal static void SetRealCell(BindableObject cell, Gtk.Container renderer)
		{
			cell.SetValue(RealCellProperty, renderer);
		}
	}
}
