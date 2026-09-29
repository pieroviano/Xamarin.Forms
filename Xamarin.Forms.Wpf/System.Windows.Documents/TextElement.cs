using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Xamarin.Forms.Wpf;
using XF = Xamarin.Forms;

namespace System.Windows.Documents
{
	/// <summary>The font and color properties text inherits, shared with the controls (WPF's own arrangement).</summary>
	public abstract class TextElement : FrameworkContentElement
	{
		public static readonly XF.BindableProperty FontFamilyProperty = Dp.Register<TextElement>(nameof(FontFamily), typeof(FontFamily), new FontFamily("Segoe UI"), inherits: true);
		public static readonly XF.BindableProperty FontSizeProperty = Dp.Register<TextElement>(nameof(FontSize), typeof(double), 12.0, inherits: true);
		public static readonly XF.BindableProperty FontStyleProperty = Dp.Register<TextElement>(nameof(FontStyle), typeof(FontStyle), FontStyles.Normal, inherits: true);
		public static readonly XF.BindableProperty FontWeightProperty = Dp.Register<TextElement>(nameof(FontWeight), typeof(FontWeight), FontWeights.Normal, inherits: true);
		public static readonly XF.BindableProperty FontStretchProperty = Dp.Register<TextElement>(nameof(FontStretch), typeof(FontStretch), FontStretches.Normal, inherits: true);
		public static readonly XF.BindableProperty ForegroundProperty = Dp.Register<TextElement>(nameof(Foreground), typeof(Brush), Brushes.Black, inherits: true);
		public static readonly XF.BindableProperty BackgroundProperty = Dp.Register<TextElement>(nameof(Background), typeof(Brush), null);

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

		public Brush Foreground
		{
			get => Get<Brush>(ForegroundProperty);
			set => SetValue(ForegroundProperty, value);
		}

		public Brush Background
		{
			get => Get<Brush>(BackgroundProperty);
			set => SetValue(BackgroundProperty, value);
		}

		public static FontFamily GetFontFamily(DependencyObject element) => (FontFamily)element.GetValue(FontFamilyProperty);

		public static void SetFontFamily(DependencyObject element, FontFamily value) => element.SetValue(FontFamilyProperty, value);

		public static double GetFontSize(DependencyObject element) => (double)element.GetValue(FontSizeProperty);

		public static void SetFontSize(DependencyObject element, double value) => element.SetValue(FontSizeProperty, value);

		public static FontStyle GetFontStyle(DependencyObject element) => (FontStyle)element.GetValue(FontStyleProperty);

		public static void SetFontStyle(DependencyObject element, FontStyle value) => element.SetValue(FontStyleProperty, value);

		public static FontWeight GetFontWeight(DependencyObject element) => (FontWeight)element.GetValue(FontWeightProperty);

		public static void SetFontWeight(DependencyObject element, FontWeight value) => element.SetValue(FontWeightProperty, value);

		public static Brush GetForeground(DependencyObject element) => (Brush)element.GetValue(ForegroundProperty);

		public static void SetForeground(DependencyObject element, Brush value) => element.SetValue(ForegroundProperty, value);

		/// <summary>The plain text of this element.</summary>
		internal abstract string PlainText { get; }
	}

	public abstract class Block : TextElement
	{
	}

	public abstract class Inline : TextElement
	{
	}

	[XF.ContentProperty(nameof(Text))]
	public class Run : Inline
	{
		public Run()
		{
		}

		public Run(string text) => Text = text;

		public static readonly XF.BindableProperty TextProperty = Dp.Register<Run>(nameof(Text), typeof(string), string.Empty);

		public string Text
		{
			get => Get<string>(TextProperty);
			set => SetValue(TextProperty, value ?? string.Empty);
		}

		internal override string PlainText => Text;
	}

	public class LineBreak : Inline
	{
		internal override string PlainText => "\n";
	}

	public class InlineCollection : Collection<Inline>
	{
		public void Add(string text) => Add(new Run(text));
	}

	[XF.ContentProperty(nameof(Inlines))]
	public class Span : Inline
	{
		public InlineCollection Inlines { get; } = new InlineCollection();

		internal override string PlainText => string.Concat(Inlines.Select(i => i.PlainText));
	}

	public class Bold : Span
	{
	}

	public class Italic : Span
	{
	}

	public class Underline : Span
	{
	}

	[XF.ContentProperty(nameof(Inlines))]
	public class Paragraph : Block
	{
		public Paragraph()
		{
		}

		public Paragraph(Inline inline) => Inlines.Add(inline);

