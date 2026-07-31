using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz28556 : ContentPage
	{
		public Bz28556()
		{
			InitializeComponent();
		}

		public Bz28556(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{
			[InlineData(true)]
			[InlineData(false)]
			public void SettersAppliedBeforeTriggers(bool useCompiledXaml)
			{
				var layout = new Bz28556(useCompiledXaml);

				Assert.Equal(Color.Yellow, layout.entry.TextColor);
				Assert.Equal(Color.Green, layout.entry.BackgroundColor);

				Assert.Equal(Color.Red, layout.disableEntry.TextColor);
				Assert.Equal(Color.Purple, layout.disableEntry.BackgroundColor);

				layout.entry.IsEnabled = false;
				layout.disableEntry.IsEnabled = true;

				Assert.Equal(Color.Yellow, layout.disableEntry.TextColor);
				Assert.Equal(Color.Green, layout.disableEntry.BackgroundColor);

				Assert.Equal(Color.Red, layout.entry.TextColor);
				Assert.Equal(Color.Purple, layout.entry.BackgroundColor);

				layout.entry.IsEnabled = true;
				layout.disableEntry.IsEnabled = false;

				Assert.Equal(Color.Yellow, layout.entry.TextColor);
				Assert.Equal(Color.Green, layout.entry.BackgroundColor);

				Assert.Equal(Color.Red, layout.disableEntry.TextColor);
				Assert.Equal(Color.Purple, layout.disableEntry.BackgroundColor);
			}
		}
	}
}

