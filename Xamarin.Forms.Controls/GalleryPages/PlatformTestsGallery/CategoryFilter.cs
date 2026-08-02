using Xamarin.Forms.Controls.Tests;
using Xamarin.Forms.Internals;
using Xunit.Abstractions;

namespace Xamarin.Forms.Controls.GalleryPages.PlatformTestsGallery
{
	[Preserve(AllMembers = true)]
	public class CategoryFilter : ITestCaseFilter
	{
		// NUnit's [Category] has no direct equivalent in xUnit; the convention is
		// [Trait("Category", "...")], which surfaces on the test case as a trait.
		public const string CategoryTrait = "Category";

		string _category;

		public CategoryFilter(string category) => _category = category;

		public bool Match(ITestCase testCase)
		{
			return testCase.Traits.TryGetValue(CategoryTrait, out var categories)
				&& categories.Contains(_category);
		}
	}
}
