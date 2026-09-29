using System.Windows.Media;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;
using XFShapes = Xamarin.Forms.Shapes;

namespace System.Windows.Shapes
{
	/// <summary>A shape over a Xamarin.Forms shape: fill, stroke and dashes carry over.</summary>
	public abstract class Shape : FrameworkElement
	{
		public static readonly XF.BindableProperty FillProperty = Dp.Register<Shape>(nameof(Fill), typeof(Brush), null);
		public static readonly XF.BindableProperty StrokeProperty = Dp.Register<Shape>(nameof(Stroke), typeof(Brush), null);
		public static readonly XF.BindableProperty StrokeThicknessProperty = Dp.Register<Shape>(nameof(StrokeThickness), typeof(double), 1.0);
		public static readonly XF.BindableProperty StrokeDashArrayProperty = Dp.Register<Shape>(nameof(StrokeDashArray), typeof(DoubleCollection), null);
		public static readonly XF.BindableProperty StrokeDashOffsetProperty = Dp.Register<Shape>(nameof(StrokeDashOffset), typeof(double), 0.0);
		public static readonly XF.BindableProperty StretchProperty = Dp.Register<Shape>(nameof(Stretch), typeof(Stretch), Stretch.None);

		public Brush Fill
		{
			get => Get<Brush>(FillProperty);
			set => SetValue(FillProperty, value);
		}

		public Brush Stroke
		{
			get => Get<Brush>(StrokeProperty);
			set => SetValue(StrokeProperty, value);
		}

		public double StrokeThickness
		{
			get => Get<double>(StrokeThicknessProperty);
			set => SetValue(StrokeThicknessProperty, value);
		}

		public DoubleCollection StrokeDashArray
		{
			get => Get<DoubleCollection>(StrokeDashArrayProperty);
			set => SetValue(StrokeDashArrayProperty, value);
		}

		public double StrokeDashOffset
		{
			get => Get<double>(StrokeDashOffsetProperty);
			set => SetValue(StrokeDashOffsetProperty, value);
		}

		public Stretch Stretch
		{
			get => Get<Stretch>(StretchProperty);
			set => SetValue(StretchProperty, value);
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplyShape();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (HasNativeView)
				ApplyShape();
		}

		internal virtual void ApplyShape()
		{
			var shape = (XFShapes.Shape)NativeView;
			shape.Fill = ToForms(Fill);
			shape.Stroke = ToForms(Stroke);
			shape.StrokeThickness = StrokeThickness;
			shape.StrokeDashArray = StrokeDashArray?.ToForms();
			shape.StrokeDashOffset = StrokeDashOffset;
			shape.Aspect = Stretch == Stretch.Fill ? XF.Stretch.Fill : Stretch == Stretch.Uniform ? XF.Stretch.Uniform : Stretch == Stretch.UniformToFill ? XF.Stretch.UniformToFill : XF.Stretch.None;
		}

		static XF.Brush ToForms(Brush brush) => brush == null ? null : new XF.SolidColorBrush(brush.ToFormsColor());
	}

	public sealed class Line : Shape
	{
		public static readonly XF.BindableProperty X1Property = Dp.Register<Line>(nameof(X1), typeof(double), 0.0);
		public static readonly XF.BindableProperty Y1Property = Dp.Register<Line>(nameof(Y1), typeof(double), 0.0);
		public static readonly XF.BindableProperty X2Property = Dp.Register<Line>(nameof(X2), typeof(double), 0.0);
		public static readonly XF.BindableProperty Y2Property = Dp.Register<Line>(nameof(Y2), typeof(double), 0.0);

		public double X1
		{
			get => Get<double>(X1Property);
			set => SetValue(X1Property, value);
		}

		public double Y1
		{
			get => Get<double>(Y1Property);
			set => SetValue(Y1Property, value);
		}

		public double X2
		{
			get => Get<double>(X2Property);
			set => SetValue(X2Property, value);
		}

		public double Y2
		{
			get => Get<double>(Y2Property);
			set => SetValue(Y2Property, value);
		}

		internal override XF.View CreateNativeView() => new XFShapes.Line();

		/// <summary>The line in its own coordinates, from the top left of the element (WPF's own frame).</summary>
		internal override void ApplyShape()
		{
			base.ApplyShape();
			var line = (XFShapes.Line)NativeView;
			line.X1 = X1;
			line.Y1 = Y1;
			line.X2 = X2;
			line.Y2 = Y2;
			if (HorizontalAlignment == HorizontalAlignment.Stretch && double.IsNaN(Width))
				line.HorizontalOptions = XF.LayoutOptions.Start;
			if (VerticalAlignment == VerticalAlignment.Stretch && double.IsNaN(Height))
				line.VerticalOptions = XF.LayoutOptions.Start;
		}
	}

