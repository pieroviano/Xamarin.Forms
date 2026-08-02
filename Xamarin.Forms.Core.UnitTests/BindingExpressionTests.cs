using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class BindingExpressionTests : BaseTestFixture
	{
		[Fact]
		public void Ctor()
		{
			string path = "Foo.Bar";
			var binding = new Binding(path);

			var be = new BindingExpression(binding, path);

			Assert.Same(binding, be.Binding);
			Assert.Equal(path, be.Path);
		}

		[Fact]
		public void CtorInvalid()
		{
			string path = "Foo.Bar";
			var binding = new Binding(path);

			Assert.Throws<ArgumentNullException>(() => new BindingExpression(binding, null));

			Assert.Throws<ArgumentNullException>(() => new BindingExpression(null, path));
		}

		[Fact]
		public void ApplyNull()
		{
			const string path = "Foo.Bar";
			var binding = new Binding(path);
			var be = new BindingExpression(binding, path);
			AssertEx.DoesNotThrow(() => be.Apply(null, new MockBindable(), TextCell.TextProperty));
		}

		// We only throw on invalid path features, if they give an invalid property
		// name, it won't have compiled in the first place or they misstyped.
		[Theory]
		[InlineData("Foo.")]
		[InlineData("Foo[]")]
		[InlineData("Foo.Bar[]")]
		[InlineData("Foo[1")]
		public void InvalidPaths(string path)
		{
			var fex = Assert.Throws<FormatException>(() =>
			{
				var binding = new Binding(path);
				new BindingExpression(binding, path);
			});

			Assert.False(String.IsNullOrWhiteSpace(fex.Message), "FormatException did not contain an explanation");
		}

		[Theory]
		[InlineData(".", true, true)]
		[InlineData(".", true, false)]
		[InlineData(".", false, true)]
		[InlineData(".", false, false)]
		[InlineData("[1]", true, true)]
		[InlineData("[1]", true, false)]
		[InlineData("[1]", false, true)]
		[InlineData("[1]", false, false)]
		[InlineData(".[1]", true, true)]
		[InlineData(".[1]", true, false)]
		[InlineData(".[1]", false, true)]
		[InlineData(".[1]", false, false)]
		[InlineData("Foo", true, true)]
		[InlineData("Foo", true, false)]
		[InlineData("Foo", false, true)]
		[InlineData("Foo", false, false)]
		[InlineData("Foo.Bar", true, true)]
		[InlineData("Foo.Bar", true, false)]
		[InlineData("Foo.Bar", false, true)]
		[InlineData("Foo.Bar", false, false)]
		[InlineData("Foo.Bar[1]", true, true)]
		[InlineData("Foo.Bar[1]", true, false)]
		[InlineData("Foo.Bar[1]", false, true)]
		[InlineData("Foo.Bar[1]", false, false)]
		public void ValidPaths(string path, bool spaceBefore, bool spaceAfter)
		{
			if (spaceBefore)
				path = " " + path;
			if (spaceAfter)
				path = path + " ";

			var binding = new Binding(path);
			AssertEx.DoesNotThrow(() => new BindingExpression(binding, path));
		}

		public static IEnumerable<object[]> TryConvertWithNumbersAndCulturesCases => new[]
		{
			new object[]{ "4.2", new CultureInfo("en"), 4.2m },
			new object[]{ "4,2", new CultureInfo("de"), 4.2m },
			new object[]{ "-4.2", new CultureInfo("en"), -4.2m },
			new object[]{ "-4,2", new CultureInfo("de"), -4.2m },

			new object[]{ "4.2", new CultureInfo("en"), new decimal?(4.2m)},
			new object[]{ "4,2", new CultureInfo("de"), new decimal?(4.2m) },
			new object[]{ "-4.2", new CultureInfo("en"), new decimal?(-4.2m)},
			new object[]{ "-4,2", new CultureInfo("de"), new decimal?(-4.2m) },

			new object[]{ "4.2", new CultureInfo("en"), 4.2d },
			new object[]{ "4,2", new CultureInfo("de"), 4.2d },
			new object[]{ "-4.2", new CultureInfo("en"), -4.2d },
			new object[]{ "-4,2", new CultureInfo("de"), -4.2d },

			new object[]{ "4.2", new CultureInfo("en"), new double?(4.2d)},
			new object[]{ "4,2", new CultureInfo("de"), new double?(4.2d) },
			new object[]{ "-4.2", new CultureInfo("en"), new double?(-4.2d)},
			new object[]{ "-4,2", new CultureInfo("de"), new double?(-4.2d) },

			new object[]{ "4.2", new CultureInfo("en"), 4.2f },
			new object[]{ "4,2", new CultureInfo("de"), 4.2f },
			new object[]{ "-4.2", new CultureInfo("en"), -4.2f },
			new object[]{ "-4,2", new CultureInfo("de"), -4.2f },

			new object[]{ "4.2", new CultureInfo("en"), new float?(4.2f)},
			new object[]{ "4,2", new CultureInfo("de"), new float?(4.2f) },
			new object[]{ "-4.2", new CultureInfo("en"), new float?(-4.2f)},
			new object[]{ "-4,2", new CultureInfo("de"), new float?(-4.2f) },

			new object[]{ "4.", new CultureInfo("en"), "4." },
			new object[]{ "4,", new CultureInfo("de"), "4," },
			new object[]{ "-0", new CultureInfo("en"), "-0" },
			new object[]{ "-0", new CultureInfo("de"), "-0" },
		};

		[Theory]
		[MemberData(nameof(TryConvertWithNumbersAndCulturesCases))]
		public void TryConvertWithNumbersAndCultures(object inputString, CultureInfo culture, object expected)
		{
			CultureInfo.CurrentCulture = culture;
			BindingExpression.TryConvert(ref inputString, Entry.TextProperty, expected.GetType(), false);

			Assert.Equal(expected, inputString);
		}
	}
}
