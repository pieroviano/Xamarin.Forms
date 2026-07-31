using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class SetStyleIdFromXName : ContentPage
	{
		public SetStyleIdFromXName() => InitializeComponent();
		public SetStyleIdFromXName(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(false), InlineData(true)]
			public void SetStyleId(bool useCompiledXaml)
			{
				var layout = new SetStyleIdFromXName(useCompiledXaml);
				Assert.Equal("label0", layout.label0.StyleId);
				Assert.Equal("foo", layout.label1.StyleId);
				Assert.Equal("bar", layout.label2.StyleId);
			}
		}
	}
}
