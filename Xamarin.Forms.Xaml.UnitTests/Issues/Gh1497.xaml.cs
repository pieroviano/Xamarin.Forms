using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	abstract class Gh1497BaseValidationBehavior<TBindable, TModel> : Behavior<TBindable> where TBindable : BindableObject
	{
	}

	sealed class Gh1497EntryValidationBehavior<TModel> : Gh1497BaseValidationBehavior<Entry, TModel>
	{
	}

	public partial class Gh1497 : ContentPage
	{
		public Gh1497()
		{
			InitializeComponent();
		}

		public Gh1497(bool useCompiledXaml)
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
			public void GenericsIssue(bool useCompiledXaml)
			{
				var layout = new Gh1497(useCompiledXaml);
				Assert.IsType<Gh1497EntryValidationBehavior<Entry>>(layout.entry.Behaviors[0]);
			}
		}
	}
}
