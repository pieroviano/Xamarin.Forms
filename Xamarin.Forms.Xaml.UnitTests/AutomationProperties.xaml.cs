using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class AutomationProperties : ContentPage
	{
		public AutomationProperties()
		{
			InitializeComponent();
		}

		public AutomationProperties(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
				Application.Current = new MockApplication();
			}

			public void Dispose()
{
				Device.PlatformServices = null;
				Application.Current = null;
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void AutomationPropertiesName(bool useCompiledXaml)
			{
				var layout = new AutomationProperties(useCompiledXaml);

				Assert.Equal("Name", (string)layout.entry.GetValue(Xamarin.Forms.AutomationProperties.NameProperty));
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void AutomationPropertiesHelpText(bool useCompiledXaml)
			{
				var layout = new AutomationProperties(useCompiledXaml);

				Assert.Equal("Sets your name", (string)layout.entry.GetValue(Xamarin.Forms.AutomationProperties.HelpTextProperty));
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void AutomationPropertiesIsInAccessibleTree(bool useCompiledXaml)
			{
				var layout = new AutomationProperties(useCompiledXaml);
				Application.Current.MainPage = layout;

				Assert.True((bool)layout.entry.GetValue(Xamarin.Forms.AutomationProperties.IsInAccessibleTreeProperty));
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void AutomationPropertiesLabeledBy(bool useCompiledXaml)
			{
				var layout = new AutomationProperties(useCompiledXaml);
				Application.Current.MainPage = layout;

				Assert.Equal(layout.label, (Element)layout.entry.GetValue(Xamarin.Forms.AutomationProperties.LabeledByProperty));
			}
		}
	}
}
