using System;
using System.Threading.Tasks;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	/// <summary>
	/// Small helpers for NUnit assertions with no direct xUnit counterpart.
	/// </summary>
	/// <remarks>
	/// xUnit deliberately omits DoesNotThrow (its position is "just call the code"), but the
	/// NUnit suite used it to express intent. These wrap the idiomatic xUnit form -
	/// Record.Exception + Assert.Null - so the intent survives the migration and a throw
	/// still produces a clear failure rather than an unhandled exception.
	///
	/// Mirrors Xamarin.Forms.Xaml.UnitTests/AssertEx.cs, which the earlier migration of that
	/// project introduced for the same reason.
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

		public static async Task DoesNotThrowAsync(Func<Task> func)
		{
			var ex = await Record.ExceptionAsync(func);
			Assert.True(ex == null, $"expected no exception, but got {ex?.GetType().Name}: {ex?.Message}");
		}
	}
}
