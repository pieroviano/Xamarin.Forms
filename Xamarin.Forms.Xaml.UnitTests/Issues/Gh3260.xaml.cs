using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public abstract class Gh3260MyGLayout<T> : Layout<T> where T : View
	{
		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			throw new NotImplementedException();
		}
	}

	public class Gh3260MyLayout : Gh3260MyGLayout<View>
	{
		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			throw new NotImplementedException();
		}
	}

	public partial class Gh3260 : ContentPage
	{
		public Gh3260()
		{
			InitializeComponent();
		}

		public Gh3260(bool useCompiledXaml)
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
			[InlineData(false), InlineData(true)]
			public void AssignContentWithNoContentAttributeDoesNotThrow(bool useCompiledXaml)
			{
				var layout = new Gh3260(useCompiledXaml);
				Assert.Single(layout.mylayout.Children);
				Assert.Equal(layout.label, layout.mylayout.Children[0]);
			}
		}
	}
}
