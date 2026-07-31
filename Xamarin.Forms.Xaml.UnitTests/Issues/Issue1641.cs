using System;
using Xunit;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Issue1641
	{
		[Fact]
		public void StaticResourceInTableView()
		{
			var xaml = @"
					<ContentPage
					xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"">
						<ContentPage.Resources>
					        <ResourceDictionary>
					          <x:String x:Key=""caption"" >Hello there!</x:String>
					        </ResourceDictionary>
						</ContentPage.Resources>

					    <TableView>                 
					        <TableRoot Title=""x"">
					            <TableSection Title=""y"">
					                <TextCell Text=""{StaticResource caption}"" />
					            </TableSection>
					        </TableRoot>
					    </TableView>
					</ContentPage>";
			var page = new ContentPage().LoadFromXaml(xaml);
			var table = page.Content as TableView;
			Assert.Equal("Hello there!", page.Resources["caption"] as string);
			Assert.Equal("Hello there!", (table.Root[0][0] as TextCell).Text);

		}
	}
}

