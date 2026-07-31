using System;
using Xunit;

using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class DataTemplateExtension : ContentPage
	{
		public DataTemplateExtension() => InitializeComponent();
		public DataTemplateExtension(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(true), InlineData(false)]
			public void DataTemplateExtension(bool useCompiledXaml)
			{
				var layout = new DataTemplateExtension(useCompiledXaml);
				var content = layout.Resources["content"] as ShellContent;
				var template = content.ContentTemplate;
				var obj = template.CreateContent();
				Assert.IsType<DataTemplateExtension>(obj);
			}
		}
	}
}