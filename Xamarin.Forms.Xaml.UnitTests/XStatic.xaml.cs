using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{

	public class Icons
	{
		public const string CLOSE = "ic_close.png";
	}

	public class MockxStatic
	{
		public static string MockStaticProperty { get { return "Property"; } }
		public const string MockConstant = "Constant";
		public static string MockField = "Field";
		public static string MockFieldRef = Icons.CLOSE;
		public string InstanceProperty { get { return "InstanceProperty"; } }
		public static readonly Color BackgroundColor = Color.Fuchsia;

		public class Nested
		{
			public static string Foo = "FOO";
		}
	}

	public enum MockEnum : long
	{
		First,
		Second,
		Third,
	}

	public partial class XStatic : ContentPage
	{
		public XStatic()
		{
			InitializeComponent();
		}
		public XStatic(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			//{x:Static Member=prefix:typeName.staticMemberName}
			//{x:Static prefix:typeName.staticMemberName}

			//The code entity that is referenced must be one of the following:
			// - A constant
			// - A static property
			// - A field
			// - An enumeration value
			// All other cases should throw

			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
			}

			public void Dispose()
{
				Device.PlatformServices = null;
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void StaticProperty(bool useCompiledXaml)
			{
				var layout = new XStatic(useCompiledXaml);
				Assert.Equal("Property", layout.staticproperty.Text);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void MemberOptional(bool useCompiledXaml)
			{
				var layout = new XStatic(useCompiledXaml);
				Assert.Equal("Property", layout.memberisoptional.Text);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void FieldColor(bool useCompiledXaml)
			{
				var layout = new XStatic(useCompiledXaml);
				Assert.Equal(Color.Fuchsia, layout.color.TextColor);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void Constant(bool useCompiledXaml)
			{
				var layout = new XStatic(useCompiledXaml);
				Assert.Equal("Constant", layout.constant.Text);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			//https://bugzilla.xamarin.com/show_bug.cgi?id=49228
			public void ConstantInARemoteAssembly(bool useCompiledXaml)
			{
				var layout = new XStatic(useCompiledXaml);
				Assert.Equal("XamarinFormsControls", layout.remoteConstant.Text);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void Field(bool useCompiledXaml)
			{
				var layout = new XStatic(useCompiledXaml);
				Assert.Equal("Field", layout.field.Text);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void Enum(bool useCompiledXaml)
			{
				var layout = new XStatic(useCompiledXaml);
				Assert.Equal(ScrollOrientation.Both, layout.enuM.Orientation);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void FieldRef(bool useCompiledXaml)
			{
				var layout = new XStatic(useCompiledXaml);
				Assert.Equal("ic_close.png", layout.field2.Text);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			// https://bugzilla.xamarin.com/show_bug.cgi?id=48242
			public void xStaticAndImplicitOperators(bool useCompiledXaml)
			{
				var layout = new XStatic(useCompiledXaml);
				Assert.Equal("ic_close.png", (layout.ToolbarItems[0].IconImageSource as FileImageSource).File);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			// https://bugzilla.xamarin.com/show_bug.cgi?id=55096
			public void xStaticAndNestedClasses(bool useCompiledXaml)
			{
				var layout = new XStatic(useCompiledXaml);
				Assert.Equal(MockxStatic.Nested.Foo, layout.nestedField.Text);
			}
		}
	}
}