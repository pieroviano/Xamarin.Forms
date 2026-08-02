using Xamarin.Forms.Controls.Tests;
using Xamarin.Forms.Internals;
using Xunit.Sdk;

namespace Xamarin.Forms.Controls.GalleryPages.PlatformTestsGallery
{
	[Preserve(AllMembers = true)]
	public class TestNameContainsFilter : ITestCaseFilter
	{
		string _substring;

		public TestNameContainsFilter(string substring) => _substring = substring;

		public bool Match(ITestCase testCase)
		{
			return testCase.TestCaseDisplayName.Contains(_substring);
		}
	}
}
