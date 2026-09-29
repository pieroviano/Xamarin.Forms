using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows.Controls
{
	public enum ScrollBarVisibility
	{
		Disabled,
		Auto,
		Hidden,
		Visible,
	}

	public enum CharacterCasing
	{
		Normal,
		Lower,
		Upper,
	}

	public enum UndoAction
	{
		None,
		Merge,
		Undo,
		Redo,
		Clear,
		Create,
	}

	public class TextChangedEventArgs : RoutedEventArgs
	{
		public TextChangedEventArgs(RoutedEvent id, UndoAction action) : base(id) => UndoAction = action;

		public UndoAction UndoAction { get; }

		protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
		{
			if (genericHandler is TextChangedEventHandler typed)
				typed(genericTarget, this);
			else
				base.InvokeEventHandler(genericHandler, genericTarget);
		}
	}

	public delegate void TextChangedEventHandler(object sender, TextChangedEventArgs e);
}

namespace System.Windows.Controls.Primitives
{
	/// <summary>The editing part of text boxes: read-only, Enter and Tab, scrolling, and the change events.</summary>
	public abstract class TextBoxBase : Control
	{
		public static readonly RoutedEvent TextChangedEvent =
			EventManager.RegisterRoutedEvent("TextChanged", RoutingStrategy.Bubble, typeof(TextChangedEventHandler), typeof(TextBoxBase));

		public static readonly RoutedEvent SelectionChangedEvent =
			EventManager.RegisterRoutedEvent("SelectionChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TextBoxBase));

		public static readonly XF.BindableProperty IsReadOnlyProperty = Dp.Register<TextBoxBase>(nameof(IsReadOnly), typeof(bool), false);
		public static readonly XF.BindableProperty AcceptsReturnProperty = Dp.Register<TextBoxBase>(nameof(AcceptsReturn), typeof(bool), false);
		public static readonly XF.BindableProperty AcceptsTabProperty = Dp.Register<TextBoxBase>(nameof(AcceptsTab), typeof(bool), false);
		public static readonly XF.BindableProperty HorizontalScrollBarVisibilityProperty = Dp.Register<TextBoxBase>(nameof(HorizontalScrollBarVisibility), typeof(ScrollBarVisibility), ScrollBarVisibility.Hidden);
		public static readonly XF.BindableProperty VerticalScrollBarVisibilityProperty = Dp.Register<TextBoxBase>(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), ScrollBarVisibility.Hidden);
		public static readonly XF.BindableProperty IsUndoEnabledProperty = Dp.Register<TextBoxBase>(nameof(IsUndoEnabled), typeof(bool), true);

		protected TextBoxBase()
		{
			SetValue(BorderThicknessProperty, new Thickness(1));
		}

		public bool IsReadOnly
		{
			get => Get<bool>(IsReadOnlyProperty);
			set => SetValue(IsReadOnlyProperty, value);
		}

		public bool AcceptsReturn
		{
			get => Get<bool>(AcceptsReturnProperty);
			set => SetValue(AcceptsReturnProperty, value);
		}

		public bool AcceptsTab
		{
			get => Get<bool>(AcceptsTabProperty);
			set => SetValue(AcceptsTabProperty, value);
		}

		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get => Get<ScrollBarVisibility>(HorizontalScrollBarVisibilityProperty);
			set => SetValue(HorizontalScrollBarVisibilityProperty, value);
		}

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get => Get<ScrollBarVisibility>(VerticalScrollBarVisibilityProperty);
			set => SetValue(VerticalScrollBarVisibilityProperty, value);
		}

		public bool IsUndoEnabled
		{
			get => Get<bool>(IsUndoEnabledProperty);
			set => SetValue(IsUndoEnabledProperty, value);
		}

		public event TextChangedEventHandler TextChanged { add => AddHandler(TextChangedEvent, value); remove => RemoveHandler(TextChangedEvent, value); }

		public event RoutedEventHandler SelectionChanged { add => AddHandler(SelectionChangedEvent, value); remove => RemoveHandler(SelectionChangedEvent, value); }

		protected virtual void OnTextChanged(TextChangedEventArgs e) => RaiseEvent(e);

		protected virtual void OnSelectionChanged(RoutedEventArgs e) => RaiseEvent(e);

		internal void RaiseTextChanged() => OnTextChanged(new TextChangedEventArgs(TextChangedEvent, UndoAction.None));

		internal void RaiseSelectionChanged() => OnSelectionChanged(new RoutedEventArgs(SelectionChangedEvent, this));

		public virtual void SelectAll()
		{
		}

		public void Copy()
		{
		}

		public void Cut()
		{
		}

		public void Paste()
		{
		}

		public bool Undo() => false;

		public bool Redo() => false;

		public void ScrollToEnd()
		{
		}

		public void ScrollToHome()
		{
		}

		public void LineUp()
		{
		}

		public void LineDown()
		{
		}
	}
}

