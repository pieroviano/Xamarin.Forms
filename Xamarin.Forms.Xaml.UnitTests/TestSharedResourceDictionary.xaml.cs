using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class TestSharedResourceDictionary : ContentPage
	{
		public TestSharedResourceDictionary()
		{
			InitializeComponent();
		}

		public TestSharedResourceDictionary(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
				Application.Current = new MockApplication
				{
					Resources = new ResourceDictionary
					{
#pragma warning disable 618
						MergedWith = typeof(MyRD)
#pragma warning restore 618
					}
				};
			}

			public void Dispose()
			{
				// Application.Current is process-global, and merely constructing the MockApplication
				// above published it (Application's ctor calls SetCurrentApplication). Leaving it set
				// leaks this fixture's MergedWith=MyRD - which defines "foo" and "bar" - into every
				// later test: LoaderTests.MissingStaticResourceShouldThrow asserts that resolving
				// "foo" throws, so it goes red or green purely on xUnit's ordering, which differs
				// between platforms. Same failure BaseTestFixture.Dispose records as MEASURED.
				Application.Current = null;
				Device.PlatformServices = null;
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void MergedResourcesAreFound(bool useCompiledXaml)
			{
				var layout = new TestSharedResourceDictionary(useCompiledXaml);
				Assert.Equal(Color.Pink, layout.label.TextColor);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void NoConflictsBetweenSharedRDs(bool useCompiledXaml)
			{
				var layout = new TestSharedResourceDictionary(useCompiledXaml);
				Assert.Equal(Color.Pink, layout.label.TextColor);
				Assert.Equal(Color.Purple, layout.label2.TextColor);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void ImplicitStyleCanBeSharedFromSharedRD(bool useCompiledXaml)
			{
				var layout = new TestSharedResourceDictionary(useCompiledXaml);
				Assert.Equal(Color.Red, layout.implicitLabel.TextColor);
			}

			class MyRD : ResourceDictionary
			{
				public MyRD()
				{
					Add("foo", "Foo");
					Add("bar", "Bar");
				}
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void MergedRDAtAppLevel(bool useCompiledXaml)
			{
				var layout = new TestSharedResourceDictionary(useCompiledXaml);
				Assert.Equal("Foo", layout.label3.Text);
			}

		}
	}
}