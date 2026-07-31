using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz53381 : ContentView
	{
		public Bz53381()
		{
			InitializeComponent();
		}

		public Bz53381(bool useCompiledXaml)
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
				Application.Current = null;
				Device.PlatformServices = null;
			}

			[InlineData(true)]
			[InlineData(false)]
			public void ControlTemplateAsImplicitAppLevelStyles(bool useCompiledXaml)
			{
				Application.Current = new Bz53381App();
				var view = new Bz53381(useCompiledXaml);
				Application.Current.MainPage = new ContentPage { Content = view };
				var presenter = ((StackLayout)view.InternalChildren[0]).Children[1] as ContentPresenter;
				Assume.That(presenter, Is.Not.Null);
				var grid = presenter.Content as Grid;
				Assert.NotNull(grid);
				Assert.Equal(Color.Green, grid.BackgroundColor);
			}
		}
	}
}