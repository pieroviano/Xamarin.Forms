using System;
using System.Collections.Generic;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class ItemsViewTests : BaseTestFixture
	{
		public ItemsViewTests()
		{
			var mockDeviceInfo = new TestDeviceInfo();
			Device.Info = mockDeviceInfo;
		}

		public override void Dispose()
		{
			base.Dispose();
			Device.Info = null;
		}

		[Fact]
		public void VerticalListMeasurement()
		{
			var itemsView = new StructuredItemsView();

			var sizeRequest = itemsView.Measure(double.PositiveInfinity, double.PositiveInfinity);

			Assert.Equal(Device.Info.ScaledScreenSize.Height, sizeRequest.Request.Height);
			Assert.Equal(Device.Info.ScaledScreenSize.Width, sizeRequest.Request.Width);
		}

		[Fact]
		public void HorizontalListMeasurement()
		{
			var itemsView = new StructuredItemsView();

			itemsView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal);

			var sizeRequest = itemsView.Measure(double.PositiveInfinity, double.PositiveInfinity);

			Assert.Equal(Device.Info.ScaledScreenSize.Height, sizeRequest.Request.Height);
			Assert.Equal(Device.Info.ScaledScreenSize.Width, sizeRequest.Request.Width);
		}

		[Fact]
		public void BindingContextPropagatesLayouts()
		{
			var bindingContext = new object();
			var itemsView = new StructuredItemsView();
			itemsView.BindingContext = bindingContext;
			var linearItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal);
			itemsView.ItemsLayout = linearItemsLayout;

			// BindingContext is set when ItemsLayout is set
			Assert.Equal(itemsView.BindingContext, linearItemsLayout.BindingContext);

			// BindingContext is updated when BindingContext on ItemsView is changed
			bindingContext = new object();
			itemsView.BindingContext = bindingContext;
			Assert.Equal(itemsView.BindingContext, linearItemsLayout.BindingContext);
		}
	}
}