	public sealed class Rectangle : Shape
	{
		public static readonly XF.BindableProperty RadiusXProperty = Dp.Register<Rectangle>(nameof(RadiusX), typeof(double), 0.0);
		public static readonly XF.BindableProperty RadiusYProperty = Dp.Register<Rectangle>(nameof(RadiusY), typeof(double), 0.0);

		public Rectangle() => SetValue(StretchProperty, Stretch.Fill);

		public double RadiusX
		{
			get => Get<double>(RadiusXProperty);
			set => SetValue(RadiusXProperty, value);
		}

		public double RadiusY
		{
			get => Get<double>(RadiusYProperty);
			set => SetValue(RadiusYProperty, value);
		}

		internal override XF.View CreateNativeView() => new XFShapes.Rectangle();

		internal override void ApplyShape()
		{
			base.ApplyShape();
			var rectangle = (XFShapes.Rectangle)NativeView;
			rectangle.RadiusX = RadiusX;
			rectangle.RadiusY = RadiusY;
		}
	}

	public sealed class Ellipse : Shape
	{
		public Ellipse() => SetValue(StretchProperty, Stretch.Fill);

		internal override XF.View CreateNativeView() => new XFShapes.Ellipse();
	}
}

namespace System.Windows.Controls
{
	/// <summary>
	/// An image. <see cref="Stretch.None"/> shows it at its own size from the top left, clipped - what a VB6 image
	/// without Stretch shows - and the other modes scale it into the element.
	/// </summary>
	public class Image : FrameworkElement
	{
		public static readonly XF.BindableProperty SourceProperty = Dp.Register<Image>(nameof(Source), typeof(ImageSource), null);
		public static readonly XF.BindableProperty StretchProperty = Dp.Register<Image>(nameof(Stretch), typeof(Stretch), Stretch.Uniform);
		public static readonly XF.BindableProperty StretchDirectionProperty = Dp.Register<Image>(nameof(StretchDirection), typeof(StretchDirection), StretchDirection.Both);

		XF.Image _image;

		public ImageSource Source
		{
			get => Get<ImageSource>(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		public Stretch Stretch
		{
			get => Get<Stretch>(StretchProperty);
			set => SetValue(StretchProperty, value);
		}

		public StretchDirection StretchDirection
		{
			get => Get<StretchDirection>(StretchDirectionProperty);
			set => SetValue(StretchDirectionProperty, value);
		}

		internal override XF.View CreateNativeView()
		{
			_image = new XF.Image();
			return new XF.AbsoluteLayout { IsClippedToBounds = true, Children = { _image } };
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplyImage();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (HasNativeView && (p == SourceProperty || p == StretchProperty))
				ApplyImage();
		}

		void ApplyImage()
		{
			_image.Source = Source?.ToForms();
			var stretch = Stretch;
			_image.Aspect = stretch == Stretch.Fill ? XF.Aspect.Fill : stretch == Stretch.UniformToFill ? XF.Aspect.AspectFill : XF.Aspect.AspectFit;
			XF.AbsoluteLayout.SetLayoutFlags(_image, stretch == Stretch.None ? XF.AbsoluteLayoutFlags.None : XF.AbsoluteLayoutFlags.All);
			XF.AbsoluteLayout.SetLayoutBounds(_image, stretch == Stretch.None
				? new XF.Rectangle(0, 0, Source?.Width > 0 ? Source.Width : XF.AbsoluteLayout.AutoSize, Source?.Height > 0 ? Source.Height : XF.AbsoluteLayout.AutoSize)
				: new XF.Rectangle(0, 0, 1, 1));
		}
	}

	public enum StretchDirection
	{
		UpOnly,
		DownOnly,
		Both,
	}
}

namespace Xamarin.Forms.Wpf
{
	/// <summary>A Xamarin.Forms view that shows a GTK widget as it is: for what Xamarin.Forms has no view of its own (a calendar).</summary>
	internal sealed class NativeHostView : View
	{
		internal NativeHostView(Gtk.Widget widget) => Widget = widget;

		internal Gtk.Widget Widget { get; }
	}

	internal sealed class NativeHostRenderer : Platform.GTK.ViewRenderer<NativeHostView, Gtk.Widget>
	{
		protected override void OnElementChanged(Platform.GTK.ElementChangedEventArgs<NativeHostView> e)
		{
			if (e.NewElement != null && Control == null)
				SetNativeControl(e.NewElement.Widget);

			base.OnElementChanged(e);
		}
	}
}
