using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz42531 : ContentPage
	{
		public Bz42531()
		{
			InitializeComponent();
		}

		public Bz42531(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			public void Dispose()
{
				Device.PlatformServices = null;
			}

			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			public void RDInDataTemplates(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					MockCompiler.Compile(typeof(Bz42531));
				var p = new Bz42531(useCompiledXaml);
				ListView lv = p.lv;
				var template = lv.ItemTemplate;
				var cell = template.CreateContent(null, lv) as ViewCell;
				var sl = cell.View as StackLayout;
				Assert.Single(sl.Resources);
				var label = sl.Children[0] as Label;
				Assert.Equal(LayoutOptions.Center, label.HorizontalOptions);
			}
		}
	}
}