namespace System.Windows.Controls
{
	/// <summary>
	/// A text box: one line over a Xamarin.Forms entry, several (Enter accepted, or wrapping) over an editor. The
	/// view changes kind when those properties do.
	/// </summary>
	public class TextBox : TextBoxBase
	{
		bool _syncing;
		int _selectionStart, _selectionLength;

		public static readonly XF.BindableProperty TextProperty = Dp.Register<TextBox>(nameof(Text), typeof(string), string.Empty, mode: XF.BindingMode.TwoWay);
		public static readonly XF.BindableProperty MaxLengthProperty = Dp.Register<TextBox>(nameof(MaxLength), typeof(int), 0);
		public static readonly XF.BindableProperty TextAlignmentProperty = Dp.Register<TextBox>(nameof(TextAlignment), typeof(TextAlignment), TextAlignment.Left);
		public static readonly XF.BindableProperty TextWrappingProperty = Dp.Register<TextBox>(nameof(TextWrapping), typeof(TextWrapping), TextWrapping.NoWrap);
		public static readonly XF.BindableProperty CharacterCasingProperty = Dp.Register<TextBox>(nameof(CharacterCasing), typeof(CharacterCasing), CharacterCasing.Normal);
		public static readonly XF.BindableProperty MinLinesProperty = Dp.Register<TextBox>(nameof(MinLines), typeof(int), 1);
		public static readonly XF.BindableProperty MaxLinesProperty = Dp.Register<TextBox>(nameof(MaxLines), typeof(int), int.MaxValue);

		public string Text
		{
			get => Get<string>(TextProperty);
			set => SetValue(TextProperty, Cased(value ?? string.Empty));
		}

		public int MaxLength
		{
			get => Get<int>(MaxLengthProperty);
			set => SetValue(MaxLengthProperty, value);
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

		public CharacterCasing CharacterCasing
		{
			get => Get<CharacterCasing>(CharacterCasingProperty);
			set => SetValue(CharacterCasingProperty, value);
		}

		public int MinLines
		{
			get => Get<int>(MinLinesProperty);
			set => SetValue(MinLinesProperty, value);
		}

		public int MaxLines
		{
			get => Get<int>(MaxLinesProperty);
			set => SetValue(MaxLinesProperty, value);
		}

		/// <summary>Where the selection starts (the caret, when nothing is selected).</summary>
		public int SelectionStart
		{
			get
			{
				ReadSelection();
				return _selectionStart;
			}
			set => Select(value, SelectionLength);
		}

		public int SelectionLength
		{
			get
			{
				ReadSelection();
				return _selectionLength;
			}
			set => Select(SelectionStart, value);
		}

		/// <summary>The selected text; setting it replaces the selection and selects the new text.</summary>
		public string SelectedText
		{
			get
			{
				var text = Text;
				var start = Math.Min(SelectionStart, text.Length);
				return text.Substring(start, Math.Min(SelectionLength, text.Length - start));
			}
			set
			{
				var text = Text;
				var start = Math.Min(SelectionStart, text.Length);
				var length = Math.Min(SelectionLength, text.Length - start);
				var insert = value ?? string.Empty;
				Text = text.Substring(0, start) + insert + text.Substring(start + length);
				Select(start, insert.Length);
			}
		}

		public int CaretIndex
		{
			get => SelectionStart;
			set => Select(value, 0);
		}

		public int LineCount => Text.Split('\n').Length;

		public void Select(int start, int length)
		{
			if (start < 0)
				throw new ArgumentOutOfRangeException(nameof(start));
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length));

			var textLength = Text.Length;
			_selectionStart = Math.Min(start, textLength);
			_selectionLength = Math.Min(length, textLength - _selectionStart);
			if (NativeView is NativeShell shell && shell.Inner is XF.Entry entry)
			{
				entry.CursorPosition = _selectionStart;
				entry.SelectionLength = _selectionLength;
			}

			RaiseSelectionChanged();
		}

		public override void SelectAll() => Select(0, Text.Length);

		public void Clear() => Text = string.Empty;

		public void AppendText(string textData) => Text += textData;

		public string GetLineText(int lineIndex)
		{
			var lines = Text.Split('\n');
			return lineIndex >= 0 && lineIndex < lines.Length ? lines[lineIndex] + (lineIndex < lines.Length - 1 ? "\n" : string.Empty) : throw new ArgumentOutOfRangeException(nameof(lineIndex));
		}

		public void ScrollToLine(int lineIndex)
		{
		}

		string Cased(string text)
		{
			switch (CharacterCasing)
			{
				case CharacterCasing.Upper:
					return text.ToUpperInvariant();
				case CharacterCasing.Lower:
					return text.ToLowerInvariant();
				default:
					return text;
			}
		}

