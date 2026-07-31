using Xunit;

using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class FindByName : ContentPage
	{
		public FindByName()
		{
			InitializeComponent();
		}

		public FindByName(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class FindByNameTests
		{
			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void TestRootName(bool useCompiledXaml)
			{
				var page = new FindByName(useCompiledXaml);
				Assert.Same(page, ((Forms.Internals.INameScope)page).FindByName("root"));
				Assert.Same(page, page.FindByName<FindByName>("root"));
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void TestName(bool useCompiledXaml)
			{
				var page = new FindByName(useCompiledXaml);
				Assert.Same(page.label0, page.FindByName<Label>("label0"));
			}
		}
	}
}