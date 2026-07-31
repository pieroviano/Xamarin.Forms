using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz60203 : ContentPage
	{
		public Bz60203()
		{
			InitializeComponent();
		}

		public Bz60203(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		: IDisposable{

			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			public void Dispose()
{
				Device.PlatformServices = null;
			}

			[InlineData(true), TestCase(false)]
			public void CanCompileMultiTriggersWithDifferentConditions(bool useCompiledXaml)
			{
				var layout = new Bz60203(useCompiledXaml);
				Assert.Equal(BackgroundColorProperty.DefaultValue, layout.label.BackgroundColor);
				layout.BindingContext = new { Text = "Foo" };
				layout.label.TextColor = Color.Blue;
				Assert.Equal(Color.Pink, layout.label.BackgroundColor);
			}

		}
	}
}