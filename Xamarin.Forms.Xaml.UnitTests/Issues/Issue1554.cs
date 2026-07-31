using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Issue1554
	: IDisposable{
		public Issue1554()
{
			Device.PlatformServices = new MockPlatformServices();
		}

		public void Dispose()
{
			Device.PlatformServices = null;
		}

		[Fact]
		public void CollectionItemsInDataTemplate()
		{
			var xaml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
				<ListView 
					xmlns=""http://xamarin.com/schemas/2014/forms"" 
					xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"" 
					ItemsSource=""{Binding}"">
			        <ListView.ItemTemplate>
			          <DataTemplate>
			            <ViewCell>
			              <ViewCell.View>
			                <StackLayout>
			                  <Label Text=""{Binding}""></Label>
			                  <Label Text=""{Binding}""></Label>
			                </StackLayout>
			              </ViewCell.View>
			            </ViewCell>
			          </DataTemplate>
			        </ListView.ItemTemplate>
			      </ListView>";
			var listview = new ListView();
			var items = new[] { "Foo", "Bar", "Baz" };
			listview.BindingContext = items;

			listview.LoadFromXaml(xaml);

			ViewCell cell0 = null;
			Assert.DoesNotThrow(() =>
			{
				cell0 = (ViewCell)listview.TemplatedItems.GetOrCreateContent(0, items[0]);
			});
			ViewCell cell1 = null;
			Assert.DoesNotThrow(() =>
			{
				cell1 = (ViewCell)listview.TemplatedItems.GetOrCreateContent(1, items[1]);
			});

			Assert.NotSame(cell0, cell1);
			Assert.NotSame(cell0.View, cell1.View);
			Assert.NotSame(((StackLayout)cell0.View).Children[0], ((StackLayout)cell1.View).Children[0]);
			Assert.NotSame(((StackLayout)cell0.View).Children[1], ((StackLayout)cell1.View).Children[1]);

		}
	}
}

