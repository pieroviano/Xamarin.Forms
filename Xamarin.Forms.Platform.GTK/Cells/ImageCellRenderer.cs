using System;
using System.ComponentModel;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Cells
{
	public class ImageCellRenderer : CellRenderer
	{
		public override CellBase GetCell(Cell item, Gtk.Container reusableView, Controls.ListView listView)
		{
			var gtkImageCell = base.GetCell(item, reusableView, listView) as ImageCell;
			var imageCell = (Xamarin.Forms.ImageCell)item;
			SetImage(imageCell, gtkImageCell);
			return gtkImageCell;
		}

		protected override Gtk.Container GetCellWidgetInstance(Cell item)
		{
			var imageCell = (Xamarin.Forms.ImageCell)item;

			var text = imageCell.Text ?? string.Empty;
			var textColor = imageCell.TextColor.ToGtkColor();
			var detail = imageCell.Detail ?? string.Empty;
			var detailColor = imageCell.DetailColor.ToGtkColor();

			return new ImageCell(
					null,
					text,
					textColor,
					detail,
					detailColor);
		}

		protected override void CellPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			base.CellPropertyChanged(sender, args);

			var gtkImageCell = (ImageCell)sender;
			var imageCell = (Xamarin.Forms.ImageCell)gtkImageCell.Cell;

			if (args.PropertyName == Xamarin.Forms.TextCell.TextProperty.PropertyName)
			{
				gtkImageCell.Text = imageCell.Text ?? string.Empty;
			}
			else if (args.PropertyName == Xamarin.Forms.TextCell.DetailProperty.PropertyName)
			{
				gtkImageCell.Detail = imageCell.Detail ?? string.Empty;
			}
			else if (args.PropertyName == Xamarin.Forms.ImageCell.ImageSourceProperty.PropertyName)
			{
				SetImage(imageCell, gtkImageCell);
			}
			// ImageCell derives from TextCell, so TextColor/DetailColor are just as bindable here as on a
			// plain TextCell. GetCellWidgetInstance only reads them once, at construction, so without
			// these branches a binding or trigger changing either colour at runtime did nothing.
			else if (args.PropertyName == Xamarin.Forms.TextCell.TextColorProperty.PropertyName)
			{
				gtkImageCell.TextColor = imageCell.TextColor.ToGtkColor();
			}
			else if (args.PropertyName == Xamarin.Forms.TextCell.DetailColorProperty.PropertyName)
			{
				gtkImageCell.DetailColor = imageCell.DetailColor.ToGtkColor();
			}
		}

		private static void SetImage(Xamarin.Forms.ImageCell cell, ImageCell target)
		{
			target.Image = null;
			_ = cell.ApplyNativeImageAsync(Xamarin.Forms.ImageCell.ImageSourceProperty, image =>
			{
				target.Image = image;
			});
		}
	}
}
