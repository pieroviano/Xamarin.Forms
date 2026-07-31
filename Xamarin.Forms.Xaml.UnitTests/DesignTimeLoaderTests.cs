using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class DesignTimeLoaderTests
	: IDisposable{
		public DesignTimeLoaderTests()
{
			Device.PlatformServices = new MockPlatformServices();
			Xamarin.Forms.Internals.Registrar.RegisterAll(new Type[0]);
		}

		public void Dispose()
{
			Device.PlatformServices = null;
			XamlLoader.FallbackTypeResolver = null;
			XamlLoader.ValueCreatedCallback = null;
			XamlLoader.InstantiationFailedCallback = null;
#pragma warning disable 0618
			Xamarin.Forms.Internals.ResourceLoader.ExceptionHandler = null;
			Xamarin.Forms.Xaml.Internals.XamlLoader.DoNotThrowOnExceptions = false;
#pragma warning restore 0618
		}

		[Fact]
		public void ContentPageWithMissingClass()
		{
			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
					x:Class=""Xamarin.Forms.Xaml.UnitTests.CustomView""
				/>";

			Assert.IsType<ContentPage>(XamlLoader.Create(xaml, true));
		}

		[Fact]
		public void ViewWithMissingClass()
		{
			var xaml = @"
				<ContentView xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
					x:Class=""Xamarin.Forms.Xaml.UnitTests.CustomView""
				/>";

			Assert.IsType<ContentView>(XamlLoader.Create(xaml, true));
		}

		[Fact]
		public void ContentPageWithMissingTypeMockviewReplacement()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(MockView);

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly""
					xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
					<ContentPage.Content>
						<local:MyCustomButton />
					</ContentPage.Content>
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);
			Assert.IsType<MockView>(page.Content);
		}

		[Fact]
		public void ContentPageWithMissingTypeNoReplacement()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type;

			var xaml = @"
					<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
						xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly""
						xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
						<ContentPage.Content>
							<local:MyCustomButton Foo=""Bar"">
								<local:MyCustomButton.Qux>
									<Label />
								</local:MyCustomButton.Qux>
							</local:MyCustomButton>
						</ContentPage.Content>
					</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);
			Assert.Null(page.Content);
		}

		[Fact]
		public void MissingTypeWithKnownProperty()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(Button);
			XamlLoader.ValueCreatedCallback = (x, v) =>
			{
				if (x.XmlTypeName == "MyCustomButton" && v is VisualElement ve)
				{
					ve._mergedStyle.ReRegisterImplicitStyles("MissingNamespace.MyCustomButton");
				}
			};

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly""
					xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
					<ContentPage.Content>
						<local:MyCustomButton BackgroundColor=""Red"" />
					</ContentPage.Content>
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);
			Assert.IsType<Button>(page.Content);
			Assert.Equal(new Color(1, 0, 0), page.Content.BackgroundColor);
		}

		[Fact]
		public void MissingTypeWithUnknownProperty()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(Button);
			XamlLoader.ValueCreatedCallback = (x, v) =>
			{
				if (x.XmlTypeName == "MyCustomButton" && v is VisualElement ve)
				{
					ve._mergedStyle.ReRegisterImplicitStyles("MissingNamespace.MyCustomButton");
				}
			};

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly""
					xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
					<ContentPage.Content>
						<local:MyCustomButton MyColor=""Red"" />
					</ContentPage.Content>
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);
			Assert.IsType<Button>(page.Content);
		}

		[Fact]
		public void ExplicitStyleAppliedToMissingType()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(Button);
			XamlLoader.ValueCreatedCallback = (x, v) =>
			{
				if (x.XmlTypeName == "MyCustomButton" && v is VisualElement ve)
				{
					ve._mergedStyle.ReRegisterImplicitStyles("MissingNamespace.MyCustomButton");
				}
			};

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly""
					xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"">
					<ContentPage.Resources>
						<Style x:Key=""LocalStyle"" TargetType=""local:MyCustomButton"">
							<Setter Property=""BackgroundColor"" Value=""Red"" />
						</Style>
					</ContentPage.Resources>
					<local:MyCustomButton Style=""{StaticResource LocalStyle}"" />
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);
			Assert.IsType<Button>(page.Content);
			Assert.Equal(Color.Red, page.Content.BackgroundColor);
		}

		[Fact]
		[Ignore(nameof(ImplicitStyleAppliedToMissingType))]
		public void ImplicitStyleAppliedToMissingType()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(Button);
			XamlLoader.ValueCreatedCallback = (x, v) =>
			{
				if (x.XmlTypeName == "MyCustomButton" && v is VisualElement ve)
				{
					ve._mergedStyle.ReRegisterImplicitStyles("MissingNamespace.MyCustomButton");
				}
			};

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly"">
					<ContentPage.Resources>
						<Style TargetType=""local:MyCustomButton"">
							<Setter Property=""BackgroundColor"" Value=""Red"" />
						</Style>
					</ContentPage.Resources>
					<local:MyCustomButton />
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);

			var myButton = (Button)page.Content;

			Assert.Equal(Color.Red, myButton.BackgroundColor);
		}

		[Fact]
		public void StyleTargetingRealTypeNotAppliedToMissingType()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(Button);
			XamlLoader.ValueCreatedCallback = (x, v) =>
			{
				if (x.XmlTypeName == "MyCustomButton" && v is VisualElement ve)
				{
					ve._mergedStyle.ReRegisterImplicitStyles("MissingNamespace.MyCustomButton");
				}
			};

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly"">
					<ContentPage.Resources>
						<Style TargetType=""Button"">
							<Setter Property=""BackgroundColor"" Value=""Red"" />
						</Style>
					</ContentPage.Resources>
					<local:MyCustomButton />
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);

			var myButton = (Button)page.Content;

			//Button Style shouldn't apply to MyCustomButton
			Assert.NotEqual(Color.Red, myButton.BackgroundColor);
		}

		[Fact]
		[Ignore(nameof(StyleTargetingMissingTypeNotAppliedToFallbackType))]
		public void StyleTargetingMissingTypeNotAppliedToFallbackType()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(Button);
			XamlLoader.ValueCreatedCallback = (x, v) =>
			{
				if (x.XmlTypeName == "MyCustomButton" && v is VisualElement ve)
				{
					ve._mergedStyle.ReRegisterImplicitStyles("MissingNamespace.MyCustomButton");
				}
			};

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly"">
					<ContentPage.Resources>
						<Style TargetType=""local:MyCustomButton"">
							<Setter Property=""BackgroundColor"" Value=""Red"" />
						</Style>
					</ContentPage.Resources>
					<Button />
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);

			var myButton = (Button)page.Content;

			//MyCustomButton Style should not be applied
			Assert.NotEqual(Color.Red, myButton.BackgroundColor);
		}

		[Fact]
		public void StyleAppliedToDerivedTypesAppliesToDerivedMissingType()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(Button);
			XamlLoader.ValueCreatedCallback = (x, v) =>
			{
				if (x.XmlTypeName == "MyCustomButton" && v is VisualElement ve)
				{
					ve._mergedStyle.ReRegisterImplicitStyles("MissingNamespace.MyCustomButton");
				}
			};

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly"">
					<ContentPage.Resources>
						<Style TargetType=""Button"" ApplyToDerivedTypes=""True"">
							<Setter Property=""BackgroundColor"" Value=""Red"" />
						</Style>
					</ContentPage.Resources>
					<local:MyCustomButton />
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);

			var myButton = (Button)page.Content;

			//Button Style should apply to MyCustomButton
			Assert.Equal(Color.Red, myButton.BackgroundColor);
		}

		[Fact]
		public void UnknownGenericType()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ??
				(p.Any(i => i.TypeName == "MyCustomButton`1") ? typeof(ProxyGenericButton<>) : typeof(MockView));

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly""
					xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"">
					<local:MyCustomButton x:TypeArguments=""local:MyCustomType"" />
				 </ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);
			Assert.IsType<ProxyGenericButton<MockView>>(page.Content);
		}

		[Fact]
		public void InvalidGenericType()
		{
			int exceptionCount = 0;
#pragma warning disable 0618 // Type or member is obsolete
			Forms.Internals.ResourceLoader.ExceptionHandler = _ => exceptionCount++;
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(MockView);
#pragma warning restore 0618 // Type or member is obsolete

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly""
					xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"">
					<local:MyCustomButton x:TypeArguments=""local:MyCustomType"" />
				 </ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);
			Assert.Null(page.Content);
			Assert.Equal(1, exceptionCount);
		}

		[Fact]
		public void UnknownMarkupExtensionOnMissingType()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(MockView);

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly"">
					<local:MyCustomButton Bar=""{local:Foo}"" />
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);
			Assert.IsType<MockView>(page.Content);
		}

		[Fact]
		public void UnknownMarkupExtensionKnownType()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(MockView);

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly"">
					<Button Text=""{local:Foo}"" />
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);
			Assert.IsType<Button>(page.Content);
		}

		[Fact]
		public void StaticResourceKeyInApp()
		{
			var app = @"
				<Application xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"">
					<Application.Resources>
						<ResourceDictionary>
							<Style TargetType=""Button"" x:Key=""StyleInApp"">
								<Setter Property=""BackgroundColor"" Value=""HotPink"" />
							</Style>
						</ResourceDictionary>
					</Application.Resources>
				</Application>
			";
			Application.Current = (Application)XamlLoader.Create(app, true);

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms"">
					<Button Style=""{StaticResource StyleInApp}"" />
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);
			Assert.IsType<Button>(page.Content);
			Assert.Equal(Color.HotPink, page.Content.BackgroundColor);
		}

		[Fact]
		public void StaticResourceKeyNotFound()
		{
			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms"">
					<Button Style=""{StaticResource MissingStyle}"" />
				</ContentPage>";

			var exceptions = new List<Exception>();
#pragma warning disable CS0618 // Type or member is obsolete
			Xamarin.Forms.Internals.ResourceLoader.ExceptionHandler = exceptions.Add;
#pragma warning restore CS0618 // Type or member is obsolete

			var page = (ContentPage)XamlLoader.Create(xaml, true);
			Assert.IsType<Button>(page.Content);
			Assert.Equal(2, exceptions.Count);
		}

		[Fact]
		public void CssStyleAppliedToMissingType()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(Button);
			XamlLoader.ValueCreatedCallback = (x, v) =>
			{
				if (x.XmlTypeName == "MyCustomButton" && v is Element e)
				{
					e._cssFallbackTypeName = "MyCustomButton";
				}
				if (x.XmlTypeName == "MyCustomButton" && v is VisualElement ve)
				{
					ve._mergedStyle.ReRegisterImplicitStyles("MissingNamespace.MyCustomButton");
				}
			};

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly"">
					<ContentPage.Resources>
						<StyleSheet>
							<![CDATA[
							MyCustomButton {
								background-color: blue;
							}
							]]>
						</StyleSheet>
					</ContentPage.Resources>
					<local:MyCustomButton />
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);

			var myButton = (Button)page.Content;

			Assert.Equal(Color.Blue, myButton.BackgroundColor);
		}

		[Fact]
		public void CssStyleTargetingRealTypeNotAppliedToMissingType()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(Button);
			XamlLoader.ValueCreatedCallback = (x, v) =>
			{
				if (x.XmlTypeName == "MyCustomButton" && v is Element e)
				{
					e._cssFallbackTypeName = "MyCustomButton";
				}
				if (x.XmlTypeName == "MyCustomButton" && v is VisualElement ve)
				{
					ve._mergedStyle.ReRegisterImplicitStyles("MissingNamespace.MyCustomButton");
				}
			};

			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
					xmlns:local=""clr-namespace:MissingNamespace;assembly=MissingAssembly"">
					<ContentPage.Resources>
						<StyleSheet>
							<![CDATA[
							button {
								background-color: red;
							}
							]]>
						</StyleSheet>
					</ContentPage.Resources>
					<StackLayout>
						<Button />
						<MyCustomButton />
					</StackLayout>
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);

			var button = ((StackLayout)page.Content).Children[0];
			var myButton = ((StackLayout)page.Content).Children[1];

			Assert.Equal(Color.Red, button.BackgroundColor);
			Assert.NotEqual(Color.Red, myButton.BackgroundColor);
		}

		[Fact]
		public void CssStyleTargetingMissingTypeNotAppliedToFallbackType()
		{
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(Button);
			XamlLoader.ValueCreatedCallback = (x, v) =>
			{
				if (x.XmlTypeName == "MyCustomButton" && v is VisualElement ve)
				{
					ve._mergedStyle.ReRegisterImplicitStyles("MissingNamespace.MyCustomButton");
				}
			};
			var xaml = @"
				<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms"">
					<ContentPage.Resources>
						<StyleSheet>
							<![CDATA[
							MyCustomButton {
								background-color: blue;
							}
							]]>
						</StyleSheet>
					</ContentPage.Resources>
					<Button />
				</ContentPage>";

			var page = (ContentPage)XamlLoader.Create(xaml, true);

			var myButton = (Button)page.Content;

			Assert.NotEqual(Color.Blue, myButton.BackgroundColor);
		}

		[Fact]
		public void CanProvideInstanceWhenInstantiationThrows()
		{
			var xaml = @"
					<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
						xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
						xmlns:local=""clr-namespace:Xamarin.Forms.Xaml.UnitTests;assembly=Xamarin.Forms.Xaml.UnitTests""
						xmlns:missing=""clr-namespace:MissingNamespace;assembly=MissingAssembly"">
						<StackLayout>
							<local:InstantiateThrows />
							<local:InstantiateThrows x:FactoryMethod=""CreateInstance"" />
							<local:InstantiateThrows>
								<x:Arguments>
									<x:Int32>1</x:Int32>
								</x:Arguments>
							</local:InstantiateThrows>
							<missing:Test>
								<x:Arguments>
									<x:Int32>1</x:Int32>
								</x:Arguments>
							</missing:Test>
						</StackLayout>
					</ContentPage>";
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(Button);
			XamlLoader.InstantiationFailedCallback = (xmltype, type, exception) => new Button();
			var o = XamlLoader.Create(xaml, true);
			Assert.DoesNotThrow(() => XamlLoader.Create(xaml, true));
		}

		[Fact]
		public void CanProvideInstanceWhenReplacedTypeConstructorInvalid()
		{
			var xaml = @"
						<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
							xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
							xmlns:local=""clr-namespace:Xamarin.Forms.Xaml.UnitTests;assembly=Xamarin.Forms.Xaml.UnitTests"">
							<StackLayout>
								<local:ReplacedType x:FactoryMethod=""CreateInstance"" />
							</StackLayout>
						</ContentPage>";
			XamlLoader.FallbackTypeResolver = (p, type) => type ?? typeof(Button);
			XamlLoader.InstantiationFailedCallback = (xmltype, type, exception) => new Button();
			Assert.DoesNotThrow(() => XamlLoader.Create(xaml, true));
		}

		[Fact]
		public void CanIgnoreSettingPropertyThatThrows()
		{
			var xaml = @"
					<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
						xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
						xmlns:local=""clr-namespace:Xamarin.Forms.Xaml.UnitTests;assembly=Xamarin.Forms.Xaml.UnitTests"">
						<local:SettingPropertyThrows TestValue=""Test"" TestBP=""bar""/>
					</ContentPage>";

			var exceptions = new List<Exception>();
#pragma warning disable CS0618 // Type or member is obsolete
			Xamarin.Forms.Internals.ResourceLoader.ExceptionHandler = exceptions.Add;
#pragma warning restore CS0618 // Type or member is obsolete
			Assert.DoesNotThrow(() => XamlLoader.Create(xaml, true));
			Assert.Equal(2, exceptions.Count);
		}

		[Fact]
		public void IgnoreConverterException()
		{
			var xaml = @"
					<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
						xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
						xmlns:local=""clr-namespace:Xamarin.Forms.Xaml.UnitTests;assembly=Xamarin.Forms.Xaml.UnitTests"">
						<Label BackgroundColor=""AlmostPink"" />
					</ContentPage>";

			var exceptions = new List<Exception>();
#pragma warning disable CS0618 // Type or member is obsolete
			Xamarin.Forms.Internals.ResourceLoader.ExceptionHandler = exceptions.Add;
#pragma warning restore CS0618 // Type or member is obsolete
			Assert.DoesNotThrow(() => XamlLoader.Create(xaml, true));
			Assert.Equal(1, exceptions.Count);
		}

		[Fact]
		public void IgnoreMarkupExtensionException()
		{
			var xaml = @"
						<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
							xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
								xmlns:local=""clr-namespace:Xamarin.Forms.Xaml.UnitTests;assembly=Xamarin.Forms.Xaml.UnitTests"">
								<StackLayout>
									<ListView ItemsSource=""{x:Static Foo}"" />
									<ListView ItemsSource=""{local:Missing Test}"" />
									<Label Text=""{Binding Foo"" />
								</StackLayout>
						</ContentPage>";
			var exceptions = new List<Exception>();
#pragma warning disable CS0618 // Type or member is obsolete
			Xamarin.Forms.Internals.ResourceLoader.ExceptionHandler = exceptions.Add;
#pragma warning restore CS0618 // Type or member is obsolete
			Assert.DoesNotThrow(() => XamlLoader.Create(xaml, true));
			Assert.That(exceptions.Count, Is.GreaterThan(1));
		}

		[Fact]
		public void CanResolveRootNode()
		{
			string assemblyName = null;
			string clrNamespace = null;
			string typeName = null;

			XamlLoader.FallbackTypeResolver = (fallbackTypeInfos, type) =>
			{
				assemblyName = fallbackTypeInfos?[1].AssemblyName;
				clrNamespace = fallbackTypeInfos?[1].ClrNamespace;
				typeName = fallbackTypeInfos?[1].TypeName;
				return type ?? typeof(MockView);
			};

			var xaml = @"
						<local:MissingType xmlns=""http://xamarin.com/schemas/2014/forms""
							xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
							xmlns:local=""clr-namespace:my.namespace;assembly=my.assembly"">
						</local:MissingType>";

			XamlLoader.Create(xaml, true);
			Assert.Equal("my.assembly", assemblyName);
			Assert.Equal("my.namespace", clrNamespace);
			Assert.Equal("MissingType", typeName);
		}

		[Fact]
		public void CanResolveRootNodeWithoutAssembly()
		{
			string assemblyName = null;
			string clrNamespace = null;
			string typeName = null;

			XamlLoader.FallbackTypeResolver = (fallbackTypeInfos, type) =>
			{
				assemblyName = fallbackTypeInfos?[1].AssemblyName;
				clrNamespace = fallbackTypeInfos?[1].ClrNamespace;
				typeName = fallbackTypeInfos?[1].TypeName;
				return type ?? typeof(MockView);
			};

			var xaml = @"
						<local:MissingType xmlns=""http://xamarin.com/schemas/2014/forms""
							xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
							xmlns:local=""using:my.namespace"">
						</local:MissingType>";

			XamlLoader.Create(xaml, true);
			Assert.Equal(null, assemblyName);
			Assert.Equal("my.namespace", clrNamespace);
			Assert.Equal("MissingType", typeName);
		}

		[Fact]
		public void IgnoreNamedMissingTypeException()
		{
			var xaml = @"
					<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
						xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
						xmlns:local=""clr-namespace:Xamarin.Forms.Xaml.UnitTests;assembly=Xamarin.Forms.Xaml.UnitTests"">
						<StackLayout>
							<local:Missing x:Name=""MyName"" />
							<Button x:Name=""button"" />
							<Button x:Name=""button"" />
						</StackLayout>
					</ContentPage>";
			var exceptions = new List<Exception>();
#pragma warning disable CS0618 // Type or member is obsolete
			Xamarin.Forms.Internals.ResourceLoader.ExceptionHandler = exceptions.Add;
#pragma warning restore CS0618 // Type or member is obsolete
			Assert.DoesNotThrow(() => XamlLoader.Create(xaml, true));
			Assert.That(exceptions.Count, Is.GreaterThan(1));
		}

		[Fact]
		public void IgnoreFindByNameInvalidCastException()
		{
			var xaml = @"
						<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
							xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml"">
							<Label x:Name=""MyName"" />
						</ContentPage>";

			var exceptions = new List<Exception>();
#pragma warning disable CS0618 // Type or member is obsolete
			Xamarin.Forms.Internals.ResourceLoader.ExceptionHandler = exceptions.Add;
#pragma warning restore CS0618 // Type or member is obsolete
			var content = (ContentPage)XamlLoader.Create(xaml, true);
			Assert.DoesNotThrow(() => content.FindByName<Button>("MyName"));
			Assert.That(exceptions.Count, Is.GreaterThanOrEqualTo(1));
		}

		[Fact]
		public void TextAsRandomContent()
		{
			var xaml = @"
						<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
							xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
								xmlns:local=""clr-namespace:Xamarin.Forms.Xaml.UnitTests;assembly=Xamarin.Forms.Xaml.UnitTests"">
								<StackLayout>
									<Label>xyz</Label>
									<StackLayout>foo</StackLayout>
									<Label><Label.FormattedText>bar</Label.FormattedText></Label>
								</StackLayout>
						</ContentPage>";
			XamlLoader.Create(xaml, true);
			var exceptions = new List<Exception>();
#pragma warning disable CS0618 // Type or member is obsolete
			Xamarin.Forms.Internals.ResourceLoader.ExceptionHandler = exceptions.Add;
#pragma warning restore CS0618 // Type or member is obsolete
			Assert.DoesNotThrow(() => XamlLoader.Create(xaml, true));
			Assert.That(exceptions.Count, Is.GreaterThanOrEqualTo(1));
		}

		[Fact]
		public void MissingGenericRootTypeProvidesCorrectTypeName()
		{
			var xaml = @"
					<local:GenericContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
						xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
						xmlns:local=""clr-namespace:MissingNamespace""
						x:TypeArguments=""x:Object"" />";

			XamlLoader.FallbackTypeResolver = (p, type) =>
			{
				if (type != null)
					return type;
				Assert.That(p.Select(i => i.TypeName), Has.Some.EqualTo("GenericContentPage`1"));
				return typeof(ProxyGenericContentPage<>);
			};

			Assert.DoesNotThrow(() => XamlLoader.Create(xaml, true));
		}
	}

	public class ProxyGenericContentPage<T> : ContentPage { }

	public class ProxyGenericButton<T> : Button { }

	public class InstantiateThrows
	{
		public InstantiateThrows() => throw new InvalidOperationException();
		public static InstantiateThrows CreateInstance() => throw new InvalidOperationException();
		public InstantiateThrows(int value) => throw new InvalidOperationException();
	}

	public class SettingPropertyThrows : View
	{
		public static readonly BindableProperty TestBPProperty =
			BindableProperty.Create("TestBP", typeof(string), typeof(SettingPropertyThrows), default(string),
				propertyChanged: (b, o, n) => throw new Exception());

		public string TestValue
		{
			get { return null; }
			set { throw new InvalidOperationException(); }
		}
	}
}
