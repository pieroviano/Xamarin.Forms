using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Issue1497
	: IDisposable{
		public Issue1497()
{
			Device.PlatformServices = new MockPlatformServices();
		}

		public void Dispose()
{
			Device.PlatformServices = null;
		}

		[Fact]
		public void BPCollectionsWithSingleElement()
		{
			var xaml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
						<Grid
							xmlns=""http://xamarin.com/schemas/2014/forms"" 
							xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"">	 
							<Grid.ColumnDefinitions>
								<ColumnDefinition Width=""*""/>
							</Grid.ColumnDefinitions>
					    </Grid>";

			var grid = new Grid().LoadFromXaml(xaml);
			Assert.Single(grid.ColumnDefinitions);
			Assert.True(grid.ColumnDefinitions[0].Width.IsStar);
		}
	}
}