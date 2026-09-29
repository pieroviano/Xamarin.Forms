using System.Collections;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows.Controls
{
	/// <summary>A control: colors, border, padding, fonts, content alignment and tab order.</summary>
	public class Control : FrameworkElement
	{
		public Control() => SetValue(FocusableProperty, true);

		public static readonly XF.BindableProperty BackgroundProperty = Dp.Register<Control>(nameof(Background), typeof(Brush), null);
		public static readonly XF.BindableProperty ForegroundProperty = TextElement.ForegroundProperty;
		public static readonly XF.BindableProperty BorderBrushProperty = Dp.Register<Control>(nameof(BorderBrush), typeof(Brush), null);
		public static readonly XF.BindableProperty BorderThicknessProperty = Dp.Register<Control>(nameof(BorderThickness), typeof(Thickness), default(Thickness));
		public static readonly XF.BindableProperty PaddingProperty = Dp.Register<Control>(nameof(Padding), typeof(Thickness), default(Thickness));
		public static readonly XF.BindableProperty FontFamilyProperty = TextElement.FontFamilyProperty;
		public static readonly XF.BindableProperty FontSizeProperty = TextElement.FontSizeProperty;
		public static readonly XF.BindableProperty FontStyleProperty = TextElement.FontStyleProperty;
		public static readonly XF.BindableProperty FontWeightProperty = TextElement.FontWeightProperty;
		public static readonly XF.BindableProperty FontStretchProperty = TextElement.FontStretchProperty;
		public static readonly XF.BindableProperty HorizontalContentAlignmentProperty = Dp.Register<Control>(nameof(HorizontalContentAlignment), typeof(HorizontalAlignment), HorizontalAlignment.Left);
		public static readonly XF.BindableProperty VerticalContentAlignmentProperty = Dp.Register<Control>(nameof(VerticalContentAlignment), typeof(VerticalAlignment), VerticalAlignment.Top);
		public static readonly XF.BindableProperty IsTabStopProperty = Dp.Register<Control>(nameof(IsTabStop), typeof(bool), true);
		public static readonly XF.BindableProperty TabIndexProperty = Dp.Register<Control>(nameof(TabIndex), typeof(int), int.MaxValue);
		public static readonly XF.BindableProperty TemplateProperty = Dp.Register<Control>(nameof(Template), typeof(ControlTemplate), null);

		public Brush Background
		{
			get => Get<Brush>(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

		public Brush Foreground
		{
			get => Get<Brush>(ForegroundProperty);
			set => SetValue(ForegroundProperty, value);
		}

		public Brush BorderBrush
		{
			get => Get<Brush>(BorderBrushProperty);
			set => SetValue(BorderBrushProperty, value);
		}

		public Thickness BorderThickness
		{
			get => Get<Thickness>(BorderThicknessProperty);
			set => SetValue(BorderThicknessProperty, value);
		}

		public Thickness Padding
		{
			get => Get<Thickness>(PaddingProperty);
			set => SetValue(PaddingProperty, value);
		}

		public FontFamily FontFamily
		{
			get => Get<FontFamily>(FontFamilyProperty);
			set => SetValue(FontFamilyProperty, value);
		}

		public double FontSize
		{
			get => Get<double>(FontSizeProperty);
			set => SetValue(FontSizeProperty, value);
		}

		public FontStyle FontStyle
		{
			get => Get<FontStyle>(FontStyleProperty);
			set => SetValue(FontStyleProperty, value);
		}

		public FontWeight FontWeight
		{
			get => Get<FontWeight>(FontWeightProperty);
			set => SetValue(FontWeightProperty, value);
		}

		public FontStretch FontStretch
		{
			get => Get<FontStretch>(FontStretchProperty);
			set => SetValue(FontStretchProperty, value);
		}

		public HorizontalAlignment HorizontalContentAlignment
		{
			get => Get<HorizontalAlignment>(HorizontalContentAlignmentProperty);
			set => SetValue(HorizontalContentAlignmentProperty, value);
		}

		public VerticalAlignment VerticalContentAlignment
		{
			get => Get<VerticalAlignment>(VerticalContentAlignmentProperty);
			set => SetValue(VerticalContentAlignmentProperty, value);
		}

		public bool IsTabStop
		{
			get => Get<bool>(IsTabStopProperty);
			set => SetValue(IsTabStopProperty, value);
		}

		public int TabIndex
		{
			get => Get<int>(TabIndexProperty);
			set => SetValue(TabIndexProperty, value);
		}

		public ControlTemplate Template
		{
			get => Get<ControlTemplate>(TemplateProperty);
			set => SetValue(TemplateProperty, value);
		}

		public static readonly RoutedEvent PreviewMouseDoubleClickEvent =
			EventManager.RegisterRoutedEvent("PreviewMouseDoubleClick", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), typeof(Control));

		public static readonly RoutedEvent MouseDoubleClickEvent =
			EventManager.RegisterRoutedEvent("MouseDoubleClick", RoutingStrategy.Direct, typeof(MouseButtonEventHandler), typeof(Control));

		public event MouseButtonEventHandler PreviewMouseDoubleClick { add => AddHandler(PreviewMouseDoubleClickEvent, value); remove => RemoveHandler(PreviewMouseDoubleClickEvent, value); }

		public event MouseButtonEventHandler MouseDoubleClick { add => AddHandler(MouseDoubleClickEvent, value); remove => RemoveHandler(MouseDoubleClickEvent, value); }

		protected virtual void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
		{
		}

		protected virtual void OnMouseDoubleClick(MouseButtonEventArgs e)
		{
		}

		/// <summary>WPF: a second click of any button raises MouseDoubleClick on each control the MouseDown passes.</summary>
		internal override void OnMouseDownRouted(MouseButtonEventArgs e)
		{
			base.OnMouseDownRouted(e);
			if (e.ClickCount != 2)
				return;

			var d = new MouseButtonEventArgs(e.GetPosition, e.ChangedButton, e.ButtonState, e.ClickCount) { RoutedEvent = MouseDoubleClickEvent };
			d.SetSource(this);
			RaiseEvent(d);
			if (d.Handled)
				e.Handled = true;
		}

		internal override void OnOtherClassHandler(RoutedEventArgs e)
		{
			base.OnOtherClassHandler(e);
			if (e.RoutedEvent == MouseDoubleClickEvent)
				OnMouseDoubleClick((MouseButtonEventArgs)e);
			else if (e.RoutedEvent == PreviewMouseDoubleClickEvent)
				OnPreviewMouseDoubleClick((MouseButtonEventArgs)e);
		}

		// ---- the Xamarin.Forms view ----------------------------------------------------------------------------------

		/// <summary>The view that shows this control's text, whose font and color follow the control's.</summary>
		internal virtual XF.View TextView => NativeView;

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplyBackground();
			ApplyBorder();
			ApplyText();
			NativeView.IsTabStop = IsTabStop && Focusable;
			NativeView.TabIndex = TabIndex;
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (!HasNativeView)
				return;

			var p = e.Property.Bindable;
			if (p == BackgroundProperty)
				ApplyBackground();
			else if (p == BorderBrushProperty || p == BorderThicknessProperty || p == PaddingProperty)
				ApplyBorder();
			else if (p == ForegroundProperty || p == FontFamilyProperty || p == FontSizeProperty || p == FontStyleProperty
				|| p == FontWeightProperty || p == HorizontalContentAlignmentProperty || p == VerticalContentAlignmentProperty)
				ApplyText();
			else if (p == IsTabStopProperty)
				NativeView.IsTabStop = IsTabStop && Focusable;
			else if (p == TabIndexProperty)
				NativeView.TabIndex = TabIndex;
		}

		internal virtual void ApplyBackground() =>
			NativeView.BackgroundColor = Background?.ToFormsColor() ?? XF.Color.Default;

		/// <summary>Border and padding: a control drawn in a <see cref="NativeShell"/> gets both from it.</summary>
		internal virtual void ApplyBorder()
		{
			if (NativeView is NativeShell shell)
				shell.SetBorder(BorderBrush?.ToFormsColor() ?? XF.Color.Default, BorderThickness, Padding);
		}

		internal virtual void ApplyText()
		{
			var view = TextView;
			if (view == null)
				return;

			NativeText.ApplyFont(view, this);
			NativeText.ApplyForeground(view, this);
		}
	}

	/// <summary>A control's visual tree in WPF; there are no templates here, so it is kept but never applied.</summary>
	public class ControlTemplate
	{
		public ControlTemplate()
		{
		}

		public ControlTemplate(Type targetType) => TargetType = targetType;

		public Type TargetType { get; set; }
	}

	public class DataTemplate
	{
		public DataTemplate()
		{
		}

		public DataTemplate(object dataType) => DataType = dataType;

		public object DataType { get; set; }
	}

	/// <summary>A control with a single piece of content: text, or an element.</summary>
	[XF.ContentProperty(nameof(Content))]
	public class ContentControl : Control
	{
		public static readonly XF.BindableProperty ContentProperty = Dp.Register<ContentControl>(nameof(Content), typeof(object), null);
		public static readonly XF.BindableProperty ContentTemplateProperty = Dp.Register<ContentControl>(nameof(ContentTemplate), typeof(DataTemplate), null);
		public static readonly XF.BindableProperty ContentStringFormatProperty = Dp.Register<ContentControl>(nameof(ContentStringFormat), typeof(string), null);

		public object Content
		{
			get => Get<object>(ContentProperty);
			set => SetValue(ContentProperty, value);
		}

		public DataTemplate ContentTemplate
		{
			get => Get<DataTemplate>(ContentTemplateProperty);
			set => SetValue(ContentTemplateProperty, value);
		}

		public string ContentStringFormat
		{
			get => Get<string>(ContentStringFormatProperty);
			set => SetValue(ContentStringFormatProperty, value);
		}

		public bool HasContent => Content != null;

		protected virtual void OnContentChanged(object oldContent, object newContent)
		{
		}

		/// <summary>The content as text, for a native view that can only show text; access keys stripped.</summary>
		internal string ContentText
		{
			get
			{
				var content = Content;
				switch (content)
				{
					case null:
						return string.Empty;
					case string s:
						return NativeText.StripAccessKey(s);
					case TextBlock tb:
						return tb.Text;
					case AccessText at:
						return NativeText.StripAccessKey(at.Text);
					case ContentControl cc:
						return cc.ContentText;
					default:
						return string.IsNullOrEmpty(ContentStringFormat) ? content.ToString() : string.Format(ContentStringFormat, content);
				}
			}
		}

		internal override IEnumerable LogicalChildrenCore => Content == null ? Array.Empty<object>() : new[] { Content };

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.Property.Bindable == ContentProperty)
			{
				RemoveLogicalChild(e.OldValue);
				AddLogicalChild(e.NewValue);
			}

			base.OnPropertyChanged(e);

			if (e.Property.Bindable == ContentProperty)
			{
				if (HasNativeView)
					ApplyContent();
				OnContentChanged(e.OldValue, e.NewValue);
			}
		}

		internal override XF.View CreateNativeView() => new NativeShell();

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplyContent();
		}

		/// <summary>The content into the native view: an element's own view, anything else as text.</summary>
		internal virtual void ApplyContent()
		{
			if (!(NativeView is NativeShell shell))
				return;

			if (Content is UIElement element)
			{
				shell.Inner = element.NativeView;
			}
			else
			{
				if (!(shell.Inner is XF.Label label))
					shell.Inner = label = new XF.Label();
				label.Text = ContentText;
				ApplyText();
			}
		}

		internal override XF.View TextView => (NativeView as NativeShell)?.Inner as XF.Label;

		internal override void ApplyText()
		{
			base.ApplyText();
			if (TextView is XF.Label label)
			{
				label.HorizontalTextAlignment = NativeText.ToForms(HorizontalContentAlignment);
				label.VerticalTextAlignment = NativeText.ToForms(VerticalContentAlignment);
			}
		}
	}

	[XF.ContentProperty(nameof(Content))]
	public class HeaderedContentControl : ContentControl
	{
		public static readonly XF.BindableProperty HeaderProperty = Dp.Register<HeaderedContentControl>(nameof(Header), typeof(object), null);

		public object Header
		{
			get => Get<object>(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public bool HasHeader => Header != null;

		/// <summary>The header as text; access keys stripped.</summary>
		internal string HeaderText => Header is string s ? NativeText.StripAccessKey(s) : Header?.ToString() ?? string.Empty;

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.Property.Bindable == HeaderProperty)
			{
				RemoveLogicalChild(e.OldValue);
				AddLogicalChild(e.NewValue);
			}

			base.OnPropertyChanged(e);
			if (e.Property.Bindable == HeaderProperty && HasNativeView)
				ApplyHeader();
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			ApplyHeader();
		}

		internal virtual void ApplyHeader()
		{
		}
	}

	public class UserControl : ContentControl
	{
		public UserControl() => SetValue(FocusableProperty, false);
	}

	/// <summary>Text with an access key: the part of a <see cref="Label"/> WPF uses for underscores.</summary>
	[XF.ContentProperty(nameof(Text))]
	public class AccessText : FrameworkElement
	{
		public static readonly XF.BindableProperty TextProperty = Dp.Register<AccessText>(nameof(Text), typeof(string), string.Empty);

		public string Text
		{
			get => Get<string>(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public char AccessKey => NativeText.AccessKeyOf(Text);

		internal override XF.View CreateNativeView() => new XF.Label();

		internal override void SyncNative()
		{
			base.SyncNative();
			((XF.Label)NativeView).Text = NativeText.StripAccessKey(Text);
			NativeText.ApplyFont(NativeView, this);
			NativeText.ApplyForeground(NativeView, this);
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (HasNativeView && e.Property.Bindable == TextProperty)
				((XF.Label)NativeView).Text = NativeText.StripAccessKey(Text);
		}
	}

	/// <summary>A caption: its content is text (with an access key), drawn by a Xamarin.Forms label.</summary>
	public class Label : ContentControl
	{
		public Label()
		{
			SetValue(FocusableProperty, false);
			SetValue(IsTabStopProperty, false);
		}

		public static readonly XF.BindableProperty TargetProperty = Dp.Register<Label>(nameof(Target), typeof(UIElement), null);

		/// <summary>The element the access key of this label focuses.</summary>
		public UIElement Target
		{
			get => Get<UIElement>(TargetProperty);
			set => SetValue(TargetProperty, value);
		}
	}

	/// <summary>Text, drawn by a Xamarin.Forms label.</summary>
	[XF.ContentProperty(nameof(Inlines))]
	public class TextBlock : FrameworkElement
	{
		InlineCollection _inlines;

		public static readonly XF.BindableProperty TextProperty = Dp.Register<TextBlock>(nameof(Text), typeof(string), string.Empty);
		public static readonly XF.BindableProperty BackgroundProperty = Dp.Register<TextBlock>(nameof(Background), typeof(Brush), null);
		public static readonly XF.BindableProperty ForegroundProperty = TextElement.ForegroundProperty;
		public static readonly XF.BindableProperty FontFamilyProperty = TextElement.FontFamilyProperty;
		public static readonly XF.BindableProperty FontSizeProperty = TextElement.FontSizeProperty;
		public static readonly XF.BindableProperty FontStyleProperty = TextElement.FontStyleProperty;
		public static readonly XF.BindableProperty FontWeightProperty = TextElement.FontWeightProperty;
		public static readonly XF.BindableProperty FontStretchProperty = TextElement.FontStretchProperty;
		public static readonly XF.BindableProperty TextAlignmentProperty = Dp.Register<TextBlock>(nameof(TextAlignment), typeof(TextAlignment), TextAlignment.Left);
		public static readonly XF.BindableProperty TextWrappingProperty = Dp.Register<TextBlock>(nameof(TextWrapping), typeof(TextWrapping), TextWrapping.NoWrap);
		public static readonly XF.BindableProperty TextTrimmingProperty = Dp.Register<TextBlock>(nameof(TextTrimming), typeof(TextTrimming), TextTrimming.None);
		public static readonly XF.BindableProperty PaddingProperty = Dp.Register<TextBlock>(nameof(Padding), typeof(Thickness), default(Thickness));

		public TextBlock()
		{
		}

		public TextBlock(Inline inline) => Inlines.Add(inline);

		public string Text
		{
			get => Get<string>(TextProperty);
			set => SetValue(TextProperty, value ?? string.Empty);
		}

		public InlineCollection Inlines => _inlines ?? (_inlines = new InlineCollection());

		public Brush Background
		{
			get => Get<Brush>(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

		public Brush Foreground
		{
			get => Get<Brush>(ForegroundProperty);
			set => SetValue(ForegroundProperty, value);
		}

		public FontFamily FontFamily
		{
			get => Get<FontFamily>(FontFamilyProperty);
			set => SetValue(FontFamilyProperty, value);
		}

		public double FontSize
		{
			get => Get<double>(FontSizeProperty);
			set => SetValue(FontSizeProperty, value);
		}

		public FontStyle FontStyle
		{
			get => Get<FontStyle>(FontStyleProperty);
			set => SetValue(FontStyleProperty, value);
		}

		public FontWeight FontWeight
		{
			get => Get<FontWeight>(FontWeightProperty);
			set => SetValue(FontWeightProperty, value);
		}

		public FontStretch FontStretch
		{
			get => Get<FontStretch>(FontStretchProperty);
			set => SetValue(FontStretchProperty, value);
		}

		public TextAlignment TextAlignment
		{
			get => Get<TextAlignment>(TextAlignmentProperty);
			set => SetValue(TextAlignmentProperty, value);
		}

		public TextWrapping TextWrapping
		{
			get => Get<TextWrapping>(TextWrappingProperty);
			set => SetValue(TextWrappingProperty, value);
		}

		public TextTrimming TextTrimming
		{
			get => Get<TextTrimming>(TextTrimmingProperty);
			set => SetValue(TextTrimmingProperty, value);
		}

		public Thickness Padding
		{
			get => Get<Thickness>(PaddingProperty);
			set => SetValue(PaddingProperty, value);
		}

		/// <summary>The text shown: <see cref="Text"/>, or the inlines when there is no text.</summary>
		string ShownText => string.IsNullOrEmpty(Text) && _inlines != null && _inlines.Count > 0
			? string.Concat(System.Linq.Enumerable.Select(_inlines, i => i.PlainText))
			: Text;

		internal override XF.View CreateNativeView() => new XF.Label();

		internal override void SyncNative()
		{
			base.SyncNative();
			Apply(null);
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (HasNativeView)
				Apply(e.Property.Bindable);
		}

		void Apply(XF.BindableProperty p)
		{
			var label = (XF.Label)NativeView;
			if (p == null || p == TextProperty)
				label.Text = ShownText;
			if (p == null || p == BackgroundProperty)
				label.BackgroundColor = Background?.ToFormsColor() ?? XF.Color.Default;
			if (p == null || p == TextAlignmentProperty)
				label.HorizontalTextAlignment = NativeText.ToForms(TextAlignment);
			if (p == null || p == TextWrappingProperty || p == TextTrimmingProperty)
				label.LineBreakMode = TextWrapping != TextWrapping.NoWrap ? XF.LineBreakMode.WordWrap
					: TextTrimming != TextTrimming.None ? XF.LineBreakMode.TailTruncation : XF.LineBreakMode.NoWrap;
			if (p == null || p == PaddingProperty)
				label.Padding = NativeText.ToForms(Padding);
			if (p == null || p == ForegroundProperty || p == FontFamilyProperty || p == FontSizeProperty || p == FontStyleProperty || p == FontWeightProperty)
			{
				NativeText.ApplyFont(label, this);
				NativeText.ApplyForeground(label, this);
			}
		}
	}

	/// <summary>An element with a single child: WPF's base of <see cref="Border"/>.</summary>
	[XF.ContentProperty(nameof(Child))]
	public class Decorator : FrameworkElement
	{
		UIElement _child;

		public virtual UIElement Child
		{
			get => _child;
			set
			{
				if (_child == value)
					return;

				RemoveLogicalChild(_child);
				_child = value;
				AddLogicalChild(value);
				if (HasNativeView)
					((NativeShell)NativeView).Inner = value?.NativeView;
			}
		}

		internal override IEnumerable LogicalChildrenCore => _child == null ? Array.Empty<object>() : new object[] { _child };

		internal override XF.View CreateNativeView() => new NativeShell(_child?.NativeView);
	}

	public class Border : Decorator
	{
		public static readonly XF.BindableProperty BackgroundProperty = Dp.Register<Border>(nameof(Background), typeof(Brush), null);
		public static readonly XF.BindableProperty BorderBrushProperty = Dp.Register<Border>(nameof(BorderBrush), typeof(Brush), null);
		public static readonly XF.BindableProperty BorderThicknessProperty = Dp.Register<Border>(nameof(BorderThickness), typeof(Thickness), default(Thickness));
		public static readonly XF.BindableProperty PaddingProperty = Dp.Register<Border>(nameof(Padding), typeof(Thickness), default(Thickness));
		public static readonly XF.BindableProperty CornerRadiusProperty = Dp.Register<Border>(nameof(CornerRadius), typeof(CornerRadius), default(CornerRadius));

		public Brush Background
		{
			get => Get<Brush>(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

		public Brush BorderBrush
		{
			get => Get<Brush>(BorderBrushProperty);
			set => SetValue(BorderBrushProperty, value);
		}

		public Thickness BorderThickness
		{
			get => Get<Thickness>(BorderThicknessProperty);
			set => SetValue(BorderThicknessProperty, value);
		}

		public Thickness Padding
		{
			get => Get<Thickness>(PaddingProperty);
			set => SetValue(PaddingProperty, value);
		}

		public CornerRadius CornerRadius
		{
			get => Get<CornerRadius>(CornerRadiusProperty);
			set => SetValue(CornerRadiusProperty, value);
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			Apply();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (HasNativeView && (p == BackgroundProperty || p == BorderBrushProperty || p == BorderThicknessProperty || p == PaddingProperty))
				Apply();
		}

		void Apply()
		{
			var shell = (NativeShell)NativeView;
			shell.BackgroundColor = Background?.ToFormsColor() ?? XF.Color.Default;
			shell.SetBorder(BorderBrush?.ToFormsColor() ?? XF.Color.Default, BorderThickness, Padding);
		}
	}

	/// <summary>
	/// A frame with a caption. Its content sits inside the frame at WPF's inset, so a child laid out against the
	/// group box's own origin (as a converted VB6 frame's children are, with a compensating margin) lands where it did.
	/// </summary>
	public class GroupBox : HeaderedContentControl
	{
		/// <summary>Where WPF's default template puts the content, relative to the group box.</summary>
		static readonly Thickness s_contentInset = new Thickness(6, 17, 6, 6);

		XF.Label _header;
		XF.Frame _frame;
		XF.ContentView _content;

		public GroupBox()
		{
			SetValue(FocusableProperty, false);
			SetValue(IsTabStopProperty, false);
			SetValue(BorderThicknessProperty, new Thickness(1));
		}

		internal override XF.View CreateNativeView()
		{
			_frame = new XF.Frame { HasShadow = false, CornerRadius = 3, Padding = 0, BackgroundColor = XF.Color.Transparent, Margin = new XF.Thickness(0, 7, 0, 0) };
			_content = new XF.ContentView { Padding = new XF.Thickness(s_contentInset.Left, s_contentInset.Top, s_contentInset.Right, s_contentInset.Bottom) };
			_header = new XF.Label { Margin = new XF.Thickness(8, 0, 0, 0), Padding = new XF.Thickness(2, 0, 2, 0), HorizontalOptions = XF.LayoutOptions.Start, VerticalOptions = XF.LayoutOptions.Start };

			var root = new XF.Grid { RowSpacing = 0, ColumnSpacing = 0, IsClippedToBounds = false };
			root.Children.Add(_frame);
			root.Children.Add(_content);
			root.Children.Add(_header);
			return root;
		}

		internal override XF.View TextView => _header;

		internal override void ApplyContent()
		{
			if (_content == null)
				return;

			_content.Content = Content is UIElement element ? element.NativeView : Content == null ? null : new XF.Label { Text = ContentText };
		}

		internal override void ApplyHeader()
		{
			if (_header == null)
				return;

			_header.Text = HeaderText;
			_header.IsVisible = _header.Text.Length > 0;
		}

		internal override void ApplyBackground()
		{
			NativeView.BackgroundColor = Background?.ToFormsColor() ?? XF.Color.Default;
			_header.BackgroundColor = Background?.ToFormsColor() ?? InheritedBackground();
		}

		/// <summary>The header covers the frame line, so it takes the background it sits on.</summary>
		XF.Color InheritedBackground()
		{
			for (var d = LogicalParent; d != null; d = d.LogicalParent)
			{
				switch (d)
				{
					case Control c when c.Background != null:
						return c.Background.ToFormsColor();
					case Panel p when p.Background != null:
						return p.Background.ToFormsColor();
				}
			}

			return SystemColors.ControlBrush.ToFormsColor();
		}

		internal override void ApplyBorder()
		{
			var visible = BorderThickness.Left > 0 || BorderThickness.Top > 0 || BorderThickness.Right > 0 || BorderThickness.Bottom > 0;
			_frame.IsVisible = visible;
			_frame.BorderColor = BorderBrush?.ToFormsColor() ?? SystemColors.ControlDarkBrush.ToFormsColor();
		}
	}
}