		/// <summary>An entry keeps its caret and selection itself; read them back when asked.</summary>
		void ReadSelection()
		{
			if (NativeView is NativeShell shell && shell.Inner is XF.Entry entry)
			{
				_selectionStart = Math.Max(0, Math.Min(entry.CursorPosition, Text.Length));
				_selectionLength = Math.Max(0, Math.Min(entry.SelectionLength, Text.Length - _selectionStart));
			}
		}

		bool Multiline => AcceptsReturn || TextWrapping != TextWrapping.NoWrap;

		internal override XF.View CreateNativeView() => new NativeShell();

		internal override XF.View TextView => (NativeView as NativeShell)?.Inner;

		internal override void SyncNative()
		{
			EnsureInnerView();
			base.SyncNative();
			ApplyEditing();
		}

		/// <summary>An entry, or an editor for several lines: the one this text box needs, created or swapped.</summary>
		void EnsureInnerView()
		{
			var shell = (NativeShell)NativeView;
			if (Multiline ? shell.Inner is XF.Editor : shell.Inner is XF.Entry)
				return;

			XF.InputView view;
			if (Multiline)
			{
				var editor = new XF.Editor { AutoSize = XF.EditorAutoSizeOption.Disabled };
				editor.TextChanged += OnNativeTextChanged;
				view = editor;
			}
			else
			{
				var entry = new XF.Entry();
				entry.TextChanged += OnNativeTextChanged;
				entry.PropertyChanged += (s, e) =>
				{
					if (e.PropertyName == XF.Entry.CursorPositionProperty.PropertyName || e.PropertyName == XF.Entry.SelectionLengthProperty.PropertyName)
						RaiseSelectionChanged();
				};
				view = entry;
			}

			shell.Inner = view;
			ApplyText();
			ApplyEditing();
		}

		void OnNativeTextChanged(object sender, XF.TextChangedEventArgs e)
		{
			if (_syncing)
				return;

			var text = e.NewTextValue ?? string.Empty;
			var cased = Cased(text);
			SetValue(TextProperty, cased);
			if (cased != text)
				PushText();
		}

		void PushText()
		{
			var shell = (NativeShell)NativeView;
			_syncing = true;
			try
			{
				switch (shell.Inner)
				{
					case XF.Entry entry when entry.Text != Text:
						entry.Text = Text;
						break;
					case XF.Editor editor when editor.Text != Text:
						editor.Text = Text;
						break;
				}
			}
			finally
			{
				_syncing = false;
			}
		}

		void ApplyEditing()
		{
			var shell = (NativeShell)NativeView;
			PushText();
			var maxLength = MaxLength > 0 ? MaxLength : int.MaxValue;
			switch (shell.Inner)
			{
				case XF.Entry entry:
					entry.MaxLength = maxLength;
					entry.IsReadOnly = IsReadOnly;
					entry.HorizontalTextAlignment = NativeText.ToForms(TextAlignment);
					break;
				case XF.Editor editor:
					editor.MaxLength = maxLength;
					editor.IsReadOnly = IsReadOnly;
					break;
			}
		}

		/// <summary>No border (a VB6 text box with BorderStyle 0) is an entry without its frame.</summary>
		internal override void ApplyBorder()
		{
			var thickness = BorderThickness;
			var frameless = thickness.Left <= 0 && thickness.Top <= 0 && thickness.Right <= 0 && thickness.Bottom <= 0;
			if (NativeInput.WidgetOf(this) is Gtk.Widget widget)
			{
				foreach (var entry in GtkWidgets.Descendants<Gtk.Entry>(widget))
					entry.HasFrame = !frameless;
			}

			((NativeShell)NativeView).Padding = NativeText.ToForms(Padding);
		}

