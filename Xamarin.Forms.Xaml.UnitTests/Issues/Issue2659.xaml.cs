using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Issue2659 : ContentPage
	{
		public Issue2659()
		{
			InitializeComponent();
		}

		public Issue2659(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		void OnSetStyleButtonClicked(object sender, EventArgs args)
		{
			Style style = (Style)Resources["buttonStyle"];
			SetButtonStyle(style);
		}

		void OnUnsetStyleButtonClicked(object sender, EventArgs args)
		{
			SetButtonStyle(null);
		}

		void OnSetLocalButtonClicked(object sender, EventArgs args)
		{
			EnumerateButtons((Button button) =>
			{
				button.TextColor = Color.Red;
				button.FontAttributes = FontAttributes.Bold;
			});
		}

		void OnClearLocalButtonClicked(object sender, EventArgs args)
		{
			EnumerateButtons((Button button) =>
			{
				button.ClearValue(Button.TextColorProperty);
				button.ClearValue(Button.FontAttributesProperty);
			});
		}

		void SetButtonStyle(Style style)
		{
			EnumerateButtons(button =>
			{
				button.Style = style;
			});
		}

		void EnumerateButtons(Action<Button> action)
		{
			foreach (View view in stackLayout.Children)
				action((Button)view);
		}

		public class Tests
		{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			void AssertStyleApplied(Button button)
			{
				Assert.Equal(LayoutOptions.Center, button.HorizontalOptions);
				Assert.Equal(LayoutOptions.CenterAndExpand, button.VerticalOptions);
				Assert.Equal(16, button.FontSize);
				Assert.Equal(Color.Blue, button.TextColor);
				Assert.Equal(FontAttributes.Italic, button.FontAttributes);
			}

			void AssertStyleUnApplied(Button button)
			{
				Assert.Equal(View.HorizontalOptionsProperty.DefaultValue, button.HorizontalOptions);
				Assert.Equal(View.VerticalOptionsProperty.DefaultValue, button.VerticalOptions);
				Assert.Equal(10, button.FontSize);
				Assert.Equal(Button.TextColorProperty.DefaultValue, button.TextColor);
				Assert.Equal(Button.FontAttributesProperty.DefaultValue, button.FontAttributes);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void SetUnsetStyleFromResource(bool useCompiledXaml)
			{
				var layout = new Issue2659(useCompiledXaml);
				layout.EnumerateButtons(AssertStyleUnApplied);

				((IButtonController)layout.button0).SendClicked();
				layout.EnumerateButtons(AssertStyleApplied);

				((IButtonController)layout.button1).SendClicked();
				layout.EnumerateButtons(AssertStyleUnApplied);
			}

			void AssertPropertiesApplied(Button button)
			{
				Assert.Equal(Color.Red, button.TextColor);
				Assert.Equal(FontAttributes.Bold, button.FontAttributes);
			}

			void AssertPropertiesUnApplied(Button button)
			{
				Assert.Equal(Button.TextColorProperty.DefaultValue, button.TextColor);
				Assert.Equal(Button.FontAttributesProperty.DefaultValue, button.FontAttributes);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void SetUnsetLocalProperties(bool useCompiledXaml)
			{
				var layout = new Issue2659(useCompiledXaml);
				layout.EnumerateButtons(AssertPropertiesUnApplied);

				((IButtonController)layout.button2).SendClicked();
				layout.EnumerateButtons(AssertPropertiesApplied);

				((IButtonController)layout.button3).SendClicked();
				layout.EnumerateButtons(AssertPropertiesUnApplied);
			}
		}
	}
}