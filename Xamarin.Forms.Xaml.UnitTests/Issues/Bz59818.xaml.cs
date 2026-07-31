using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Bz59818 : ContentPage
	{
		public Bz59818()
		{
			InitializeComponent();
		}

		public Bz59818(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			IReadOnlyList<string> _flags;

			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
				_flags = Device.Flags;
				if (Device.Flags == null)
					Device.SetFlags(new List<string>().AsReadOnly());
			}

			public void Dispose()
{
				Device.PlatformServices = null;
				Device.SetFlags(_flags);
			}

			[Theory]
			[InlineData(true, "xamlDoubleImplicitOpHack")]
			[InlineData(false, "xamlDoubleImplicitOpHack")]
			[InlineData(true, null)]
			[InlineData(false, null)]
			public void Bz59818(bool useCompiledXaml, string flag)
			{
				Device.SetFlags(new List<string>(Device.Flags) {
					flag
				}.AsReadOnly());

				((MockPlatformServices)Device.PlatformServices).RuntimePlatform = Device.iOS;

				if (flag != "xamlDoubleImplicitOpHack")
				{
					if (useCompiledXaml)
						Assert.Throws<InvalidCastException>(() => new Bz59818(useCompiledXaml));
					else
						Assert.Throws<XamlParseException>(() => new Bz59818(useCompiledXaml));
					return;
				}
				var layout = new Bz59818(useCompiledXaml);
				Assert.Equal(new GridLength(100), layout.grid.ColumnDefinitions[0].Width);
			}
		}
	}
}
