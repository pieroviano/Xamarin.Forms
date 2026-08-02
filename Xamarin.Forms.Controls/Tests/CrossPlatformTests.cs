using System.Threading.Tasks;
using Xunit;

namespace Xamarin.Forms.Controls.Tests
{
	public class CrossPlatformTests : CrossPlatformTestFixture
	{
		[Fact]
		[Trait("Description", "Always Passes")]
		public void PassingCrossPlatformTest()
		{
			// xUnit has no Assert.Pass; a test that returns without throwing has passed.
		}

		[Fact]
		[Trait("Description", "Setting ListView Header to null should not crash")]
		public void Bugzilla28575()
		{
			string header = "Hello I am Header!!!!";

			var listview = new ListView();
			listview.Header = new Label()
			{
				Text = header,
				TextColor = Color.Red,
#pragma warning disable 618
				XAlign = TextAlignment.Center
#pragma warning restore 618
			};

			listview.Header = null;
		}

		[Fact]
		[Trait("Description", "isPresentedChanged raises multiple times")]
		public void Bugzilla32230()
		{
			var mdp = new FlyoutPage();
			var count = 0;
			mdp.IsPresentedChanged += (sender, args) => { count += 1; };

			mdp.IsPresented = true;
			Assert.Equal(1, count);

			mdp.IsPresented = false;
			mdp.IsPresented = true;
			Assert.Equal(3, count);
		}

		[Fact]
		[Trait("Description", "ButtonRenderer UpdateTextColor function crash")]
		public async Task Bugzilla35738()
		{
			var customButton = new TestClasses.CustomButton() { Text = "This is a custom button", TextColor = Color.Fuchsia };
			await TestingPlatform.CreateRenderer(customButton);
		}

		[Fact]
		[Trait("Description", "[Bug] CollectionView exception when IsGrouped=true and null ItemSource")]
		public async Task GitHub8269()
		{
			var collectionView = new CollectionView { ItemsSource = null, IsGrouped = true };
			await TestingPlatform.CreateRenderer(collectionView);
		}

		[Fact]
		[Trait("Description", "[Bug] [UWP] NullReferenceException when call SavePropertiesAsync method off the main thread")]
		public async Task GitHub8682()
		{
			await Task.Run(async () =>
			{
				await Application.Current.SavePropertiesAsync();
			});
		}
	}
}
