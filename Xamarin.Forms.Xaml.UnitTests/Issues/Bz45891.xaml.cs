using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz45891 : ContentPage
	{
		public Bz45891()
		{
			InitializeComponent();
		}

		public Bz45891(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public static readonly BindableProperty ListProperty =
			BindableProperty.Create("List", typeof(IEnumerable<string>), typeof(Bz45891), default(IEnumerable<string>));

		public IEnumerable<string> List
		{
			get { return (IEnumerable<string>)GetValue(ListProperty); }
			set { SetValue(ListProperty, value); }
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
			public void LookForInheritanceOnOpImplicit(bool useCompiledXaml)
			{
				var p = new Bz45891(useCompiledXaml);
				Assert.Equal("Foo", p.List.First());
			}
		}
	}
}