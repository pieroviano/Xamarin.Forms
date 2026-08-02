using System;
using System.Linq;
using System.Reflection;
using Xamarin.Forms.Internals;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class PreviewerReflectionTests
	{
		class FakePlatform : IPlatform
		{
			public SizeRequest GetNativeSize(VisualElement view, double widthConstraint, double heightConstraint)
			{
				throw new NotImplementedException();
			}
		}

		[Fact]
		public void PageHasPlatformProperty()
		{
			var page = new Page();

			var setPlatform = page.GetType().GetProperty("Platform", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(setPlatform);

			Action setValue = () => setPlatform.SetValue(page, new FakePlatform(), null);
			AssertEx.DoesNotThrow(setValue);
		}

		[Fact]
		public void RegisterAllExists()
		{
			var type = typeof(Registrar);

			var methods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
			var method = methods.Single(t =>
			{
				var parameters = t.GetParameters();

				return t.Name == "RegisterAll"
						&& parameters.Length == 1
						&& parameters[0].ParameterType == typeof(Type[]);
			});

			Assert.NotNull(method);
		}
	}
}