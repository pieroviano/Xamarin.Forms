// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	[ContentProperty(nameof(Text))]
	public class Gh9212MarkupExtension : IMarkupExtension
	{
		public string Text { get; set; }
		public object ProvideValue(IServiceProvider serviceProvider) => Text;
	}

	public partial class Gh9212 : ContentPage
	{
		public Gh9212() => InitializeComponent();
		public Gh9212(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void SingleQuoteAndTrailingSpaceInMarkupValue(bool useCompiledXaml)
			{
				var layout = new Gh9212(useCompiledXaml);
				Assert.Equal("Foo, Bar", layout.label.Text);
			}
		}
	}
}
