using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Issue2114 : Application
	{
		public Issue2114()
		{
			InitializeComponent();
		}

		public Issue2114(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();

				Current = null;
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void StaticResourceOnApplication(bool useCompiledXaml)
			{
				Issue2114 app;
				AssertEx.DoesNotThrow(() => app = new Issue2114(useCompiledXaml));

				Assert.True(Current.Resources.ContainsKey("ButtonStyle"));
				Assert.True(Current.Resources.ContainsKey("NavButtonBlueStyle"));
				Assert.True(Current.Resources.ContainsKey("NavButtonGrayStyle"));
			}
		}
	}
}