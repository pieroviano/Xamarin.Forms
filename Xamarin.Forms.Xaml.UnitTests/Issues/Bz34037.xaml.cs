using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Bz34037Converter0 : IValueConverter, IMarkupExtension
	{
		public static int Invoked { get; set; }
		public static object Parameter { get; set; }
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			Invoked++;
			Parameter = parameter;
			return true;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public object ProvideValue(IServiceProvider serviceProvider)
		{
			return new Bz34037Converter0();
		}
	}

	public class Bz34037Converter1 : IValueConverter, IMarkupExtension
	{
		public static int Invoked { get; set; }
		public static object Parameter { get; set; }
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			Invoked++;
			Parameter = parameter;
			return true;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public object ProvideValue(IServiceProvider serviceProvider)
		{
			return new Bz34037Converter1();
		}
	}

	public partial class Bz34037 : ContentPage
	{
		public Bz34037()
		{
			InitializeComponent();
		}

		public string Property
		{
			get { return "FooBar"; }
		}

		public Bz34037(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		class Tests
		: IDisposable{
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
				Bz34037Converter0.Invoked = 0;
				Bz34037Converter1.Invoked = 0;
			}

			public void Dispose()
{
				Device.PlatformServices = null;
				Application.Current = null;
			}


			[InlineData(true)]
			[InlineData(false)]
			public void ConverterParameterOrderDoesNotMatters(bool useCompiledXaml)
			{
				var layout = new Bz34037(useCompiledXaml);
				Assert.Equal(1, Bz34037Converter0.Invoked);
				Assert.Equal(1, Bz34037Converter1.Invoked);
				Assert.Equal(typeof(string), Bz34037Converter0.Parameter);
				Assert.Equal(typeof(string), Bz34037Converter1.Parameter);
				Assert.True(layout.s0.IsToggled);
				Assert.True(layout.s1.IsToggled);
			}
		}
	}
}