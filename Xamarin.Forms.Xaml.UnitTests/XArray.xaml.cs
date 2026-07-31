using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	[ContentProperty("Content")]
	public class MockBindableForArray : View
	{
		public object Content { get; set; }
	}

	public partial class XArray : MockBindableForArray
	{
		public XArray()
		{
			InitializeComponent();
		}

		public XArray(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void SupportsXArray(bool useCompiledXaml)
			{
				var layout = new XArray(useCompiledXaml);
				var array = layout.Content;
				Assert.NotNull(array);
				Assert.IsType<string[]>(array);
				Assert.Equal(2, ((string[])layout.Content).Length);
				Assert.Equal("Hello", ((string[])layout.Content)[0]);
				Assert.Equal("World", ((string[])layout.Content)[1]);
			}
		}
	}
}