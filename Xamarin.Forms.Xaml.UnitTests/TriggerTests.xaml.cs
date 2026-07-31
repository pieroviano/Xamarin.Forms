using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class TriggerTests : ContentPage
	{
		public TriggerTests()
		{
			InitializeComponent();
		}

		public TriggerTests(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			public void Dispose()
{
				Device.PlatformServices = null;
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void ValueIsConverted(bool useCompiledXaml)
			{
				var layout = new TriggerTests(useCompiledXaml);
				Entry entry = layout.entry;
				Assert.NotNull(entry);

				var triggers = entry.Triggers;
				Assert.NotEmpty(triggers);
				var pwTrigger = triggers[0] as Trigger;
				Assert.Equal(Entry.IsPasswordProperty, pwTrigger.Property);
				Assert.True((bool)pwTrigger.Value);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void ValueIsConvertedWithPropertyCondition(bool useCompiledXaml)
			{
				var layout = new TriggerTests(useCompiledXaml);
				Entry entry = layout.entry1;
				Assert.NotNull(entry);

				var triggers = entry.Triggers;
				Assert.NotEmpty(triggers);
				var pwTrigger = triggers[0] as MultiTrigger;
				var pwCondition = pwTrigger.Conditions[0] as PropertyCondition;
				Assert.Equal(Entry.IsPasswordProperty, pwCondition.Property);
				Assert.True((bool)pwCondition.Value);
			}
		}
	}
}