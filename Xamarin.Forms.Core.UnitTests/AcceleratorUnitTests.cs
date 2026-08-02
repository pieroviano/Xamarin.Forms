using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms.Internals;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class AcceleratorUnitTests : BaseTestFixture
	{

		[Fact]
		public void AcceleratorThrowsOnEmptyString()
		{
			Assert.Throws<ArgumentNullException>(() => Accelerator.FromString(""));
		}

		[Fact]
		public void AcceleratorThrowsOnNull()
		{
			Assert.Throws<ArgumentNullException>(() => Accelerator.FromString(null));
		}

		[Fact]
		public void AcceleratorFromString()
		{
			string shourtCutKeyBinding = "ctrl+A";
			var accelerator = Accelerator.FromString(shourtCutKeyBinding);

			Assert.Equal(shourtCutKeyBinding, accelerator.ToString());
		}

		[Fact]
		public void AcceleratorFromOnlyLetter()
		{
			string shourtCutKeyBinding = "A";
			var accelerator = Accelerator.FromString(shourtCutKeyBinding);

			Assert.Equal(accelerator.Keys.Count(), 1);
			Assert.Equal(accelerator.Keys.ElementAt(0), shourtCutKeyBinding);
		}

		[Theory]
		[MemberData(nameof(GenerateTestsData))]
		public void AcceleratorFromLetterAndModifier(TestShortcut shourtcut)
		{
			string modifier = shourtcut.Modifier;
			string key = shourtcut.Key;
			var accelerator = Accelerator.FromString(shourtcut.ToString());

			Assert.Equal(accelerator.Keys.Count(), 1);
			Assert.Equal(accelerator.Modifiers.Count(), 1);
			Assert.Equal(accelerator.Keys.ElementAt(0), shourtcut.Key);
			Assert.Equal(accelerator.Modifiers.ElementAt(0), shourtcut.Modifier);
		}


		[Fact]
		public void AcceleratorFromLetterAnd2Modifier()
		{
			string modifier = "ctrl";
			string modifier1Alt = "alt";
			string key = "A";
			string shourtCutKeyBinding = $"{modifier}+{modifier1Alt}+{key}";
			var accelerator = Accelerator.FromString(shourtCutKeyBinding);

			Assert.Equal(accelerator.Keys.Count(), 1);
			Assert.Equal(accelerator.Modifiers.Count(), 2);
			Assert.Equal(accelerator.Keys.ElementAt(0), key);
			Assert.Equal(accelerator.Modifiers.ElementAt(0), modifier);
			Assert.Equal(accelerator.Modifiers.ElementAt(1), modifier1Alt);
		}


		[Preserve(AllMembers = true)]
		public struct TestShortcut
		{
			internal TestShortcut(string modifier)
			{
				Modifier = modifier;
				Key = modifier[0].ToString();
			}

			internal string Modifier { get; set; }
			internal string Key { get; set; }

			public override string ToString()
			{
				return $"{Modifier}+{Key}";
			}
		}
		public static IEnumerable<TestShortcut> GenerateTests
		{
			get { return new string[] { "ctrl", "cmd", "alt", "shift", "fn", "win" }.Select(str => new TestShortcut(str)); }
		}

		// xUnit's [MemberData] requires IEnumerable<object[]>, where NUnit's
		// [TestCaseSource] accepted a sequence of the argument type directly.
		public static IEnumerable<object[]> GenerateTestsData =>
			GenerateTests.Select(t => new object[] { t });
	}
}
