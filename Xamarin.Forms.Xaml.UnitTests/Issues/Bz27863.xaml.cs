using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz27863 : ContentPage
	{
		public Bz27863()
		{
			InitializeComponent();
		}

		public Bz27863(bool useCompiledXaml)
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

			[InlineData(true)]
			[InlineData(false)]
			public void DataTemplateInResourceDictionaries(bool useCompiledXaml)
			{
				var layout = new Bz27863(useCompiledXaml);
				var listview = layout.Resources["listview"] as ListView;
				Assert.NotNull(listview.ItemTemplate);
				var template = listview.ItemTemplate;
				var cell = template.CreateContent() as ViewCell;
				cell.BindingContext = "Foo";
				Assert.Equal("ooF", ((Label)((StackLayout)cell.View).Children[0]).Text);
			}
		}
	}
}