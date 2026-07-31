using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh4516VM
	{
		public Uri[] Images { get; } = { };
	}

	public partial class Gh4516 : ContentPage
	{
		public Gh4516() => InitializeComponent();
		public Gh4516(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(true), InlineData(false)]
			public void BindingToEmptyCollection(bool useCompiledXaml)
			{
				Gh4516 layout = null;
				AssertEx.DoesNotThrow(() => layout = new Gh4516(useCompiledXaml) { BindingContext = new Gh4516VM() });
				Assert.Equal("foo.jpg", (layout.image.Source as FileImageSource).File);
			}
		}
	}
}