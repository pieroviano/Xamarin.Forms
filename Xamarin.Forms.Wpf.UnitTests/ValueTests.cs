using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xamarin.Forms.Wpf;
using Xunit;

namespace Wpf.UnitTests
{
	/// <summary>Values as XAML writes them, system colors, resource URIs and RTF text.</summary>
	public class ValueTests : WpfTestBase
	{
		[Theory]
		[InlineData("1", 1, 1, 1, 1)]
		[InlineData("1,2", 1, 2, 1, 2)]
		[InlineData("8,84,0,0", 8, 84, 0, 0)]
		[InlineData("-6 -17 -6 -6", -6, -17, -6, -6)]
		public void ThicknessesParse(string text, double left, double top, double right, double bottom) =>
			Assert.Equal(new Thickness(left, top, right, bottom), new ThicknessConverter().ConvertFromInvariantString(text));

		[Fact]
		public void GridLengthsParse()
		{
			var converter = new GridLengthConverter();

			Assert.Equal(GridLength.Auto, converter.ConvertFromInvariantString("Auto"));
			Assert.Equal(new GridLength(1, GridUnitType.Star), converter.ConvertFromInvariantString("*"));
			Assert.Equal(new GridLength(2.5, GridUnitType.Star), converter.ConvertFromInvariantString("2.5*"));
			Assert.Equal(new GridLength(100), converter.ConvertFromInvariantString("100"));
		}

		[Theory]
		[InlineData("Red", 255, 255, 0, 0)]
		[InlineData("#FF0000", 255, 255, 0, 0)]
		[InlineData("#80FF0000", 128, 255, 0, 0)]
		[InlineData("#F00", 255, 255, 0, 0)]
		[InlineData("transparent", 0, 255, 255, 255)]
		public void ColorsParse(string text, byte a, byte r, byte g, byte b) =>
			Assert.Equal(Color.FromArgb(a, r, g, b), ColorConverter.Parse(text));

		[Fact]
		public void NamedBrushesAreFrozenAndShared()
		{
			Assert.Same(Brushes.Red, Brushes.Red);
			Assert.True(Brushes.Red.IsFrozen);
			Assert.Equal(Colors.Red, Brushes.Red.Color);
			Assert.Throws<InvalidOperationException>(() => Brushes.Red.Color = Colors.Blue);
		}

		[Fact]
		public void FontWeightsCursorsAndDashesParse()
		{
			Assert.Equal(FontWeights.Bold, new FontWeightConverter().ConvertFromInvariantString("Bold"));
			Assert.Equal(FontStyles.Italic, new FontStyleConverter().ConvertFromInvariantString("Italic"));
			Assert.Same(Cursors.Hand, new CursorConverter().ConvertFromInvariantString("Hand"));
			Assert.Equal("pointer", Cursors.Hand.GtkName);
			Assert.Equal(new[] { 4.0, 2.0 }, (DoubleCollection)new DoubleCollectionConverter().ConvertFromInvariantString("4 2"));
		}

		[Fact]
		public void SystemColorsAreSharedBrushesWithKeys()
		{
			Run(() =>
			{
				Assert.Same(SystemColors.ControlBrush, SystemColors.ControlBrush);
				Assert.Equal(Color.FromRgb(0xF0, 0xF0, 0xF0), SystemColors.ControlColor);
				Assert.Equal("SystemColors.ControlBrushKey", (string)SystemColors.ControlBrushKey);
				Assert.Same(SystemColors.WindowTextBrush, new FrameworkElement().TryFindResource(SystemColors.WindowTextBrushKey));
			});
		}

		[Theory]
		[InlineData("pack://application:,,,/Forms/Resources/a.png", "Forms/Resources/a.png")]
		[InlineData("pack://application:,,,/Showcase;component/Forms/a.png", "Forms/a.png")]
		[InlineData("Resources/a.png", "Resources/a.png")]
		[InlineData("Resources\\a.png", "Resources/a.png")]
		[InlineData("http://example.com/a.png", null)]
		public void ResourceUrisAreApplicationPaths(string uri, string path) => Assert.Equal(path, ResourceLocator.RelativePath(uri));

		[Fact]
		public void AnImageIsFoundNextToTheExecutable()
		{
			var image = new BitmapImage(new Uri("pack://application:,,,/Fixtures/Pixel.png"));

			Assert.Equal(2, image.PixelWidth);
			Assert.Equal(3, image.PixelHeight);
		}

		[Fact]
		public void AnImageIsFoundInsideTheAssembly()
		{
			var image = BitmapFrame.Create(new Uri("Fixtures/Embedded.png", UriKind.Relative));

			Assert.Equal(5, image.PixelWidth);
			Assert.Equal(4, image.PixelHeight);
		}

		[Fact]
		public void AMissingImageHasNoSize()
		{
			var image = new BitmapImage(new Uri("Fixtures/Missing.png", UriKind.Relative));

			Assert.Equal(0, image.PixelWidth);
			Assert.Null(image.ToForms());
		}

		[Fact]
		public void RtfLoadsAsItsText()
		{
			const string rtf = @"{\rtf1\ansi\ansicpg1252\deff0{\fonttbl{\f0 Arial;}}{\colortbl;\red255\green0\blue0;}\f0 Caff\'e8 \b bold\b0\par second\tab end \u8364?}";

			Assert.Equal("Caffè bold\nsecond\tend €", Rtf.ToText(rtf));
		}

		[Fact]
		public void TextSurvivesARoundTripThroughRtf()
		{
			const string text = "a {b} \\c\nnext è";

			Assert.Equal(text, Rtf.ToText(Rtf.FromText(text)));
		}

		[Fact]
		public void ATextRangeLoadsRtfIntoTheDocument()
		{
			Run(() =>
			{
				var box = new System.Windows.Controls.RichTextBox();
				var range = new TextRange(box.Document.ContentStart, box.Document.ContentEnd);

				using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(@"{\rtf1 one\par two}")))
					range.Load(stream, DataFormats.Rtf);

				Assert.Equal("one\ntwo", new TextRange(box.Document.ContentStart, box.Document.ContentEnd).Text);
				Assert.Equal(2, box.Document.Blocks.Count);
				Assert.Equal("one\ntwo", ((Xamarin.Forms.Editor)box.NativeView).Text);
			});
		}
	}
}
