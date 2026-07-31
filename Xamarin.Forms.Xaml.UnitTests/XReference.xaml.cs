using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class XReference : ContentPage
	{
		public XReference()
		{
			InitializeComponent();
		}

		public XReference(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		{
			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void SupportsXReference(bool useCompiledXaml)
			{
				var layout = new XReference(useCompiledXaml);
				Assert.Same(layout.image, layout.imageView.Content);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void XReferenceAsCommandParameterToSelf(bool useCompiledXaml)
			{
				var layout = new XReference(useCompiledXaml);

				var commandParameterWasSelf = false;
				var button = layout.aButton;
				button.BindingContext = new
				{
					ButtonClickCommand = new Command(o =>
					{
						if (o == button)
							commandParameterWasSelf = true;
					})
				};
				((IButtonController)button).SendClicked();
				Assert.True(commandParameterWasSelf, "the command parameter was not the button itself");
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void XReferenceAsBindingSource(bool useCompiledXaml)
			{
				var layout = new XReference(useCompiledXaml);

				Assert.Equal("foo", layout.entry.Text);
				Assert.Equal("bar", layout.entry.Placeholder);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void CrossXReference(bool useCompiledXaml)
			{
				var layout = new XReference(useCompiledXaml);

				Assert.Same(layout.label0, layout.label1.BindingContext);
				Assert.Same(layout.label1, layout.label0.BindingContext);
			}
		}
	}
}