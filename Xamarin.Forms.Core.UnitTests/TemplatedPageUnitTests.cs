using System.Collections.Generic;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class TemplatedPageUnitTests : BaseTestFixture
	{
		[Fact]
		public void TemplatedPage_should_have_the_InternalChildren_correctly_when_ControlTemplate_changed()
		{
			var sut = new TemplatedPage();
			IList<Element> internalChildren = ((IControlTemplated)sut).InternalChildren;
			internalChildren.Add(new VisualElement());
			internalChildren.Add(new VisualElement());
			internalChildren.Add(new VisualElement());

			sut.ControlTemplate = new ControlTemplate(typeof(ExpectedView));

			Assert.Equal(1, internalChildren.Count);
			Assert.IsAssignableFrom<ExpectedView>(internalChildren[0]);
		}

		private class ExpectedView : View
		{
			public ExpectedView()
			{
			}
		}
	}
}
