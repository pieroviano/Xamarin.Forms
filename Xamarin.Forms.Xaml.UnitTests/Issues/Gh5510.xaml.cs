using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class Gh5510VM : INotifyPropertyChanged
	{
		private string name = "Bill";
		private Dictionary<string, string> errors;

		public Gh5510VM()
		{
			errors = new Dictionary<string, string>
			{
				{ nameof(Name), "An error" }
			};
		}

		public string Name
		{
			get => name;
			set => SetProperty(ref name, value);
		}

		public Dictionary<string, string> Errors
		{
			get => errors;
			private set => SetProperty(ref errors, value);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void ClearErrorForPerson() => Errors = new Dictionary<string, string>();

		protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(field, value))
				return false;
			field = value;
			RaisePropertyChanged(propertyName);
			return true;
		}

		protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);
	}

	public partial class Gh5510 : ContentPage
	{
		public Gh5510() => InitializeComponent();
		public Gh5510(bool useCompiledXaml)
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
			public void CompileBindingWithIndexer(bool useCompiledXaml)
			{
				if (useCompiledXaml)
					AssertEx.DoesNotThrow(() => MockCompiler.Compile(typeof(Gh5510)));

				var vm = new Gh5510VM();
				var layout = new Gh5510(useCompiledXaml) { BindingContext = vm };
				Assert.Equal(Color.Red, layout.entry.TextColor);
				vm.ClearErrorForPerson();
				Assert.Equal(Color.Black, layout.entry.TextColor);
			}
		}
	}
}
