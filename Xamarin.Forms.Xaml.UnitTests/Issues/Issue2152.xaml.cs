using System;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Issue2152 : ContentPage
	{
		public Issue2152()
		{
			InitializeComponent();
		}

		public Issue2152(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		int clickcount;
		public void OnButtonClicked(object sender, EventArgs e)
		{
			clickcount++;
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
			public void TestEventConnection(bool useCompiledXaml)
			{
				Issue2152 layout = null;
				AssertEx.DoesNotThrow(() => layout = new Issue2152(useCompiledXaml));
				Cell cell = null;
				AssertEx.DoesNotThrow(() => cell = layout.listview.TemplatedItems.GetOrCreateContent(0, null));
				var button = cell.FindByName<Button>("btn") as IButtonController;
				Assert.Equal(0, layout.clickcount);
				button.SendClicked();
				Assert.Equal(1, layout.clickcount);
			}
		}
	}
}