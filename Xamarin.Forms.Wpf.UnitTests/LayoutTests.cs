using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;
using XF = Xamarin.Forms;

namespace Wpf.UnitTests
{
	/// <summary>WPF layout onto the Xamarin.Forms views: sizes, margins, alignments, visibility, the panels' own placement.</summary>
	public class LayoutTests : WpfTestBase
	{
		[Fact]
		public void SizeMarginAndAlignmentAreTheViewsRequestsAndOptions()
		{
			Run(() =>
			{
				var element = new Border { Width = 50, Height = 20, Margin = new Thickness(1, 2, 3, 4), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom };
				var view = element.NativeView;

				Assert.Equal(50, view.WidthRequest);
				Assert.Equal(20, view.HeightRequest);
				Assert.Equal(new XF.Thickness(1, 2, 3, 4), view.Margin);
				Assert.Equal(XF.LayoutOptions.Start, view.HorizontalOptions);
				Assert.Equal(XF.LayoutOptions.End, view.VerticalOptions);

				element.Width = double.NaN;
				Assert.Equal(-1, view.WidthRequest);
			});
		}

		[Fact]
		public void HiddenKeepsItsPlaceAndCollapsedDoesNot()
		{
			Run(() =>
			{
				var element = new Border();
				var view = element.NativeView;

				element.Visibility = Visibility.Hidden;
				Assert.True(view.IsVisible);
				Assert.Equal(0, view.Opacity);
				Assert.True(view.InputTransparent);

				element.Visibility = Visibility.Collapsed;
				Assert.False(view.IsVisible);

				element.Visibility = Visibility.Visible;
				Assert.True(view.IsVisible);
				Assert.Equal(1, view.Opacity);
				Assert.False(view.InputTransparent);
			});
		}

		[Fact]
		public void AGridHasNoSpacingAndPlacesChildrenInTheirCells()
		{
			Run(() =>
			{
				var child = new Border();
				var grid = new Grid
				{
					RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition() },
					ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(40) }, new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) } },
				};
				Grid.SetRow(child, 1);
				Grid.SetColumn(child, 1);
				grid.Children.Add(child);
				var native = (XF.Grid)grid.NativeView;

				Assert.Equal(0, native.RowSpacing);
				Assert.Equal(0, native.ColumnSpacing);
				Assert.Equal(XF.GridLength.Auto, native.RowDefinitions[0].Height);
				Assert.Equal(new XF.GridLength(40), native.ColumnDefinitions[0].Width);
				Assert.Equal(new XF.GridLength(2, XF.GridUnitType.Star), native.ColumnDefinitions[1].Width);
				Assert.Equal(1, XF.Grid.GetRow(child.NativeView));
				Assert.Equal(1, XF.Grid.GetColumn(child.NativeView));

				Grid.SetColumn(child, 0);
				Assert.Equal(0, XF.Grid.GetColumn(child.NativeView));
			});
		}

		[Fact]
		public void ChildrenKeepTheirOrderInTheView()
		{
			Run(() =>
			{
				var a = new Border();
				var b = new Border();
				var c = new Border();
				var panel = new StackPanel { Children = { a, c } };
				var native = (XF.StackLayout)panel.NativeView;

				panel.Children.Insert(1, b);

				Assert.Equal(new[] { a.NativeView, b.NativeView, c.NativeView }, native.Children);
				panel.Children.Remove(b);
				Assert.Equal(new[] { a.NativeView, c.NativeView }, native.Children);
				Assert.Null(b.Parent);
			});
		}

		[Fact]
		public void ACanvasPlacesChildrenAtLeftAndTop()
		{
			Run(() =>
			{
				var child = new Border { Width = 10, Height = 5 };
				Canvas.SetLeft(child, 7);
				Canvas.SetTop(child, 8);
				var canvas = new Canvas { Children = { child } };
				_ = canvas.NativeView;

				Assert.Equal(new XF.Rectangle(7, 8, 10, 5), XF.AbsoluteLayout.GetLayoutBounds(child.NativeView));

				Canvas.SetLeft(child, 20);
				Assert.Equal(20, XF.AbsoluteLayout.GetLayoutBounds(child.NativeView).X);
			});
		}

		[Fact]
		public void ADockPanelDocksInOrderAndTheLastChildFills()
		{
			Run(() =>
			{
				var top = new Border { Height = 20 };
				var left = new Border { Width = 30 };
				var fill = new Border();
				DockPanel.SetDock(top, Dock.Top);
				DockPanel.SetDock(left, Dock.Left);
				var dock = new DockPanel { Children = { top, left, fill } };
				var window = Host(dock, 200, 100);

				Assert.True(PumpUntil(() => fill.ActualWidth > 0, window));
				Assert.Equal(new XF.Rectangle(0, 0, dock.ActualWidth, 20), top.NativeView.Bounds);
				Assert.Equal(new XF.Rectangle(0, 20, 30, dock.ActualHeight - 20), left.NativeView.Bounds);
				Assert.Equal(new XF.Rectangle(30, 20, dock.ActualWidth - 30, dock.ActualHeight - 20), fill.NativeView.Bounds);
				window.Close();
			});
		}

		[Fact]
		public void AHigherZIndexDrawsLater()
		{
			Run(() =>
			{
				var a = new Border();
				var b = new Border();
				var grid = new Grid { Children = { a, b } };
				var native = (XF.Grid)grid.NativeView;

				Panel.SetZIndex(a, 1);

				Assert.Same(a.NativeView, native.Children.Last());
			});
		}

		[Fact]
		public void ASolidBackgroundPaintsThePanelAndAnImageBrushIsAnImageBelowTheChildren()
		{
			Run(() =>
			{
				var child = new Border();
				var grid = new Grid { Children = { child }, Background = Brushes.Red };
				var native = (XF.Grid)grid.NativeView;
				Assert.Equal(XF.Color.Red, native.BackgroundColor);

				grid.Background = new ImageBrush(new System.Windows.Media.Imaging.BitmapImage(new System.Uri("Fixtures/Pixel.png", System.UriKind.Relative))) { Stretch = Stretch.None };

				Assert.IsType<XF.Image>(native.Children[0]);
				Assert.Same(child.NativeView, native.Children[1]);
			});
		}

		/// <remarks>
		/// The size the window asks GTK for: what GTK then gives it also depends on the window manager (a title bar
		/// has a minimum width), so the request is what is asserted.
		/// </remarks>
		[Fact]
		public void ASizeToContentWindowAsksForItsContentsSize()
		{
			Run(() =>
			{
				var window = new Window { SizeToContent = SizeToContent.WidthAndHeight, Content = new Border { Width = 123, Height = 45 } };

				Assert.Equal((123, 45), window.InitialSize());

				window.SizeToContent = SizeToContent.Manual;
				window.Width = 300;
				window.Height = 200;
				Assert.Equal((300, 200), window.InitialSize());
				window.Close();
			});
		}

		[Fact]
		public void RenderSizeAndSizeChangedFollowTheLayout()
		{
			Run(() =>
			{
				var border = new Border { Width = 60, Height = 30, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
				Size? reported = null;
				border.SizeChanged += (s, e) => reported = e.NewSize;
				var window = Host(new Grid { Children = { border } });

				Assert.True(PumpUntil(() => reported.HasValue, window));
				Assert.Equal(new Size(60, 30), reported);
				Assert.Equal(60, border.ActualWidth);
				window.Close();
			});
		}
	}
}
