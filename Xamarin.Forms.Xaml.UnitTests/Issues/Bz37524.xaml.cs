using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz37524 : ContentPage
	{
		public Bz37524()
		{
			InitializeComponent();
		}

		public Bz37524(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			public void MultiTriggerConditionNotApplied(bool useCompiledXaml)
			{
				var layout = new Bz37524(useCompiledXaml);
				Assert.False(layout.TheButton.IsEnabled);
			}
		}
	}
}