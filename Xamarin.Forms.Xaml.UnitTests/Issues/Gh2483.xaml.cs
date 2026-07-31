using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh2483Rd : ResourceDictionary
	{
	}

	public class Gh2483Custom : ResourceDictionary
	{
		public Gh2483Custom()
		{
			Add("foo", Color.Orange);
		}
	}

	public partial class Gh2483 : ContentPage
	{
		public Gh2483()
		{
			InitializeComponent();
		}

		public Gh2483(bool useCompiledXaml)
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
			[InlineData(true), InlineData(false)]
			public void DupeKeyRd(bool useCompiledXaml)
			{
				var layout = new Gh2483(useCompiledXaml);
				return;
			}
		}
	}
}
