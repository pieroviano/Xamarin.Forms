using System;
using Xamarin.Forms.Build.Tasks;
using Xunit;

namespace Xamarin.Forms.Xaml.UnitTests
{
	/// <summary>
	/// xUnit replacement for the former NUnit ExceptionTypeConstraint. See
	/// <see cref="XamlParseExceptionConstraint"/> for the rationale; used as
	/// <c>new BuildExceptionConstraint(7, 4).Verify(() =&gt; ...)</c>.
	/// </summary>
	public class BuildExceptionConstraint
	{
		readonly bool _hasLineInfo;
		readonly int _lineNumber;
		readonly int _linePosition;
		readonly Func<string, bool> _messagePredicate;

		BuildExceptionConstraint(bool hasLineInfo) => _hasLineInfo = hasLineInfo;

		public BuildExceptionConstraint() : this(false)
		{
		}

		public BuildExceptionConstraint(int lineNumber, int linePosition, Func<string, bool> messagePredicate = null)
			: this(true)
		{
			_lineNumber = lineNumber;
			_linePosition = linePosition;
			_messagePredicate = messagePredicate;
		}

		// BuildException is internal to Build.Tasks, so this cannot be the public return type.
		public void Verify(Action action)
		{
			var ex = Assert.Throws<BuildException>(action);

			if (!_hasLineInfo)
				return;

			var xmlInfo = ex.XmlInfo;
			Assert.True(xmlInfo != null && xmlInfo.HasLineInfo(), "expected the exception to carry XML line info");

			if (_messagePredicate != null)
				Assert.True(_messagePredicate(ex.Message), $"unexpected message: {ex.Message}");

			Assert.Equal(_lineNumber, xmlInfo.LineNumber);
			Assert.Equal(_linePosition, xmlInfo.LinePosition);
		}
	}
}
