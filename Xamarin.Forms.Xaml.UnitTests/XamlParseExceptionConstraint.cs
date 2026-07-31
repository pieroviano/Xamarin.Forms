using System;
using Xunit;

namespace Xamarin.Forms.Xaml.UnitTests
{
	/// <summary>
	/// xUnit replacement for the former NUnit ExceptionTypeConstraint.
	/// </summary>
	/// <remarks>
	/// NUnit allowed a custom constraint to be passed to Assert.Throws; xUnit has no
	/// constraint model, so the same checks are kept here and invoked explicitly:
	///
	///     new XamlParseExceptionConstraint(8, 9).Verify(() =&gt; ...);
	///
	/// Keeping the class - rather than collapsing each call site to a bare
	/// Assert.Throws&lt;XamlParseException&gt; - preserves the line/position/message
	/// assertions. Dropping them would have quietly weakened 22 tests.
	/// </remarks>
	public class XamlParseExceptionConstraint
	{
		readonly bool _hasLineInfo;
		readonly int _lineNumber;
		readonly int _linePosition;
		readonly Func<string, bool> _messagePredicate;

		XamlParseExceptionConstraint(bool hasLineInfo)
		{
			_hasLineInfo = hasLineInfo;
		}

		public XamlParseExceptionConstraint() : this(false)
		{
		}

		public XamlParseExceptionConstraint(int lineNumber, int linePosition, Func<string, bool> messagePredicate = null)
			: this(true)
		{
			_lineNumber = lineNumber;
			_linePosition = linePosition;
			_messagePredicate = messagePredicate;
		}

		public XamlParseException Verify(Action action)
		{
			var ex = Assert.Throws<XamlParseException>(action);

			if (!_hasLineInfo)
				return ex;

			var xmlInfo = ex.XmlInfo;
			Assert.True(xmlInfo != null && xmlInfo.HasLineInfo(), "expected the exception to carry XML line info");

			if (_messagePredicate != null)
				Assert.True(_messagePredicate(ex.UnformattedMessage), $"unexpected message: {ex.UnformattedMessage}");

			Assert.Equal(_lineNumber, xmlInfo.LineNumber);
			Assert.Equal(_linePosition, xmlInfo.LinePosition);

			return ex;
		}
	}
}
