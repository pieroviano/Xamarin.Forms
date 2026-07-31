using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Bz43733Rd : ResourceDictionary
	{
		public Bz43733Rd()
		{
			Add("SharedText", "Foo");
		}
	}

	public partial class Bz43733 : ContentPage
	{
		public Bz43733()
		{
			InitializeComponent();
		}

		public Bz43733(bool useCompiledXaml)
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
			public void ThrowOnMissingDictionary(bool useCompiledXaml)
			{
				Application.Current = new MockApplication
				{
					Resources = new ResourceDictionary
					{
#pragma warning disable 618
						MergedWith = typeof(Bz43733Rd),
#pragma warning restore 618
					}
				};
				var p = new Bz43733(useCompiledXaml);
				Assert.Equal("Foo", p.label.Text);
			}
		}
	}
}
