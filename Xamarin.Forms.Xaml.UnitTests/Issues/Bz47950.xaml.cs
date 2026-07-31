using System;
using System.Collections.Generic;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Bz47950Behavior : Behavior<View>
	{
		public static readonly BindableProperty ColorTestProperty =
			BindableProperty.CreateAttached("ColorTest", typeof(Color), typeof(View), default(Color));

		public static Color GetColorTest(BindableObject bindable) => (Color)bindable.GetValue(ColorTestProperty);
		public static void SetColorTest(BindableObject bindable, Color value) => bindable.SetValue(ColorTestProperty, value);
	}

	public partial class Bz47950 : ContentPage
	{
		public Bz47950()
		{
			InitializeComponent();
		}

		public Bz47950(bool useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests: IDisposable
		{
			public Tests() => Device.PlatformServices = new MockPlatformServices();
			public void Dispose() => Device.PlatformServices = null;

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void BehaviorAndStaticResource(bool useCompiledXaml)
			{
				var page = new Bz47950(useCompiledXaml);
			}
		}
	}
}
