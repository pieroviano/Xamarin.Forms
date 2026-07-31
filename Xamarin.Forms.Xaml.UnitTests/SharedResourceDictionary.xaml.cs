using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class SharedResourceDictionary : ResourceDictionary
	{
		public SharedResourceDictionary()
		{
			InitializeComponent();
		}

		public SharedResourceDictionary(bool useCompiledXaml)
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
			[InlineData(false)]
			[InlineData(true)]
			public void ResourcesDirectoriesCanBeXamlRoots(bool useCompiledXaml)
			{
				var layout = new SharedResourceDictionary(useCompiledXaml);
				Assert.Equal(5, layout.Count);
			}
		}
	}
}