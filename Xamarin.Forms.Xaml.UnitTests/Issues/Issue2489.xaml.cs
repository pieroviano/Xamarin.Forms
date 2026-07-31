using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Issue2489 : ContentPage
	{
		public Issue2489()
		{
			InitializeComponent();
		}

		public Issue2489(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void DataTriggerTargetType(bool useCompiledXaml)
			{
				var layout = new Issue2489(useCompiledXaml);
				Assert.NotNull(layout.wimage);
				Assert.NotNull(layout.wimage.Triggers);
				Assert.True(layout.wimage.Triggers.Any());
				Assert.IsType<DataTrigger>(layout.wimage.Triggers[0]);
				var trigger = (DataTrigger)layout.wimage.Triggers[0];
				Assert.Equal(typeof(WImage), trigger.TargetType);
			}
		}
	}

	public class WImage : View
	{
		public ImageSource Source { get; set; }
		public Aspect Aspect { get; set; }
	}
}