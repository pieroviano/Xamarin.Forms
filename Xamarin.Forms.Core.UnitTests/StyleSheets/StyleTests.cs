using System;
using System.IO;


using Xamarin.Forms.Core.UnitTests;
using Xunit;

namespace Xamarin.Forms.StyleSheets.UnitTests
{
	public class StyleTests : IDisposable
	{
		public StyleTests()
		{
			Device.PlatformServices = new MockPlatformServices();
			Internals.Registrar.RegisterAll(new Type[0]);
		}

		public void Dispose()
		{
			Device.PlatformServices = null;
			Application.ClearCurrent();
		}

		[Fact]
		public void PropertiesAreApplied()
		{
			var styleString = @"background-color: #ff0000;";
			var style = Style.Parse(new CssReader(new StringReader(styleString)), '}');
			Assert.NotNull(style);

			var ve = new VisualElement();
			Assert.Equal(Color.Default, ve.BackgroundColor);
			style.Apply(ve);
			Assert.Equal(Color.Red, ve.BackgroundColor);
		}

		[Fact]
		public void PropertiesSetByStyleDoesNotOverrideManualOne()
		{
			var styleString = @"background-color: #ff0000;";
			var style = Style.Parse(new CssReader(new StringReader(styleString)), '}');
			Assert.NotNull(style);

			var ve = new VisualElement() { BackgroundColor = Color.Pink };
			Assert.Equal(Color.Pink, ve.BackgroundColor);

			style.Apply(ve);
			Assert.Equal(Color.Pink, ve.BackgroundColor);
		}

		[Fact]
		public void StylesAreCascading()
		{
			//color should cascade, background-color should not
			var styleString = @"background-color: #ff0000; color: #00ff00;";
			var style = Style.Parse(new CssReader(new StringReader(styleString)), '}');
			Assert.NotNull(style);

			var label = new Label();
			var layout = new StackLayout
			{
				Children = {
					label,
				}
			};

			Assert.Equal(Color.Default, layout.BackgroundColor);
			Assert.Equal(Color.Default, label.BackgroundColor);
			Assert.Equal(Color.Default, label.TextColor);

			style.Apply(layout);
			Assert.Equal(Color.Red, layout.BackgroundColor);
			Assert.Equal(Color.Default, label.BackgroundColor);
			Assert.Equal(Color.Lime, label.TextColor);
		}

		[Fact]
		public void PropertiesAreOnlySetOnMatchingElements()
		{
			var styleString = @"background-color: #ff0000; color: #00ff00;";
			var style = Style.Parse(new CssReader(new StringReader(styleString)), '}');
			Assert.NotNull(style);

			var layout = new StackLayout();
			Assert.Equal(Color.Default, layout.GetValue(TextElement.TextColorProperty));
		}

		[Fact]
		public void StyleSheetsOnAppAreApplied()
		{
			var app = new MockApplication();
			app.Resources.Add(StyleSheet.FromString("label{ color: red;}"));
			var page = new ContentPage
			{
				Content = new Label()
			};
			app.MainPage = page;
			Assert.Equal(Color.Red, (page.Content as Label).TextColor);
		}

		[Fact]
		public void StyleSheetsOnAppAreAppliedBeforePageStyleSheet()
		{
			var app = new MockApplication();
			app.Resources.Add(StyleSheet.FromString("label{ color: white; background-color: blue; }"));
			var page = new ContentPage
			{
				Content = new Label()
			};
			page.Resources.Add(StyleSheet.FromString("label{ color: red; }"));
			app.MainPage = page;
			Assert.Equal(Color.Red, (page.Content as Label).TextColor);
			Assert.Equal(Color.Blue, (page.Content as Label).BackgroundColor);
		}

		[Fact]
		public void StyleSheetsOnChildAreReAppliedWhenParentStyleSheetAdded()
		{
			var app = new MockApplication();
			var page = new ContentPage
			{
				Content = new Label()
			};
			page.Resources.Add(StyleSheet.FromString("label{ color: red; }"));
			app.MainPage = page;
			Assert.Equal(Color.Red, (page.Content as Label).TextColor);

			app.Resources.Add(StyleSheet.FromString("label{ color: white; background-color: blue; }"));
			Assert.Equal(Color.Blue, (page.Content as Label).BackgroundColor);
			Assert.Equal(Color.Red, (page.Content as Label).TextColor);
		}

		[Fact]
		public void StyleSheetsOnSubviewAreAppliedBeforePageStyleSheet()
		{
			var app = new MockApplication();
			app.Resources.Add(StyleSheet.FromString("label{ color: white; }"));
			var label = new Label();
			label.Resources.Add(StyleSheet.FromString("label{color: yellow;}"));

			var page = new ContentPage
			{
				Content = label
			};
			page.Resources.Add(StyleSheet.FromString("label{ color: red; }"));
			app.MainPage = page;
			Assert.Equal(Color.Yellow, (page.Content as Label).TextColor);
		}

	}
}