using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class EntryCellTests : BaseTestFixture
	{
		public EntryCellTests()
		{
			Device.PlatformServices = new MockPlatformServices();
		}

		[Fact]
		public void ChangingHorizontalTextAlignmentFiresXAlignChanged()
		{
			var entryCell = new EntryCell { HorizontalTextAlignment = TextAlignment.Center };

			var xAlignFired = false;
			var horizontalTextAlignmentFired = false;

			entryCell.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "XAlign")
				{
					xAlignFired = true;
				}
				else if (args.PropertyName == EntryCell.HorizontalTextAlignmentProperty.PropertyName)
				{
					horizontalTextAlignmentFired = true;
				}
			};

			entryCell.HorizontalTextAlignment = TextAlignment.End;

			Assert.True(xAlignFired);
			Assert.True(horizontalTextAlignmentFired);
		}

		[Fact]
		public void EntryCellXAlignBindingMatchesHorizontalTextAlignmentBinding()
		{
			var vm = new ViewModel();
			vm.Alignment = TextAlignment.Center;

			var entryCellXAlign = new EntryCell() { BindingContext = vm };
			entryCellXAlign.SetBinding(EntryCell.XAlignProperty, new Binding("Alignment"));

			var entryCellHorizontalTextAlignment = new EntryCell() { BindingContext = vm };
			entryCellHorizontalTextAlignment.SetBinding(EntryCell.HorizontalTextAlignmentProperty, new Binding("Alignment"));

			Assert.Equal(TextAlignment.Center, entryCellXAlign.XAlign);
			Assert.Equal(TextAlignment.Center, entryCellHorizontalTextAlignment.HorizontalTextAlignment);

			vm.Alignment = TextAlignment.End;

			Assert.Equal(TextAlignment.End, entryCellXAlign.XAlign);
			Assert.Equal(TextAlignment.End, entryCellHorizontalTextAlignment.HorizontalTextAlignment);
		}

		sealed class ViewModel : INotifyPropertyChanged
		{
			TextAlignment alignment;

			public TextAlignment Alignment
			{
				get { return alignment; }
				set
				{
					alignment = value;
					OnPropertyChanged();
				}
			}

			public event PropertyChangedEventHandler PropertyChanged;

			void OnPropertyChanged([CallerMemberName] string propertyName = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}