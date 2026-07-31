using Xunit;

using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{

	public class NameScopeTests : BaseTestFixture
	{
		[Fact]
		public void TopLevelObjectsHaveANameScope()
		{
			var xaml = @"
				<View 
				xmlns=""http://xamarin.com/schemas/2014/forms""
				xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" />";

			var view = new CustomView().LoadFromXaml(xaml);

			Assert.NotNull(Forms.Internals.NameScope.GetNameScope(view));
			Assert.IsType<Forms.Internals.NameScope>(Forms.Internals.NameScope.GetNameScope(view));
		}

		[Fact]
		public void NameScopeAreSharedWithChildren()
		{
			var xaml = @"
				<StackLayout 
				xmlns=""http://xamarin.com/schemas/2014/forms""
				xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" >
					<Label />
					<Label />
				</StackLayout>";

			var layout = new StackLayout().LoadFromXaml(xaml);

			Assert.NotNull(Forms.Internals.NameScope.GetNameScope(layout));
			Assert.IsType<Forms.Internals.NameScope>(Forms.Internals.NameScope.GetNameScope(layout));

			foreach (var child in layout.Children)
			{
				Assert.Null(Forms.Internals.NameScope.GetNameScope(child));
				Assert.Same(Forms.Internals.NameScope.GetNameScope(layout), child.GetNameScope());
			}
		}

		[Fact]
		public void DataTemplateChildrenDoesNotParticipateToParentNameScope()
		{
			var xaml = @"
				<ListView
				xmlns=""http://xamarin.com/schemas/2014/forms""
				xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
				x:Name=""listview"">
					<ListView.ItemTemplate>
						<DataTemplate>
						    <TextCell Text=""{Binding name}"" x:Name=""textcell""/>
						</DataTemplate>
					</ListView.ItemTemplate>
				</ListView>";

			var listview = new ListView();
			listview.LoadFromXaml(xaml);

			Assert.Same(listview, ((Forms.Internals.INameScope)listview).FindByName("listview"));
			Assert.Null(((Forms.Internals.INameScope)listview).FindByName("textcell"));
		}

		[Fact]
		public void ElementsCreatedFromDataTemplateHaveTheirOwnNameScope()
		{
			var xaml = @"
				<ListView
				xmlns=""http://xamarin.com/schemas/2014/forms""
				xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
				x:Name=""listview"">
					<ListView.ItemTemplate>
						<DataTemplate>
						    <TextCell Text=""{Binding name}"" x:Name=""textcell""/>
						</DataTemplate>
					</ListView.ItemTemplate>
				</ListView>";

			var listview = new ListView();
			listview.LoadFromXaml(xaml);
			Assert.NotNull(Forms.Internals.NameScope.GetNameScope(listview));
			Assert.IsType<Forms.Internals.NameScope>(Forms.Internals.NameScope.GetNameScope(listview));

			var cell0 = listview.ItemTemplate.CreateContent() as Element;
			var cell1 = listview.ItemTemplate.CreateContent() as Element;

			Assert.NotNull(Forms.Internals.NameScope.GetNameScope(cell0));
			Assert.IsType<Forms.Internals.NameScope>(Forms.Internals.NameScope.GetNameScope(cell0));
			Assert.NotNull(Forms.Internals.NameScope.GetNameScope(cell1));
			Assert.IsType<Forms.Internals.NameScope>(Forms.Internals.NameScope.GetNameScope(cell1));

			Assert.NotSame(Forms.Internals.NameScope.GetNameScope(listview), Forms.Internals.NameScope.GetNameScope(cell0));
			Assert.NotSame(Forms.Internals.NameScope.GetNameScope(listview), Forms.Internals.NameScope.GetNameScope(cell1));
			Assert.NotSame(Forms.Internals.NameScope.GetNameScope(cell0), Forms.Internals.NameScope.GetNameScope(cell1));

			Assert.Null(((Forms.Internals.INameScope)listview).FindByName("textcell"));
			Assert.NotNull(((Forms.Internals.INameScope)cell0).FindByName("textcell"));
			Assert.NotNull(((Forms.Internals.INameScope)cell1).FindByName("textcell"));

			Assert.NotSame(((Forms.Internals.INameScope)cell0).FindByName("textcell"), ((Forms.Internals.INameScope)cell1).FindByName("textcell"));

		}
	}
}
