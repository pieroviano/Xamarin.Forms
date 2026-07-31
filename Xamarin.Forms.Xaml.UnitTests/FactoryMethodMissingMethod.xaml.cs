using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class FactoryMethodMissingMethod : MockView
	{
		public FactoryMethodMissingMethod()
		{
			InitializeComponent();
		}

		public FactoryMethodMissingMethod(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			[InlineData(false)]
			[InlineData(true)]
			public void Throw(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					Assert.Throws(new BuildExceptionConstraint(8, 4), () => MockCompiler.Compile(typeof(FactoryMethodMissingMethod)));
				else
					Assert.Throws<MissingMemberException>(() => new FactoryMethodMissingMethod(useCompiledXaml));
			}
		}
	}
}
