using System;
using System.Collections.ObjectModel;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class FormattedStringTests : BaseTestFixture
	{
		public FormattedStringTests()
		{
			Device.PlatformServices = new MockPlatformServices();
		}

		public override void Dispose()
		{
			base.Dispose();
			Device.PlatformServices = null;
		}

		[Fact]
		public void NullSpansNotAllowed()
		{
			var fs = new FormattedString();
			Assert.ThrowsAny<ArgumentNullException>(() => fs.Spans.Add(null));

			fs = new FormattedString();
			fs.Spans.Add(new Span());

			Assert.ThrowsAny<ArgumentNullException>(() =>
			{
				fs.Spans[0] = null;
			});
		}

		[Fact]
		public void SpanChangeTriggersSpansPropertyChange()
		{
			var span = new Span();
			var fs = new FormattedString();
			fs.Spans.Add(span);

			bool spansChanged = false;
			fs.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == "Spans")
					spansChanged = true;
			};

			span.Text = "New text";

			Assert.True(spansChanged);
		}

		[Fact]
		public void SpanChangesUnsubscribes()
		{
			var span = new Span();
			var fs = new FormattedString();
			fs.Spans.Add(span);
			fs.Spans.Remove(span);

			bool spansChanged = false;
			fs.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == "Spans")
					spansChanged = true;
			};

			span.Text = "New text";

			Assert.False(spansChanged);
		}

		[Fact]
		public void AddingSpanTriggersSpansPropertyChange()
		{
			var span = new Span();
			var fs = new FormattedString();

			bool spansChanged = false;
			fs.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == "Spans")
					spansChanged = true;
			};

			fs.Spans.Add(span);

			Assert.True(spansChanged);
		}

		[Fact]
		public void ImplicitStringConversion()
		{
			string original = "fubar";
			FormattedString fs = original;
			Assert.NotNull(fs);
			Assert.Equal(1, fs.Spans.Count);
			Assert.NotNull(fs.Spans[0]);
			Assert.Equal(original, fs.Spans[0].Text);
		}

		[Fact]
		public void ImplicitStringConversionNull()
		{
			string original = null;
			FormattedString fs = original;
			Assert.NotNull(fs);
			Assert.Equal(1, fs.Spans.Count);
			Assert.NotNull(fs.Spans[0]);
			Assert.Equal(original, fs.Spans[0].Text);
		}
	}
}