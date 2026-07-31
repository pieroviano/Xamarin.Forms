using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Bz54334App : Application
	{
		bool daymode = true;
		public Bz54334App(bool useCompiledXaml)
		{
			Resources = new ResourceDictionary{
				new Style(typeof(Label)) {
					Setters = {
						new Setter {Property = Label.TextColorProperty, Value=Color.Blue}
					}
				}
			};
			MainPage = new Bz54334(useCompiledXaml);
			MessagingCenter.Subscribe<ContentPage>(this, "ChangeTheme", (s) =>
			{
				ToggleTheme();
			});
		}

		void ToggleTheme()
		{
			Resources = daymode ? new ResourceDictionary{
				new Style(typeof(Label)) {
					Setters = {
						new Setter {Property = Label.TextColorProperty, Value=Color.Red}
					}
				}
			} : new ResourceDictionary{
				new Style(typeof(Label)) {
					Setters = {
						new Setter {Property = Label.TextColorProperty, Value=Color.Blue}
					}
				}
			};
			daymode = !daymode;
		}
	}

	public partial class Bz54334 : ContentPage
	{
		public Bz54334()
		{
			InitializeComponent();
		}
		public Bz54334(bool useCompiledXaml)
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
			public void Foo(bool useCompiledXaml)
			{
				var app = Application.Current = new Bz54334App(useCompiledXaml);
				var page = app.MainPage as Bz54334;
				var l0 = page.label;
				var l1 = page.themedLabel;

				Assert.Equal(Color.Black, l0.TextColor);
				Assert.Equal(Color.Blue, l1.TextColor);

				MessagingCenter.Send<ContentPage>(page, "ChangeTheme");
				Assert.Equal(Color.Black, l0.TextColor);
				Assert.Equal(Color.Red, l1.TextColor);

				MessagingCenter.Send<ContentPage>(page, "ChangeTheme");
				Assert.Equal(Color.Black, l0.TextColor);
				Assert.Equal(Color.Blue, l1.TextColor);

			}
		}
	}
}