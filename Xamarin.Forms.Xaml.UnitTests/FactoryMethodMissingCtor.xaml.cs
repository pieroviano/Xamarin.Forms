using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class FactoryMethodMissingCtor : MockView
	{
		public FactoryMethodMissingCtor() => InitializeComponent();
		public FactoryMethodMissingCtor(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[SetUp] public void Setup() => Device.PlatformServices = new MockPlatformServices();
			[TearDown] public void TearDown() => Device.PlatformServices = null;

			[Fact]
			public void Throw([Values(false, true)] bool useCompiledXaml)
			{
				if (useCompiledXaml)
					Assert.Throws(new BuildExceptionConstraint(7, 4), () => MockCompiler.Compile(typeof(FactoryMethodMissingCtor)));
				else
					Assert.Throws<MissingMethodException>(() => new FactoryMethodMissingCtor(useCompiledXaml));
			}
		}
	}
}