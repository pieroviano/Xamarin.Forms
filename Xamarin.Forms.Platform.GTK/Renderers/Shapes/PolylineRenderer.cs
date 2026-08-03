using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Xamarin.Forms.Platform.GTK.Controls;
using Xamarin.Forms.Platform.GTK.Extensions;
using Xamarin.Forms.PlatformConfiguration.GTKSpecific;
using Xamarin.Forms.Shapes;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public class PolylineRenderer : ShapeRenderer<Polyline, PolylineView>
	{
		PointCollection _points;
		bool _disposed;

		protected override void Dispose(bool disposing)
		{
			// The PointCollection belongs to the Forms Polyline, so it outlives this renderer and keeps
			// it (and its native subtree) alive. Detach before chaining to base, which destroys and nulls
			// Control: a Points.Add() after teardown would otherwise re-enter UpdatePoints on a null Control.
			if (disposing && !_disposed)
			{
				_disposed = true;

				if (_points != null)
				{
					_points.CollectionChanged -= OnCollectionChanged;
					_points = null;
				}
			}

			base.Dispose(disposing);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Polyline> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
			{
				UpdatePoints();
				UpdateFillRule();
			}
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			base.OnElementPropertyChanged(sender, args);

			if (args.PropertyName == Polyline.PointsProperty.PropertyName)
				UpdatePoints();
			else if (args.PropertyName == Polyline.FillRuleProperty.PropertyName)
				UpdateFillRule();
		}

		void UpdatePoints()
		{
			if (_points != null)
				_points.CollectionChanged -= OnCollectionChanged;

			// Reached from OnCollectionChanged as well, which the collection can raise once Element and
			// Control are already gone; do not resubscribe to a collection this renderer no longer serves.
			if (_disposed || Element == null || Control == null)
			{
				_points = null;
				return;
			}

			_points = Element.Points;

			_points.CollectionChanged += OnCollectionChanged;

			Control.UpdatePoints(_points);
		}

		void UpdateFillRule()
		{
			Control.UpdateFillMode(Element.FillRule == FillRule.Nonzero);
		}

		void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			UpdatePoints();
		}
	}
}
