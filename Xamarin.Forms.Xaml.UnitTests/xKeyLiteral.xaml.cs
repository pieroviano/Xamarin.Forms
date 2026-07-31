// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Build.Tasks;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class xKeyLiteral : ContentPage
	{
		public xKeyLiteral() => InitializeComponent();
		public xKeyLiteral(bool useCompiledXaml)
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
			//this requirement might change, see https://github.com/xamarin/Xamarin.Forms/issues/12425
			public void xKeyRequireStringLiteral(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					Assert.Throws<BuildException>(() => MockCompiler.Compile(typeof(xKeyLiteral)));
				else
					Assert.Throws<XamlParseException>(() => new xKeyLiteral(useCompiledXaml));
			}
		}
	}
}
