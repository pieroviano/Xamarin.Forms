using System;
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

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(true)]
			[InlineData(false)]
			public void HtmlInCDATA(bool useCompiledXaml)
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