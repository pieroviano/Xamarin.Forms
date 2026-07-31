using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class AttachedProperties : ContentPage
	{

		public AttachedProperties()
		{
			InitializeComponent();
		}

		public AttachedProperties(bool useCompiledXaml)
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

			[InlineData(true)]
			[InlineData(false)]
			public void BindProperties(bool useCompiledXaml)
			{
				var layout = new AttachedProperties(useCompiledXaml);
				var collection1 = StackLayoutProperties.GetStackLayoutCollection(layout.StackLayout1);
				var collection2 = StackLayoutProperties.GetStackLayoutCollection(layout.StackLayout2);
				Assert.Equal("a", collection1[0].ExampleProperty1);
				Assert.Equal("b", collection1[1].ExampleProperty1);
				Assert.Equal("c", collection2[0].ExampleProperty1);
				Assert.Equal("d", collection2[1].ExampleProperty1);
			}
		}
	}


	public class MyCustomClass
	{
		public string ExampleProperty1 { get; set; }
	}

	public class StackLayoutProperties
	{
		public static readonly BindableProperty StackLayoutCollectionProperty =
			BindableProperty.CreateAttached("StackLayoutCollection",
				typeof(IList<MyCustomClass>),
				typeof(StackLayoutProperties),
				null,
				defaultValueCreator: _ => new List<MyCustomClass>());

		public static IList<MyCustomClass> GetStackLayoutCollection(BindableObject view) => (IList<MyCustomClass>)view.GetValue(StackLayoutCollectionProperty);
		public static void SetStackLayoutCollection(BindableObject view, IList<MyCustomClass> value) => view.SetValue(StackLayoutCollectionProperty, value);
	}
}