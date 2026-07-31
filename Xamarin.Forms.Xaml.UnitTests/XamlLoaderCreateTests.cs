using System;
using System.Windows.Input;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
#pragma warning disable 0618 //retaining legacy call to obsolete code

	public class XamlLoaderCreateTests
	: IDisposable{
		public void Dispose()
{
			Device.PlatformServices = null;
			XamlLoader.FallbackTypeResolver = null;
			XamlLoader.ValueCreatedCallback = null;
			XamlLoader.InstantiationFailedCallback = null;
			Xamarin.Forms.Internals.ResourceLoader.ExceptionHandler = null;
			Xamarin.Forms.Xaml.Internals.XamlLoader.DoNotThrowOnExceptions = false;
		}

		[Fact]
		public void CreateFromXaml()
		{
			var xaml = @"
				<ContentView xmlns=""http://xamarin.com/schemas/2014/forms""
							 xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
						 	 x:Class=""Xamarin.Forms.Xaml.UnitTests.FOO"">
					<Label Text=""Foo""  x:Name=""label""/>
				</ContentView>";

			var view = XamlLoader.Create(xaml);
			Assert.IsType<ContentView>(view);
			Assert.Equal("Foo", ((Label)((ContentView)view).Content).Text);
		}

		[Fact]
		public void CreateFromXamlDoesntFailOnMissingEventHandler()
		{
			var xaml = @"
				<Button xmlns=""http://xamarin.com/schemas/2014/forms""
						xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
						Clicked=""handleClick"">
				</Button>";

			Button button = null;
			Assert.DoesNotThrow(() => button = XamlLoader.Create(xaml, true) as Button);
			Assert.NotNull(button);
		}
	}
#pragma warning restore 0618
}