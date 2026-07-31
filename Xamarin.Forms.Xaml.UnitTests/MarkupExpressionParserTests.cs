using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class MarkupExpressionParserTests : BaseTestFixture
	{
		IXamlTypeResolver typeResolver;

		public static readonly string Foo = "Foo";

		class MockElementNode : IElementNode, IValueNode, IXmlLineInfo
		{
			public bool HasLineInfo() { return false; }

			public int LineNumber
			{
				get { return -1; }
			}

			public int LinePosition
			{
				get { return -1; }
			}


			public IXmlNamespaceResolver NamespaceResolver
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			public object Value { get; set; }
			public Dictionary<XmlName, INode> Properties { get; set; }

			public List<XmlName> SkipProperties { get; set; }

			public NameScopeRef NameScopeRef => throw new NotImplementedException();

			public XmlType XmlType
			{
				get;
				set;
			}

			public string NamespaceURI
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			public INode Parent
			{
				get
				{
					throw new NotImplementedException();
				}
				set { throw new NotImplementedException(); }
			}

			public List<INode> CollectionItems { get; set; }

			public void Accept(IXamlNodeVisitor visitor, INode parentNode)
			{
				throw new NotImplementedException();
			}


			public List<string> IgnorablePrefixes { get; set; }

			public INode Clone()
			{
				throw new NotImplementedException();
			}
		}

		public MockElementNode()
{
			var nsManager = new XmlNamespaceManager(new NameTable());
			nsManager.AddNamespace("local", "clr-namespace:Xamarin.Forms.Xaml.UnitTests;assembly=Xamarin.Forms.Xaml.UnitTests");
			nsManager.AddNamespace("x", "http://schemas.microsoft.com/winfx/2009/xaml");
			typeResolver = new Internals.XamlTypeResolver(nsManager, XamlParser.GetElementType, Assembly.GetCallingAssembly());
		}

		[Fact]
		public void BindingOnSelf()
		{
			var bindingString = "{Binding}";
			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
			});
			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal(Binding.SelfPath, ((Binding)binding).Path);
		}

		[InlineData("{Binding Foo}")]
		[InlineData("{Binding {x:Static local:MarkupExpressionParserTests.Foo}}")]
		public void BindingWithImplicitPath(string bindingString)
		{
			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
			});

			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Foo", ((Binding)binding).Path);
		}

		[Fact]
		public void BindingWithPath()
		{
			var bindingString = "{Binding Path=Foo}";

			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
			});

			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Foo", ((Binding)binding).Path);
		}

		[Fact]
		public void BindingWithComposedPath()
		{
			var bindingString = "{Binding Path=Foo.Bar}";

			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
			});

			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Foo.Bar", ((Binding)binding).Path);
		}

		[Fact]
		public void BindingWithImplicitComposedPath()
		{
			var bindingString = "{Binding Path=Foo.Bar}";

			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
			});

			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Foo.Bar", ((Binding)binding).Path);
		}

		class MockValueProvider : IProvideParentValues, IProvideValueTarget
		{
			public MockValueProvider(string key, object resource)
			{
				var rd = new ResourceDictionary {
					{key, resource}
				};

				ve = new VisualElement
				{
					Resources = rd,
				};
			}


			VisualElement ve;
			public IEnumerable<object> ParentObjects
			{
				get
				{
					yield return ve;
				}
			}

			public object TargetObject => null;

			public object TargetProperty { get; set; } = null;
		}

		[Fact]
		public void BindingWithImplicitPathAndConverter()
		{
			var bindingString = "{Binding Foo, Converter={StaticResource Bar}}";
			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
				IProvideValueTarget = new MockValueProvider("Bar", new ReverseConverter()),
			});

			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Foo", ((Binding)binding).Path);
			Assert.NotNull(((Binding)binding).Converter);
			Assert.IsAssignableFrom<ReverseConverter>(((Binding)binding).Converter);
		}

		[Fact]
		public void BindingWithPathAndConverter()
		{
			var bindingString = "{Binding Path=Foo, Converter={StaticResource Bar}}";
			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
				IProvideValueTarget = new MockValueProvider("Bar", new ReverseConverter()),
			});

			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Foo", ((Binding)binding).Path);
			Assert.NotNull(((Binding)binding).Converter);
			Assert.IsAssignableFrom<ReverseConverter>(((Binding)binding).Converter);
		}


		[Fact]
		public void TestBindingMode()
		{
			var bindingString = "{Binding Foo, Mode=TwoWay}";

			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
			});

			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Foo", ((Binding)binding).Path);
			Assert.Equal(BindingMode.TwoWay, ((Binding)binding).Mode);
		}

		[Fact]
		public void BindingStringFormat()
		{
			var bindingString = "{Binding Foo, StringFormat=Bar}";

			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
			});
			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Foo", ((Binding)binding).Path);
			Assert.Equal("Bar", ((Binding)binding).StringFormat);
		}

		[Fact]
		public void BindingStringFormatWithEscapes()
		{
			var bindingString = "{Binding Foo, StringFormat='{}Hello {0}'}";

			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
			});

			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Foo", ((Binding)binding).Path);
			Assert.Equal("Hello {0}", ((Binding)binding).StringFormat);
		}

		[Fact]
		public void BindingStringFormatWithoutEscaping()
		{
			var bindingString = "{Binding Foo, StringFormat='{0,20}'}";

			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
			});

			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Foo", ((Binding)binding).Path);
			Assert.Equal("{0,20}", ((Binding)binding).StringFormat);
		}

		[Fact]
		public void BindingStringFormatNumeric()
		{
			var bindingString = "{Binding Foo, StringFormat=P2}";

			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
			});

			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Foo", ((Binding)binding).Path);
			Assert.Equal("P2", ((Binding)binding).StringFormat);
		}

		[Fact]
		public void BindingConverterParameter()
		{
			var bindingString = "{Binding Foo, ConverterParameter='Bar'}";

			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
			});

			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Foo", ((Binding)binding).Path);
			Assert.Equal("Bar", ((Binding)binding).ConverterParameter);
		}

		[Fact]
		public void BindingsCompleteString()
		{
			var bindingString = "{Binding Path=Foo.Bar, StringFormat='{}Qux, {0}', Converter={StaticResource Baz}, Mode=OneWayToSource}";
			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
				IProvideValueTarget = new MockValueProvider("Baz", new ReverseConverter()),
			});

			Assert.IsAssignableFrom<Binding>(binding);
			Assert.Equal("Foo.Bar", ((Binding)binding).Path);
			Assert.NotNull(((Binding)binding).Converter);
			Assert.IsAssignableFrom<ReverseConverter>(((Binding)binding).Converter);
			Assert.Equal(BindingMode.OneWayToSource, ((Binding)binding).Mode);
			Assert.Equal("Qux, {0}", ((Binding)binding).StringFormat);
		}

		[Fact]
		public void BindingWithStaticConverter()
		{
			var bindingString = "{Binding Converter={x:Static local:ReverseConverter.Instance}}";

			var binding = (new MarkupExtensionParser()).ParseExpression(ref bindingString, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
			}) as Binding;

			Assert.NotNull(binding);
			Assert.Equal(".", binding.Path);
			Assert.IsType<ReverseConverter>(binding.Converter);
		}

		public int FontSize { get; set; }

		[InlineData("{OnPlatform 20, Android=23}", Device.Android, 23)]
		[InlineData("{OnPlatform Android=20, iOS=25}", Device.iOS, 25)]
		[InlineData("{OnPlatform Android=20, GTK=25}", Device.GTK, 25)]
		[InlineData("{OnPlatform Android=20, macOS=25}", Device.macOS, 25)]
		[InlineData("{OnPlatform Android=20, Tizen=25}", Device.Tizen, 25)]
		[InlineData("{OnPlatform Android=20, UWP=25}", Device.UWP, 25)]
		[InlineData("{OnPlatform Android=20, WPF=25}", Device.WPF, 25)]
		[InlineData("{OnPlatform 20}", Device.iOS, 20)]
		[InlineData("{OnPlatform 20}", Device.GTK, 20)]
		[InlineData("{OnPlatform 20}", Device.macOS, 20)]
		[InlineData("{OnPlatform 20}", Device.Tizen, 20)]
		[InlineData("{OnPlatform 20}", Device.UWP, 20)]
		[InlineData("{OnPlatform 20}", Device.WPF, 20)]
		[InlineData("{OnPlatform 20}", "Foo", 20)]
		[InlineData("{OnPlatform Android=23, Default=20}", "Foo", 20)]
		public void OnPlatformExtension(string markup, string platform, int expected)
		{
			var services = new MockPlatformServices
			{
				RuntimePlatform = platform
			};
			Device.PlatformServices = services;

			var actual = (new MarkupExtensionParser()).ParseExpression(ref markup, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
				IProvideValueTarget = new MockValueProvider("foo", new object())
				{
					TargetProperty = GetType().GetProperty(nameof(FontSize))
				}
			});

			Assert.Equal(expected, actual);
		}

		[InlineData("{OnIdiom Phone=23, Tablet=25, Default=20}", TargetIdiom.Phone, 23)]
		[InlineData("{OnIdiom Phone=23, Tablet=25, Default=20}", TargetIdiom.Tablet, 25)]
		[InlineData("{OnIdiom 20, Phone=23, Tablet=25}", TargetIdiom.Desktop, 20)]
		[InlineData("{OnIdiom Phone=23, Tablet=25, Desktop=26, TV=30, Watch=10}", TargetIdiom.Desktop, 26)]
		[InlineData("{OnIdiom Phone=23, Tablet=25, Desktop=26, TV=30, Watch=10}", TargetIdiom.TV, 30)]
		[InlineData("{OnIdiom Phone=23, Tablet=25, Desktop=26, TV=30, Watch=10}", TargetIdiom.Watch, 10)]
		[InlineData("{OnIdiom Phone=23}", TargetIdiom.Desktop, default(int))]
		public void OnIdiomExtension(string markup, TargetIdiom idiom, int expected)
		{
			Device.SetIdiom(idiom);
			var actual = (new MarkupExtensionParser()).ParseExpression(ref markup, new Internals.XamlServiceProvider(null, null)
			{
				IXamlTypeResolver = typeResolver,
				IProvideValueTarget = new MockValueProvider("foo", new object())
				{
					TargetProperty = GetType().GetProperty(nameof(FontSize))
				}
			});

			Assert.Equal(expected, actual);
		}

		[InlineData("{Binding")]
		[InlineData("{Binding 'Foo}")]
		[InlineData("{Binding Foo, Converter={StaticResource Bar}")]
		[InlineData("{Binding Foo, Converter={StaticResource Bar}?}")]
		public void InvalidExpressions(string expression)
		{
			var serviceProvider = new Internals.XamlServiceProvider(null, null);
			serviceProvider.IXamlTypeResolver = typeResolver;
			serviceProvider.IProvideValueTarget = new MockValueProvider("Bar", new ReverseConverter());
			Assert.Throws<XamlParseException>(() => (new MarkupExtensionParser()).ParseExpression(ref expression, serviceProvider));
		}
	}
}
