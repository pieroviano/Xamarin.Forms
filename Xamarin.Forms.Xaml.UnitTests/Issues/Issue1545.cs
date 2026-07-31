using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Issue1545
	: IDisposable{
		public Issue1545()
{
			Device.PlatformServices = new MockPlatformServices { RuntimePlatform = Device.iOS };
		}

		public void Dispose()
{
			Device.PlatformServices = null;
		}

		[Fact]
		public void BindingCanNotBeReused()
		{
			string xaml = @"<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
						 xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
						 x:Class=""Xamarin.Forms.Controls.Issue1545"">
						<ListView x:Name=""List"" ItemsSource=""{Binding}"">
							<ListView.ItemTemplate>
								<DataTemplate>
									<TextCell Text=""{Binding}"" />
								</DataTemplate>
							</ListView.ItemTemplate>
						</ListView>
				</ContentPage>";

			ContentPage page = new ContentPage().LoadFromXaml(xaml);

			var items = new[] { "Fu", "Bar" };
			page.BindingContext = items;

			ListView lv = page.FindByName<ListView>("List");

			TextCell cell = (TextCell)lv.TemplatedItems.GetOrCreateContent(0, items[0]);
			Assert.Equal("Fu", cell.Text);

			cell = (TextCell)lv.TemplatedItems.GetOrCreateContent(1, items[1]);
			Assert.Equal("Bar", cell.Text);
		}

		[Fact]
		public void ElementsCanNotBeReused()
		{
			string xaml = @"<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
						 xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
						 x:Class=""Xamarin.Forms.Controls.Issue1545"">
							<ContentPage.Resources>
							<ResourceDictionary>
							<Color x:Key=""color"">#ff00aa</Color>
							</ResourceDictionary>
							</ContentPage.Resources>

						<ListView x:Name=""List"" ItemsSource=""{Binding}"">
							<ListView.ItemTemplate>
								<DataTemplate>
									<ViewCell>
									<StackLayout>
										<Label Text=""{Binding}"" BackgroundColor=""{StaticResource color}""/>
									</StackLayout>
									</ViewCell>
								</DataTemplate>
							</ListView.ItemTemplate>
						</ListView>
				</ContentPage>";

			ContentPage page = new ContentPage().LoadFromXaml(xaml);

			var items = new[] { "Fu", "Bar" };
			page.BindingContext = items;

			ListView lv = page.FindByName<ListView>("List");

			ViewCell cell0 = (ViewCell)lv.TemplatedItems.GetOrCreateContent(0, items[0]);


			Assert.Equal("Fu", ((Label)((StackLayout)cell0.View).Children[0]).Text);

			ViewCell cell1 = (ViewCell)lv.TemplatedItems.GetOrCreateContent(1, items[1]);
			Assert.Equal("Bar", ((Label)((StackLayout)cell1.View).Children[0]).Text);

			Assert.NotSame(cell0, cell1);
			Assert.NotSame(cell0.View, cell1.View);
			Assert.NotSame(((StackLayout)cell0.View).Children[0], ((StackLayout)cell1.View).Children[0]);
			Assert.Equal(Color.FromHex("ff00aa"), ((StackLayout)cell1.View).Children[0].BackgroundColor);
		}

		[Fact]
		public void ElementsFromCollectionsAreNotReused()
		{
			var xaml = @"<ListView 
						xmlns=""http://xamarin.com/schemas/2014/forms""
						xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
						xmlns:local=""clr-namespace:Xamarin.Forms.Xaml.UnitTests;assembly=Xamarin.Forms.Xaml.UnitTests""
						ItemsSource=""{Binding}"">
							<ListView.ItemTemplate>
								<DataTemplate>
									<local:ViewCellWithCollection>
										<local:ViewCellWithCollection.Children>
											<local:ViewList>
												<Label />
												<Label />
											</local:ViewList>
										</local:ViewCellWithCollection.Children>
									</local:ViewCellWithCollection>
								</DataTemplate>
							</ListView.ItemTemplate>
						</ListView>";

			var listview = new ListView();
			var items = new[] { "Foo", "Bar", "Baz" };
			listview.BindingContext = items;
			listview.LoadFromXaml(xaml);
			var cell0 = (ViewCellWithCollection)listview.TemplatedItems.GetOrCreateContent(0, items[0]);
			var cell1 = (ViewCellWithCollection)listview.TemplatedItems.GetOrCreateContent(1, items[1]);

			Assert.NotSame(cell0, cell1);
			Assert.NotSame(cell0.Children, cell1.Children);
			Assert.NotSame(cell0.Children[0], cell1.Children[0]);

		}

		[Fact]
		public void ResourcesDeclaredInDataTemplatesAreNotShared()
		{
			var xaml = @"<ListView 
						xmlns=""http://xamarin.com/schemas/2014/forms""
						xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
						xmlns:sys=""clr-namespace:System;assembly=mscorlib""
						ItemsSource=""{Binding}"">
							<ListView.ItemTemplate>
								<DataTemplate>
									<ViewCell>
										<Label Text=""{Binding}"">
											<Label.Resources>
												<ResourceDictionary>
													<sys:Object x:Key=""object""/>
												</ResourceDictionary>
											</Label.Resources>
										</Label>
									</ViewCell>
								</DataTemplate>
							</ListView.ItemTemplate>
						</ListView>";

			var listview = new ListView();
			var items = new[] { "Foo", "Bar", "Baz" };
			listview.BindingContext = items;

			listview.LoadFromXaml(xaml);
			var cell0 = (ViewCell)listview.TemplatedItems.GetOrCreateContent(0, items[0]);
			var cell1 = (ViewCell)listview.TemplatedItems.GetOrCreateContent(1, items[1]);
			Assert.NotSame(cell0, cell1);

			var label0 = (Label)cell0.View;
			var label1 = (Label)cell1.View;
			Assert.NotSame(label0, label1);
			Assert.Equal("Foo", label0.Text);
			Assert.Equal("Bar", label1.Text);

			var res0 = label0.Resources;
			var res1 = label1.Resources;
			Assert.NotSame(res0, res1);

			var obj0 = res0["object"];
			var obj1 = res1["object"];

			Assert.NotNull(obj0);
			Assert.NotNull(obj1);

			Assert.NotSame(obj0, obj1);
		}
	}

	public class ViewCellWithCollection : ViewCell
	{
		public ViewList Children { get; set; }
	}
}
