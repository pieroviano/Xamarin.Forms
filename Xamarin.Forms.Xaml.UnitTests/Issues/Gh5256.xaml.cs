using System;
using System.Windows.Input;
using Xunit;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh5256Entry : Entry
	{
		public Gh5256Entry()
		{
			base.Completed += (o, e) => this.Completed?.Execute(o);
		}
		public static readonly BindableProperty CompletedProperty =
			BindableProperty.Create("Completed", typeof(ICommand), typeof(Gh5256Entry), default(ICommand));

		public new ICommand Completed
		{
			get => (ICommand)GetValue(CompletedProperty);
			set => SetValue(CompletedProperty, value);
		}
	}

	//[XamlCompilation(XamlCompilationOptions.Skip)]
	public partial class Gh5256 : ContentPage
	{
		public Gh5256() => InitializeComponent();
		public Gh5256(bool useCompiledXaml)
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
			public void EventOverriding(bool useCompiledXaml)
			{
				var completed = false;
				var layout = new Gh5256(useCompiledXaml) { BindingContext = new { CompletedCommand = new Command(() => completed = true) } };
				layout.entry.SendCompleted();
				Assert.True(completed, "the Completed event should have invoked CompletedCommand");
			}
		}

	}
}
