using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh4438VM : Gh4438VMBase<string>
	{
		public Gh4438VM()
		{
			Add("test");
			SelectedItem = this.First();
		}
	}

	public class Gh4438VMBase<T> : Collection<string>
	{
		public virtual T SelectedItem { get; set; }
	}

	public partial class Gh4438 : ContentPage
	{
		public Gh4438()
		{
			InitializeComponent();
		}

		public Gh4438(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			public void Dispose()
{
				Device.PlatformServices = null;
			}

			[Theory]
			[InlineData(true), InlineData(false)]
			public void GenericBaseClassResolution(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					AssertEx.DoesNotThrow(() => MockCompiler.Compile(typeof(Gh4438)));
				var layout = new Gh4438(useCompiledXaml) { BindingContext = new Gh4438VM() };
				Assert.Equal("test", layout.label.Text);
			}
		}
	}
}
