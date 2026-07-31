using Xunit;

using Xamarin.Forms;
namespace Xamarin.Forms.Xaml.UnitTests
{
	public class FakeView : View
	{
		public string NonBindable { get; set; }
	}

	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class SetterOnNonBP : ContentPage
	{
		public SetterOnNonBP()
		{
			InitializeComponent();
		}

		public SetterOnNonBP(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class SetterOnNonBPTests
		{
			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void ShouldThrow(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					new BuildExceptionConstraint(10, 13).Verify(() => MockCompiler.Compile(typeof(SetterOnNonBP)));
				else
					new XamlParseExceptionConstraint(10, 13).Verify(() => new SetterOnNonBP(useCompiledXaml));
			}
		}
	}
}