using Xunit.Abstractions;

namespace Xamarin.Forms.Controls.Tests
{
	// Replaces NUnit's ITestFilter. xUnit has no filter abstraction of its own below the
	// runner-utility layer: discovery hands back a flat list of test cases and the runner
	// decides which of them to execute, so a filter is just a predicate over ITestCase.
	public interface ITestCaseFilter
	{
		bool Match(ITestCase testCase);
	}
}
