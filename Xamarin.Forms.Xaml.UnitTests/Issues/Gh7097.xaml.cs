using System;
using System.Collections.Generic;
using System.Windows.Input;
using Xunit;
using Xamarin.Forms;
using Xamarin.Forms.Core.UnitTests;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public partial class Gh7097 : Gh7097Base
	{
		public Gh7097() => InitializeComponent();
		public Gh7097(bool useCompiledXaml) : base(useCompiledXaml)
		{
			//this stub will be replaced at compile time
		}

		public class Tests
		: IDisposable{
			IReadOnlyList<string> _flags;
			public Tests()
{
				Device.PlatformServices = new MockPlatformServices();
				_flags = Device.Flags;
				Device.SetFlags(new List<string>(Device.Flags ?? new List<string>()) { "CollectionView_Experimental" }.AsReadOnly());
			}

			public void Dispose()
{
				Device.PlatformServices = null;
				Device.SetFlags(_flags);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			public void CanXReferenceRoot(bool useCompiledXaml)
			{
				var layout = new Gh7097(useCompiledXaml)
				{
					BindingContext = new
					{
						Button1Command = new MockCommand(),
						Button2Command = new MockCommand(),
					}
				};
				var cv = layout.Content as CollectionView;
				var content = cv.ItemTemplate.CreateContent() as StackLayout;
				var btn1 = content.Children[0] as Button;
				Assert.IsType<MockCommand>(btn1.Command);
			}

			[Theory]
			[InlineData(false)]
			[InlineData(true)]
			//this was later reported as https://github.com/xamarin/Xamarin.Forms/issues/7286
			public void RegisteringXNameOnSubPages(bool useCompiledXaml)
			{
				var layout = new Gh7097(useCompiledXaml);
				var s = layout.FindByName("self");
				Assert.NotNull(layout.self);
				Assert.NotNull(layout.collectionview);
			}

			class MockCommand : ICommand
			{
#pragma warning disable 0067
				public event EventHandler CanExecuteChanged;
#pragma warning restore 0067
				public bool CanExecute(object parameter) => true;
				public void Execute(object parameter) => throw new NotImplementedException();
			}
		}
	}
}
