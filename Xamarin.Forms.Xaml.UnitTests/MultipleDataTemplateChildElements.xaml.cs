using Xunit;

namespace Xamarin.Forms.Xaml.UnitTests
{
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class MultipleDataTemplateChildElements : BindableObject
	{
		public MultipleDataTemplateChildElements(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public static class Tests
		{
			[InlineData(false)]
			[InlineData(true)]
			public static void ThrowXamlParseException(bool useCompiledXaml)
			{
				Assert.Throws<XamlParseException>(useCompiledXaml ?
					(TestDelegate)(() => MockCompiler.Compile(typeof(MultipleDataTemplateChildElements))) :
					() => new MultipleDataTemplateChildElements(useCompiledXaml));
			}
		}
	}
}
