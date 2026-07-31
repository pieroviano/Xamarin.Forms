using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class HRTests
	: IDisposable{
		public HRTests()
{
			Device.PlatformServices = new MockPlatformServices();
			Xamarin.Forms.Internals.Registrar.RegisterAll(new Type[0]);
			Application.Current = null;
		}

		public void Dispose()
{
			Device.PlatformServices = null;
			XamlLoader.FallbackTypeResolver = null;
			XamlLoader.ValueCreatedCallback = null;
			XamlLoader.InstantiationFailedCallback = null;
			Forms.Internals.ResourceLoader.ExceptionHandler2 = null;
#pragma warning disable 0618
			Internals.XamlLoader.DoNotThrowOnExceptions = false;
#pragma warning restore 0618
			Application.ClearCurrent();
		}

		[Fact]
		public void LoadResources()
		{
			var app = @"
				<Application xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"">
					<Application.Resources>
						<ResourceDictionary>
							<Color x:Key=""almostPink"">HotPink</Color>
						</ResourceDictionary>
					</Application.Resources>
				</Application>
			";
			Assert.Null(Application.Current);
			var mockApplication = new MockApplication();
			var rd = XamlLoader.LoadResources(app, mockApplication);
			Assert.IsType<ResourceDictionary>(rd);
			Assert.Single(((ResourceDictionary)rd));

			//check that the live app hasn't ben modified
			Assert.Equal(mockApplication, Application.Current);
			Assert.Empty(Application.Current.Resources);
		}

		[Fact]
		public void LoadMultipleResources()
		{
			var app = @"
				<Application xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"">
					<Application.Resources>
						<Color x:Key=""almostPink"">HotPink</Color>
						<Color x:Key=""yellowOrGreen"">Chartreuse</Color>
					</Application.Resources>
				</Application>
			";

			Assert.Null(Application.Current);
			var mockApplication = new MockApplication();
			var rd = XamlLoader.LoadResources(app, mockApplication);
			Assert.IsType<ResourceDictionary>(rd);
			Assert.Equal(2, ((ResourceDictionary)rd).Count);

			//check that the live app hasn't ben modified
			Assert.Equal(mockApplication, Application.Current);
			Assert.Empty(Application.Current.Resources);
		}

		[Fact]
		public void LoadSingleImplicitResources()
		{
			var app = @"
				<Application xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"">
					<Application.Resources>
						<Color x:Key=""almostPink"">HotPink</Color>
					</Application.Resources>
				</Application>
			";

			Assert.Null(Application.Current);
			var mockApplication = new MockApplication();
			var rd = XamlLoader.LoadResources(app, mockApplication);
			Assert.IsType<ResourceDictionary>(rd);
			Assert.Single(((ResourceDictionary)rd));

			//check that the live app hasn't ben modified
			Assert.Equal(mockApplication, Application.Current);
			Assert.Empty(Application.Current.Resources);
		}
	}
}
