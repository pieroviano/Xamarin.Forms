using System;
using Xunit;

namespace Xamarin.Forms.Xaml.UnitTests
{
	/// <summary>
	/// Small helpers for NUnit assertions with no direct xUnit counterpart.
	/// </summary>
	/// <remarks>
	/// xUnit deliberately omits DoesNotThrow (its position is "just call the code"), but the
	/// NUnit suite used it 53 times to express intent. These wrap the idiomatic xUnit form -
	/// Record.Exception + Assert.Null - so the intent survives the migration and a throw
	/// still produces a clear failure rather than an unhandled exception.
	/// </remarks>
	public static class AssertEx
	{
		public static void DoesNotThrow(Action action)
		{
			var ex = Record.Exception(action);
			Assert.True(ex == null, $"expected no exception, but got {ex?.GetType().Name}: {ex?.Message}");
		}

		public static T DoesNotThrow<T>(Func<T> func)
		{
			T result = default;
			var ex = Record.Exception(() => result = func());
			Assert.True(ex == null, $"expected no exception, but got {ex?.GetType().Name}: {ex?.Message}");
			return result;
		}
	}
}