		public InlineCollection Inlines { get; } = new InlineCollection();

		internal override string PlainText => string.Concat(Inlines.Select(i => i.PlainText));
	}

	public class BlockCollection : Collection<Block>
	{
	}

	/// <summary>A document of paragraphs, kept as its text: that is what a Xamarin.Forms editor can show.</summary>
	[XF.ContentProperty(nameof(Blocks))]
	public class FlowDocument : FrameworkContentElement
	{
		public FlowDocument()
		{
		}

		public FlowDocument(Block block) => Blocks.Add(block);

		public BlockCollection Blocks { get; } = new BlockCollection();

		public TextPointer ContentStart => new TextPointer(this, 0);

		public TextPointer ContentEnd => new TextPointer(this, Text.Length);

		/// <summary>Raised when the text is replaced through a <see cref="TextRange"/>, so the editor that shows it can follow.</summary>
		internal event EventHandler TextReplaced;

		/// <summary>The paragraphs, one per line.</summary>
		internal string Text
		{
			get => string.Join("\n", Blocks.Select(b => b.PlainText));
			set
			{
				Blocks.Clear();
				foreach (var line in (value ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
					Blocks.Add(new Paragraph(new Run(line)));
				TextReplaced?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public enum LogicalDirection
	{
		Backward,
		Forward,
	}

	public class TextPointer
	{
		internal TextPointer(FlowDocument document, int offset)
		{
			Document = document;
			Offset = offset;
		}

		internal FlowDocument Document { get; }

		internal int Offset { get; }

		public LogicalDirection LogicalDirection => LogicalDirection.Forward;

		public int CompareTo(TextPointer position) => Offset.CompareTo(position.Offset);

		public TextPointer GetPositionAtOffset(int offset) => new TextPointer(Document, Math.Max(0, Math.Min(Document.Text.Length, Offset + offset)));
	}

	/// <summary>A span of a document's text, which can be read and replaced as plain text or RTF.</summary>
	public class TextRange
	{
		public TextRange(TextPointer position1, TextPointer position2)
		{
			Start = position1 ?? throw new ArgumentNullException(nameof(position1));
			End = position2 ?? throw new ArgumentNullException(nameof(position2));
			if (Start.Document != End.Document)
				throw new ArgumentException("The positions belong to different documents.");
		}

		public TextPointer Start { get; }

		public TextPointer End { get; }

		public bool IsEmpty => Start.Offset >= End.Offset;

		public string Text
		{
			get
			{
				var text = Start.Document.Text;
				var from = Math.Min(Start.Offset, text.Length);
				var to = Math.Min(Math.Max(End.Offset, from), text.Length);
				return text.Substring(from, to - from);
			}
			set
			{
				var text = Start.Document.Text;
				var from = Math.Min(Start.Offset, text.Length);
				var to = Math.Min(Math.Max(End.Offset, from), text.Length);
				Start.Document.Text = text.Substring(0, from) + value + text.Substring(to);
			}
		}

		public bool CanLoad(string dataFormat) => dataFormat == DataFormats.Rtf || dataFormat == DataFormats.Text || dataFormat == DataFormats.UnicodeText;

		public bool CanSave(string dataFormat) => CanLoad(dataFormat);

		/// <summary>Replaces the range with the content of <paramref name="stream"/>; RTF keeps its text, not its formatting.</summary>
		public void Load(Stream stream, string dataFormat)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			using (var reader = new StreamReader(stream, Encoding.Default, true, 4096, leaveOpen: true))
			{
				var content = reader.ReadToEnd();
				Text = dataFormat == DataFormats.Rtf ? Rtf.ToText(content) : content;
			}
		}

		public void Save(Stream stream, string dataFormat)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			var bytes = dataFormat == DataFormats.Rtf ? Encoding.ASCII.GetBytes(Rtf.FromText(Text)) : Encoding.UTF8.GetBytes(Text);
			stream.Write(bytes, 0, bytes.Length);
		}
	}
}

namespace System.Windows
{
	public static class DataFormats
	{
		public static readonly string Text = "Text";
		public static readonly string UnicodeText = "UnicodeText";
		public static readonly string Rtf = "Rich Text Format";
		public static readonly string Html = "HTML Format";
		public static readonly string Xaml = "Xaml";
		public static readonly string XamlPackage = "XamlPackage";
		public static readonly string CommaSeparatedValue = "CSV";
		public static readonly string Bitmap = "Bitmap";
		public static readonly string FileDrop = "FileDrop";
		public static readonly string StringFormat = typeof(string).FullName;
	}
}

namespace Xamarin.Forms.Wpf
{
	using System;
	using System.Collections.Generic;

	/// <summary>RTF as far as text goes: the characters of a document, and a document from characters.</summary>
	internal static class Rtf
	{
		static readonly string[] s_skippedDestinations =
			{ "fonttbl", "colortbl", "stylesheet", "info", "pict", "object", "header", "footer", "listtable", "listoverridetable", "generator", "themedata", "datastore", "latentstyles", "rsidtbl", "xmlnstbl", "mmathPr" };

		internal static string ToText(string rtf)
		{
			if (string.IsNullOrEmpty(rtf) || !rtf.TrimStart().StartsWith("{\\rtf", StringComparison.Ordinal))
				return rtf ?? string.Empty;

			var result = new StringBuilder();
			var skipStack = new Stack<bool>();
			var skipping = false;
			var unicodeSkip = 1;
			var pendingSkip = 0;
			var encoding = Encoding.GetEncoding(28591);

			for (var i = 0; i < rtf.Length; i++)
			{
				var c = rtf[i];
				switch (c)
				{
					case '{':
						skipStack.Push(skipping);
						continue;
					case '}':
						skipping = skipStack.Count > 0 && skipStack.Pop();
						continue;
					case '\r':
					case '\n':
						continue;
					case '\\':
						break;
					default:
						if (pendingSkip > 0)
							pendingSkip--;
						else if (!skipping)
							result.Append(c);
						continue;
				}

				if (++i >= rtf.Length)
					break;

				c = rtf[i];
				if (c == '\\' || c == '{' || c == '}')
				{
					if (!skipping)
						result.Append(c);
					continue;
				}

				if (c == '*')
				{
					skipping = true;
					continue;
				}

				if (c == '\'')
				{
					if (i + 2 < rtf.Length && int.TryParse(rtf.Substring(i + 1, 2), System.Globalization.NumberStyles.HexNumber, null, out var code))
					{
						if (pendingSkip > 0)
							pendingSkip--;
						else if (!skipping)
							result.Append(encoding.GetString(new[] { (byte)code }));
					}

					i += 2;
					continue;
				}

				if (c == '~')
				{
					if (!skipping)
						result.Append(' ');
					continue;
				}

				if (!char.IsLetter(c))
					continue;

				var start = i;
				while (i < rtf.Length && char.IsLetter(rtf[i]))
					i++;
				var word = rtf.Substring(start, i - start);

				var numberStart = i;
				if (i < rtf.Length && (rtf[i] == '-' || char.IsDigit(rtf[i])))
				{
					i++;
					while (i < rtf.Length && char.IsDigit(rtf[i]))
						i++;
				}

				var hasNumber = int.TryParse(rtf.Substring(numberStart, i - numberStart), out var number);
				if (i < rtf.Length && rtf[i] != ' ')
					i--;

				if (s_skippedDestinations.Contains(word))
				{
					skipping = true;
					continue;
				}

				if (skipping)
					continue;

				switch (word)
				{
					case "par":
					case "line":
						result.Append('\n');
						break;
					case "tab":
						result.Append('\t');
						break;
					case "uc":
						unicodeSkip = hasNumber ? number : 1;
						break;
					case "u":
						if (hasNumber)
						{
							result.Append((char)(number < 0 ? number + 65536 : number));
							pendingSkip = unicodeSkip;
						}

						break;
					case "ansicpg":
						if (hasNumber)
						{
							try
							{
								encoding = Encoding.GetEncoding(number);
							}
							catch (ArgumentException)
							{
							}
							catch (NotSupportedException)
							{
							}
						}

						break;
				}
			}

			var text = result.ToString();
			return text.EndsWith("\n", StringComparison.Ordinal) ? text.Substring(0, text.Length - 1) : text;
		}

		internal static string FromText(string text)
		{
			var sb = new StringBuilder("{\\rtf1\\ansi\\deff0{\\fonttbl{\\f0 Segoe UI;}}\\f0 ");
			foreach (var c in text ?? string.Empty)
			{
				switch (c)
				{
					case '\\':
					case '{':
					case '}':
						sb.Append('\\').Append(c);
						break;
					case '\n':
						sb.Append("\\par ");
						break;
					case '\r':
						break;
					case '\t':
						sb.Append("\\tab ");
						break;
					default:
						if (c > 127)
							sb.Append("\\u").Append((int)c < 32768 ? (int)c : (int)c - 65536).Append('?');
						else
							sb.Append(c);
						break;
				}
			}

			return sb.Append('}').ToString();
		}
	}
}
