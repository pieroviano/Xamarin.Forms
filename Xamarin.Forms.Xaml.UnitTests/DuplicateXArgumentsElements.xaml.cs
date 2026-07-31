using System;
using Xunit;

namespace Xamarin.Forms.Xaml.UnitTests
{
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class DuplicateXArgumentsElements : BindableObject
	{
		public DuplicateXArgumentsElements(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public static class Tests
		{
			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public static void ThrowXamlParseException(bool useCompiledXaml)
			{
				Assert.Throws<XamlParseException>(useCompiledXaml ?
					(Action)(() => MockCompiler.Compile(typeof(DuplicateXArgumentsElements))) :
					() => new DuplicateXArgumentsElements(useCompiledXaml));
			}
		}
	}
}
