using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;

namespace Xamarin.Forms.Platform.GTK.Extensions
{
	public static class WidgetExtensions
	{
		public static SizeRequest GetDesiredSize(
			this Widget self,
			double widthConstraint,
			double heightConstraint)
		{
			Gdk.Size desiredSize;

			if (self is IDesiredSizeProvider)
			{
				desiredSize = ((IDesiredSizeProvider)self).GetDesiredSize();
			}
			else
			{
				self.GetPreferredSize(out _, out var req); // GTK3 replacement for gtk_widget_size_request
				desiredSize = new Gdk.Size(
					req.Width > 0 ? req.Width : 0,
					req.Height > 0 ? req.Height : 0);
			}

			var widthFits = widthConstraint >= desiredSize.Width;
			var heightFits = heightConstraint >= desiredSize.Height;

			if (widthFits && heightFits) // Enough space with given constraints
			{
				return new SizeRequest(new Size(desiredSize.Width, desiredSize.Height));
			}

			if (!widthFits)
			{
				self.SetSize((int)widthConstraint, -1);

				self.GetPreferredSize(out _, out var req); // GTK3 replacement for gtk_widget_size_request
				desiredSize = new Gdk.Size(
					req.Width > 0 ? req.Width : 0,
					req.Height > 0 ? req.Height : 0);
				heightFits = heightConstraint >= desiredSize.Height;
			}

			var size = new Size(desiredSize.Width, heightFits ? desiredSize.Height : (int)heightConstraint);

			return new SizeRequest(size);
		}

		public static void MoveTo(this Widget self, double x, double y)
		{
			if (self.Parent is Fixed)
			{
				var container = self.Parent as Fixed;
				var calcX = (int)Math.Round(x);
				var calcY = (int)Math.Round(y);

				int containerChildX, containerChildY;
				GetContainerChildXY(container, self, out containerChildX, out containerChildY);

				if (containerChildX != calcX || containerChildY != calcY)
				{
					container.Move(self, calcX, calcY);
				}
			}
		}

		// gtk_fixed_get_child_position, not a pair of child-property reads. Gtk 4 has no container
		// child properties at all - GtkFixed keeps the position in its own layout manager and
		// offers a direct getter, which is both the replacement and a good deal less indirect.
		static void GetContainerChildXY(Fixed parent, Widget child, out int x, out int y)
		{
			parent.GetChildPosition(child, out double childX, out double childY);

			x = (int)childX;
			y = (int)childY;
		}

		/// <summary>
		/// Puts a widget on top of its siblings.
		/// </summary>
		/// <remarks>
		/// Replaces <c>widget.Window.Raise()</c>. In Gtk 3 an overlapping widget was raised by
		/// restacking its GdkWindow; Gtk 4 has no per-widget windows and draws siblings strictly in
		/// child order, so being last IS being on top.
		///
		/// Only a box can reorder - it is the one Gtk 4 container with an ordering API. A widget
		/// inside anything else keeps the order it was added in, which is what the Gtk 3 code got
		/// for a windowless widget too.
		/// </remarks>
		public static void Raise(this Widget self)
		{
			if (self?.Parent is Box box && box.LastChild != self)
				box.ReorderChildAfter(self, box.LastChild);
		}

		public static void SetSize(this Widget self, double width, double height)
		{
			int calcWidth = (int)Math.Round(width);
			int calcHeight = (int)Math.Round(height);

			// Avoid negative values
			if (calcWidth < -1)
			{
				calcWidth = -1;
			}

			if (calcHeight < -1)
			{
				calcHeight = -1;
			}

			if (calcWidth != self.WidthRequest || calcHeight != self.HeightRequest)
			{
				self.SetSizeRequest(calcWidth, calcHeight);
				return;
			}

			// The request already IS what we want, yet the widget is allocated smaller than it. That
			// means the resize gtk_widget_set_size_request queued the first time round was lost, and
			// it will not queue another one for an unchanged value - so without this the widget stays
			// pinned at its natural size for the lifetime of the window, however many layout passes
			// Forms runs.
			//
			// The guard is what keeps this from becoming a resize storm: inside a Gtk.Fixed - which
			// is what every Forms layout and every page's content sits in - a child is allocated its
			// PREFERRED size, i.e. never less than its request, so "allocated smaller than requested"
			// is not a state that occurs in steady operation. It is only ever the lost-resize
			// signature. Measured on the ControlGallery's detail page: request (500,528) with the
			// allocation still at the natural 257x230, no wedge (a bare QueueResize() fixed it
			// immediately, and so did nudging the request to a different value and back) - see
			// scratchpad/m3-wedge.log.
			if (self.Parent is Fixed
				&& ((calcWidth >= 0 && self.Width < calcWidth)
					|| (calcHeight >= 0 && self.Height < calcHeight)))
			{
				self.QueueResize();
			}
		}

		public static void RemoveFromContainer(this Widget self, Widget child)
		{
			var container = self as Container;

			if (child != null && child.Parent != null)
			{
				if (container != null && container.HasChild(child))
				{
					container.Remove(child);
				}
			}
		}

		public static bool HasChild(this Container self, Widget child)
		{
			return self.Children.Contains(child);
		}

		public static Size GetMaxChildDesiredSize(this Widget self, double widthConstraint, double heightConstraint)
		{
			var container = self as Container;
			var childReq = Size.Zero;

			if (container != null)
			{
				foreach (var child in container.Children)
				{
					var currentChildReq = child.GetMaxChildDesiredSize(widthConstraint, heightConstraint);

					if (currentChildReq.Height > childReq.Height)
					{
						childReq = currentChildReq;
					}
				}
			}

			self.SetSize((int)widthConstraint - 1, -1);
			var desiredSize = self.GetDesiredSize(widthConstraint, heightConstraint);

			return childReq.Height > desiredSize.Request.Height
				? childReq
				: desiredSize.Request;
		}

		public static IEnumerable<Widget> GetDescendants(this Widget self)
		{
			var descendants = new List<Widget>();
			var container = self as Container;

			if (container != null)
			{
				foreach (var child in container.Children)
				{
					descendants.Add(child);
					descendants.AddRange(child.GetDescendants());
				}
			}

			return descendants;
		}

		public static void PrintTree(this Widget widget)
		{
			const char indent = '-';
			int level = CalculateDepthLevel(widget);

			Console.WriteLine(
				string.Format(
					"({0}) {1} Name: {2} ({3})",
					level,
					new String(indent, level * 2),
					widget.Name,
					widget.GetType()));

			// Width/Height, not Allocation: the property is deprecated in Gtk 4, and its X/Y are
			// always zero there - a "Location" line would print (0, 0) for every widget.
			Console.WriteLine(string.Format("{0} Size: {1}x{2}", new String('\t', level), widget.Width, widget.Height));

			if (widget is Container)
			{
				var container = widget as Container;

				foreach (Widget child in container.Children)
				{
					PrintTree(child);
				}
			}
		}

		private static int CalculateDepthLevel(Widget widget)
		{
			int level = 0;
			Widget current = widget;

			while ((current = current.Parent) != null)
			{
				level++;
			}

			return level;
		}
	}
}
