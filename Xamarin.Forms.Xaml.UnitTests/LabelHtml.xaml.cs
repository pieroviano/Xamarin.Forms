using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class LabelHtml : ContentPage
	{
		public LabelHtml() => InitializeComponent();
		public LabelHtml(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		{
			[SetUp] public void Setup() => Device.PlatformServices = new MockPlatformServices();
			[TearDown] public void TearDown() => Device.PlatformServices = null;

			[Fact]
			public void HtmlInCDATA([Values(true, false)] bool useCompiledXaml)
			{
				var html = "<h1>Hello World!</h1><br/>SecondLine";
				var layout = new LabelHtml(useCompiledXaml);
				Assert.Equal(html, layout.label0.Text);
				Assert.Equal(html, layout.label1.Text);
				Assert.Equal(html, layout.label2.Text);
				Assert.Equal(html, layout.label3.Text);
				Assert.Equal(html, layout.label4.Text);
			}
		}
	}
}