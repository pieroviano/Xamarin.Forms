using System;
using Xunit;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Issue1637
	{
		[Fact]
		public void ImplicitCollectionWithSingleElement()
		{
			var xaml = @"
				<Grid xmlns=""http://xamarin.com/schemas/2014/forms"">
					<Grid.RowDefinitions>
						<RowDefinition Height=""*"" />
			        </Grid.RowDefinitions>
				</Grid>";
			var grid = new Grid();
			AssertEx.DoesNotThrow(() => grid.LoadFromXaml<Grid>(xaml));
			Assert.Single(grid.RowDefinitions);
			Assert.True(grid.RowDefinitions[0].Height.IsStar);
		}
	}
}

