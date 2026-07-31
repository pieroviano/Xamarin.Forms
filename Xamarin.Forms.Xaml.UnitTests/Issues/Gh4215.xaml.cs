using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh4215VM
	{
		public static implicit operator DateTime(Gh4215VM value) => DateTime.UtcNow;
		public static implicit operator string(Gh4215VM value) => "foo";
		public static implicit operator long(Gh4215VM value) => long.MaxValue;
		public static implicit operator Rectangle(Gh4215VM value) => new Rectangle();
	}

	public partial class Gh4215 : ContentPage
	{
		public Gh4215()
		{
			InitializeComponent();
		}

		public Gh4215(bool useCompiledXaml)
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
			public void AvoidAmbiguousMatch(bool useCompiledXaml)
			{
				var layout = new Gh4215(useCompiledXaml);
				AssertEx.DoesNotThrow(() => layout.BindingContext = new Gh4215VM());
				Assert.Equal("foo", layout.l0.Text);
			}
		}
	}
}