		internal override void OnWidgetAttached(Gtk.Widget widget)
		{
			base.OnWidgetAttached(widget);
			ApplyBorder();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			var p = e.Property.Bindable;
			if (p == TextProperty)
			{
				if (HasNativeView)
					PushText();

				var length = Text.Length;
				if (_selectionStart > length)
					_selectionStart = length;
				if (_selectionStart + _selectionLength > length)
					_selectionLength = length - _selectionStart;
				RaiseTextChanged();
				return;
			}

			if (!HasNativeView)
				return;

			if (p == AcceptsReturnProperty || p == TextWrappingProperty)
				EnsureInnerView();
			else if (p == MaxLengthProperty || p == IsReadOnlyProperty || p == TextAlignmentProperty)
				ApplyEditing();
			else if (p == CharacterCasingProperty)
				Text = Text;
		}
	}

	/// <summary>A password entry: the text is shown masked.</summary>
	public sealed class PasswordBox : Control
	{
		bool _syncing;

		public static readonly RoutedEvent PasswordChangedEvent =
			EventManager.RegisterRoutedEvent("PasswordChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PasswordBox));

		public static readonly XF.BindableProperty PasswordCharProperty = Dp.Register<PasswordBox>(nameof(PasswordChar), typeof(char), '●');
		public static readonly XF.BindableProperty MaxLengthProperty = Dp.Register<PasswordBox>(nameof(MaxLength), typeof(int), 0);

		string _password = string.Empty;

		public string Password
		{
			get => _password;
			set
			{
				_password = value ?? string.Empty;
				if (HasNativeView && NativeView is XF.Entry entry && entry.Text != _password)
				{
					_syncing = true;
					entry.Text = _password;
					_syncing = false;
				}

				RaiseEvent(new RoutedEventArgs(PasswordChangedEvent, this));
			}
		}

		public char PasswordChar
		{
			get => Get<char>(PasswordCharProperty);
			set => SetValue(PasswordCharProperty, value);
		}

		public int MaxLength
		{
			get => Get<int>(MaxLengthProperty);
			set => SetValue(MaxLengthProperty, value);
		}

		public event RoutedEventHandler PasswordChanged { add => AddHandler(PasswordChangedEvent, value); remove => RemoveHandler(PasswordChangedEvent, value); }

		public void Clear() => Password = string.Empty;

		public void SelectAll()
		{
		}

		internal override XF.View CreateNativeView()
		{
			var entry = new XF.Entry { IsPassword = true };
			entry.TextChanged += (s, e) =>
			{
				if (!_syncing)
					Password = e.NewTextValue;
			};
			return entry;
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			var entry = (XF.Entry)NativeView;
			entry.Text = _password;
			entry.MaxLength = MaxLength > 0 ? MaxLength : int.MaxValue;
		}
	}

	/// <summary>A rich text box, as far as its text goes: the document's paragraphs in a Xamarin.Forms editor.</summary>
	[XF.ContentProperty(nameof(Document))]
	public class RichTextBox : TextBoxBase
	{
		FlowDocument _document;
		bool _syncing;

		public RichTextBox() : this(new FlowDocument(new Paragraph()))
		{
		}

		public RichTextBox(FlowDocument document)
		{
			SetValue(AcceptsReturnProperty, true);
			Document = document;
		}

		public FlowDocument Document
		{
			get => _document;
			set
			{
				if (_document != null)
				{
					_document.TextReplaced -= OnDocumentTextReplaced;
					RemoveLogicalChild(_document);
				}

				_document = value ?? new FlowDocument();
				_document.TextReplaced += OnDocumentTextReplaced;
				AddLogicalChild(_document);
				OnDocumentTextReplaced(this, EventArgs.Empty);
			}
		}

		public TextPointer CaretPosition => _document.ContentEnd;

		public TextSelection Selection => new TextSelection(_document.ContentStart, _document.ContentEnd);

		internal override System.Collections.IEnumerable LogicalChildrenCore => new object[] { _document };

		internal override XF.View CreateNativeView()
		{
			var editor = new XF.Editor { AutoSize = XF.EditorAutoSizeOption.Disabled };
			editor.TextChanged += (s, e) =>
			{
				if (_syncing)
					return;

				_syncing = true;
				try
				{
					_document.Text = e.NewTextValue ?? string.Empty;
				}
				finally
				{
					_syncing = false;
				}

				RaiseTextChanged();
			};
			return editor;
		}

		internal override void SyncNative()
		{
			base.SyncNative();
			var editor = (XF.Editor)NativeView;
			_syncing = true;
			editor.Text = _document.Text;
			_syncing = false;
			editor.IsReadOnly = IsReadOnly;
		}

		void OnDocumentTextReplaced(object sender, EventArgs e)
		{
			if (_syncing)
				return;

			if (HasNativeView)
			{
				_syncing = true;
				((XF.Editor)NativeView).Text = _document.Text;
				_syncing = false;
			}

			RaiseTextChanged();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (HasNativeView && e.Property.Bindable == IsReadOnlyProperty)
				((XF.Editor)NativeView).IsReadOnly = IsReadOnly;
		}
	}
}

namespace System.Windows.Documents
{
	public sealed class TextSelection : TextRange
	{
		internal TextSelection(TextPointer start, TextPointer end) : base(start, end)
		{
		}
	}
}

namespace Xamarin.Forms.Wpf
{
	/// <summary>Walks a GTK widget tree (GTK 4 keeps children as a linked list on every widget).</summary>
	internal static class GtkWidgets
	{
		internal static System.Collections.Generic.IEnumerable<T> Descendants<T>(Gtk.Widget root) where T : Gtk.Widget
		{
			if (root == null)
				yield break;

			if (root is T match)
				yield return match;

			for (var child = root.FirstChild; child != null; child = child.NextSibling)
			{
				foreach (var nested in Descendants<T>(child))
					yield return nested;
			}
		}
	}
}
