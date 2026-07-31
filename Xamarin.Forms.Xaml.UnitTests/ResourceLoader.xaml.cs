using System;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class ResourceLoader : ContentPage
	{
		public ResourceLoader()
		{
			InitializeComponent();
		}

		public ResourceLoader(bool useCompiledXaml)
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
#pragma warning disable CS0618 // Type or member is obsolete
				Xamarin.Forms.Internals.ResourceLoader.ResourceProvider = null;
#pragma warning restore CS0618 // Type or member is obsolete
			}

			[InlineData(false), TestCase(true)]
			public void XamlLoadingUsesResourceLoader(bool useCompiledXaml)
			{
				var layout = new ResourceLoader(useCompiledXaml);
				Assert.Equal(Color.FromHex("#368F95"), layout.label.TextColor);

#pragma warning disable CS0618 // Type or member is obsolete
				Xamarin.Forms.Internals.ResourceLoader.ResourceProvider = (asmName, path) =>
				{
#pragma warning restore CS0618 // Type or member is obsolete
					if (path == "ResourceLoader.xaml")
						return @"
<ContentPage 
	xmlns=""http://xamarin.com/schemas/2014/forms""
	xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
	x:Class=""Xamarin.Forms.Xaml.UnitTests.ResourceLoader"" >
  
	<Label x:Name = ""label"" TextColor = ""Pink"" />
</ContentPage >";
					return null;
				};
				layout = new ResourceLoader(useCompiledXaml);
				Assert.Equal(Color.Pink, layout.label.TextColor);
			}

			[Fact]
			public void XamlLoadingUsesResourceProvider2([Values(false, true)] bool useCompiledXaml)
			{
				var layout = new ResourceLoader(useCompiledXaml);
				Assert.Equal(Color.FromHex("#368F95"), layout.label.TextColor);
				object instance = null;
				Xamarin.Forms.Internals.ResourceLoader.ResourceProvider2 = (rlq) =>
				{
					if (rlq.ResourcePath == "ResourceLoader.xaml")
					{
						instance = rlq.Instance;
						return new Forms.Internals.ResourceLoader.ResourceLoadingResponse
						{
							ResourceContent = @"
<ContentPage 
	xmlns=""http://xamarin.com/schemas/2014/forms""
	xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
	x:Class=""Xamarin.Forms.Xaml.UnitTests.ResourceLoader""
	xmlns:d=""http://xamarin.com/schemas/2014/forms/design""
	xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
	mc:Ignorable=""d"" >
  
	<Label x:Name = ""label"" TextColor = ""Pink"" d:TextColor = ""HotPink"" />
</ContentPage >"
						};
					}
					return null;
				};


				layout = new ResourceLoader(useCompiledXaml);
				Assert.Equal(layout, instance);
				Assert.Equal(Color.Pink, layout.label.TextColor);
			}

			[Fact]
			public void XamlLoadingUsesResourceProvider2WithDesignProperties([Values(false, true)] bool useCompiledXaml)
			{
				var layout = new ResourceLoader(useCompiledXaml);
				Assert.Equal(Color.FromHex("#368F95"), layout.label.TextColor);

				Xamarin.Forms.Internals.ResourceLoader.ResourceProvider2 = (rlq) =>
				{
					if (rlq.ResourcePath == "ResourceLoader.xaml")
						return new Forms.Internals.ResourceLoader.ResourceLoadingResponse
						{
							UseDesignProperties = true,
							ResourceContent = @"
<ContentPage 
	xmlns=""http://xamarin.com/schemas/2014/forms""
	xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
	x:Class=""Xamarin.Forms.Xaml.UnitTests.ResourceLoader""
	xmlns:d=""http://xamarin.com/schemas/2014/forms/design""
	xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
	mc:Ignorable=""d"" >
  
	<Label x:Name = ""label"" TextColor = ""Pink"" d:TextColor = ""HotPink"" />
</ContentPage >"
						};
					return null;
				};


				layout = new ResourceLoader(useCompiledXaml);
				Assert.Equal(Color.HotPink, layout.label.TextColor);
			}

			[InlineData(false), TestCase(true)]
			public void RDLoadingUsesResourceLoader(bool useCompiledXaml)
			{
				var layout = new ResourceLoader(useCompiledXaml);
				Assert.Equal(Color.FromHex("#368F95"), layout.label.TextColor);

#pragma warning disable CS0618 // Type or member is obsolete
				Xamarin.Forms.Internals.ResourceLoader.ResourceProvider = (asmName, path) =>
				{
#pragma warning restore CS0618 // Type or member is obsolete
					if (path == "AppResources/Colors.xaml")
						return @"
<ResourceDictionary
	xmlns=""http://xamarin.com/schemas/2014/forms""
	xmlns:x = ""http://schemas.microsoft.com/winfx/2009/xaml"" >
	<Color x:Key = ""GreenColor"" >#36FF95</Color>
</ResourceDictionary >";
					return null;
				};
				layout = new ResourceLoader(useCompiledXaml);

				Assert.Equal(Color.FromHex("#36FF95"), layout.label.TextColor);
			}
		}
	}
}
