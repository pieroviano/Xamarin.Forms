using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class NavigationUnitTest : BaseTestFixture
	{
		[Fact]
		public async Task TestNavigationImplPush()
		{
			NavigationPage nav = new NavigationPage();

			Assert.Null(nav.RootPage);
			Assert.Null(nav.CurrentPage);

			Label child = new Label { Text = "Label" };
			Page childRoot = new ContentPage { Content = child };

			await nav.Navigation.PushAsync(childRoot);

			Assert.Same(childRoot, nav.RootPage);
			Assert.Same(childRoot, nav.CurrentPage);
			Assert.Same(nav.RootPage, nav.CurrentPage);
		}

		[Fact]
		public async Task TestNavigationImplPop()
		{
			NavigationPage nav = new NavigationPage();

			Label child = new Label();
			Page childRoot = new ContentPage { Content = child };

			Label child2 = new Label();
			Page childRoot2 = new ContentPage { Content = child2 };

			await nav.Navigation.PushAsync(childRoot);
			await nav.Navigation.PushAsync(childRoot2);

			bool fired = false;
			nav.Popped += (sender, e) => fired = true;

			Assert.Same(childRoot, nav.RootPage);
			Assert.NotSame(childRoot2, nav.RootPage);
			Assert.NotSame(nav.RootPage, nav.CurrentPage);

			var popped = await nav.Navigation.PopAsync();

			Assert.True(fired);
			Assert.Same(childRoot, nav.RootPage);
			Assert.Same(childRoot, nav.CurrentPage);
			Assert.Same(nav.RootPage, nav.CurrentPage);
			Assert.Equal(childRoot2, popped);

			await nav.PopAsync();
			var last = await nav.Navigation.PopAsync();

			Assert.Null(last);
			Assert.NotNull(nav.RootPage);
			Assert.NotNull(nav.CurrentPage);
			Assert.Same(nav.RootPage, nav.CurrentPage);
		}

		[Fact]
		public async Task TestPushRoot()
		{
			NavigationPage nav = new NavigationPage();

			Assert.Null(nav.RootPage);
			Assert.Null(nav.CurrentPage);

			Label child = new Label { Text = "Label" };
			Page childRoot = new ContentPage { Content = child };

			await nav.PushAsync(childRoot);

			Assert.Same(childRoot, nav.RootPage);
			Assert.Same(childRoot, nav.CurrentPage);
			Assert.Same(nav.RootPage, nav.CurrentPage);
		}

		[Fact]
		public async Task TestPushEvent()
		{
			NavigationPage nav = new NavigationPage();

			Label child = new Label();
			Page childRoot = new ContentPage { Content = child };

			bool fired = false;
			nav.Pushed += (sender, e) => fired = true;

			await nav.PushAsync(childRoot);

			Assert.True(fired);
		}

		[Fact]
		public async Task TestDoublePush()
		{
			NavigationPage nav = new NavigationPage();

			Label child = new Label();
			Page childRoot = new ContentPage { Content = child };

			await nav.PushAsync(childRoot);

			bool fired = false;
			nav.Pushed += (sender, e) => fired = true;

			Assert.Same(childRoot, nav.RootPage);
			Assert.Same(childRoot, nav.CurrentPage);

			await nav.PushAsync(childRoot);

			Assert.False(fired);
			Assert.Same(childRoot, nav.RootPage);
			Assert.Same(childRoot, nav.CurrentPage);
			Assert.Same(nav.RootPage, nav.CurrentPage);
		}

		[Fact]
		public async Task TestPop()
		{
			NavigationPage nav = new NavigationPage();

			Label child = new Label();
			Page childRoot = new ContentPage { Content = child };

			Label child2 = new Label();
			Page childRoot2 = new ContentPage { Content = child2 };

			await nav.PushAsync(childRoot);
			await nav.PushAsync(childRoot2);

			bool fired = false;
			nav.Popped += (sender, e) => fired = true;
			var popped = await nav.PopAsync();

			Assert.True(fired);
			Assert.Same(childRoot, nav.CurrentPage);
			Assert.Equal(childRoot2, popped);

			await nav.PopAsync();
			var last = await nav.PopAsync();

			Assert.Null(last);
		}

		[Fact]
		public void TestTint()
		{
			var nav = new NavigationPage();

			Assert.Equal(Color.Default, nav.Tint);

			bool signaled = false;
			nav.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "Tint")
					signaled = true;
			};

			nav.Tint = new Color(1, 0, 0);

			Assert.Equal(new Color(1, 0, 0), nav.Tint);
			Assert.True(signaled);
		}

		[Fact]
		public void TestTintDoubleSet()
		{
			var nav = new NavigationPage();

			bool signaled = false;
			nav.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "Tint")
					signaled = true;
			};

			nav.Tint = nav.Tint;

			Assert.False(signaled);
		}

		[Fact]
		public async Task TestPopToRoot()
		{
			var nav = new NavigationPage();

			bool signaled = false;
			nav.PoppedToRoot += (sender, args) => signaled = true;

			var root = new ContentPage { Content = new View() };
			var child1 = new ContentPage { Content = new View() };
			var child2 = new ContentPage { Content = new View() };

			await nav.PushAsync(root);
			await nav.PushAsync(child1);
			await nav.PushAsync(child2);

			nav.PopToRootAsync();

			Assert.True(signaled);
			Assert.Same(root, nav.RootPage);
			Assert.Same(root, nav.CurrentPage);
			Assert.Same(nav.RootPage, nav.CurrentPage);
		}

		[Fact]
		public async Task TestPopToRootEventArgs()
		{
			var nav = new NavigationPage();

			List<Page> poppedChildren = null;
			nav.PoppedToRoot += (sender, args) => poppedChildren = (args as PoppedToRootEventArgs).PoppedPages.ToList();

			var root = new ContentPage { Content = new View() };
			var child1 = new ContentPage { Content = new View() };
			var child2 = new ContentPage { Content = new View() };

			await nav.PushAsync(root);
			await nav.PushAsync(child1);
			await nav.PushAsync(child2);

			await nav.PopToRootAsync();

			Assert.NotNull(poppedChildren);
			Assert.Equal(2, poppedChildren.Count);
			Assert.Contains(child1, poppedChildren);
			Assert.Contains(child2, poppedChildren);
			Assert.Same(root, nav.RootPage);
			Assert.Same(root, nav.CurrentPage);
			Assert.Same(nav.RootPage, nav.CurrentPage);
		}

		[Fact]
		public async Task PeekOne()
		{
			var nav = new NavigationPage();

			bool signaled = false;
			nav.PoppedToRoot += (sender, args) => signaled = true;

			var root = new ContentPage { Content = new View() };
			var child1 = new ContentPage { Content = new View() };
			var child2 = new ContentPage { Content = new View() };

			await nav.PushAsync(root);
			await nav.PushAsync(child1);
			await nav.PushAsync(child2);

			Assert.Equal(((INavigationPageController)nav).Peek(1), child1);
		}

		[Fact]
		public async Task PeekZero()
		{
			var nav = new NavigationPage();

			bool signaled = false;
			nav.PoppedToRoot += (sender, args) => signaled = true;

			var root = new ContentPage { Content = new View() };
			var child1 = new ContentPage { Content = new View() };
			var child2 = new ContentPage { Content = new View() };

			await nav.PushAsync(root);
			await nav.PushAsync(child1);
			await nav.PushAsync(child2);

			Assert.Equal(((INavigationPageController)nav).Peek(0), child2);
			Assert.Equal(((INavigationPageController)nav).Peek(), child2);
		}

		[Fact]
		public async Task PeekPastStackDepth()
		{
			var nav = new NavigationPage();

			bool signaled = false;
			nav.PoppedToRoot += (sender, args) => signaled = true;

			var root = new ContentPage { Content = new View() };
			var child1 = new ContentPage { Content = new View() };
			var child2 = new ContentPage { Content = new View() };

			await nav.PushAsync(root);
			await nav.PushAsync(child1);
			await nav.PushAsync(child2);

			Assert.Equal(((INavigationPageController)nav).Peek(3), null);
		}

		[Fact]
		public async Task PeekShallow()
		{
			var nav = new NavigationPage();

			bool signaled = false;
			nav.PoppedToRoot += (sender, args) => signaled = true;

			var root = new ContentPage { Content = new View() };
			var child1 = new ContentPage { Content = new View() };
			var child2 = new ContentPage { Content = new View() };

			await nav.PushAsync(root);
			await nav.PushAsync(child1);
			await nav.PushAsync(child2);

			Assert.Equal(((INavigationPageController)nav).Peek(-1), null);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(3)]
		public async Task PeekEmpty(int depth)
		{
			var nav = new NavigationPage();

			bool signaled = false;
			nav.PoppedToRoot += (sender, args) => signaled = true;

			Assert.Equal(((INavigationPageController)nav).Peek(depth), null);
		}


		[Fact]
		public void ConstructWithRoot()
		{
			var root = new ContentPage();
			var nav = new NavigationPage(root);


			Assert.Equal(1, ((INavigationPageController)nav).StackDepth);
			Assert.Equal(root, ((IElementController)nav).LogicalChildren[0]);

		}

		[Fact]
		public void TitleViewSetProperty()
		{
			var root = new ContentPage();
			var nav = new NavigationPage(root);

			View target = new View();

			NavigationPage.SetTitleView(root, target);

			var result = NavigationPage.GetTitleView(root);

			Assert.Same(result, target);
		}

		[Fact]
		public void TitleViewSetsParentWhenAdded()
		{
			var root = new ContentPage();
			var nav = new NavigationPage(root);

			View target = new View();

			NavigationPage.SetTitleView(root, target);

			Assert.Same(root, target.Parent);
		}

		[Fact]
		public void TitleViewClearsParentWhenRemoved()
		{
			var root = new ContentPage();
			var nav = new NavigationPage(root);

			View target = new View();

			NavigationPage.SetTitleView(root, target);

			NavigationPage.SetTitleView(root, null);

			Assert.Null(target.Parent);
		}

		[Fact]
		public async Task NavigationChangedEventArgs()
		{
			var rootPage = new ContentPage { Title = "Root" };
			var navPage = new NavigationPage(rootPage);

			var rootArg = new Page();

			navPage.Pushed += (s, e) =>
			{
				rootArg = e.Page;
			};

			var pushPage = new ContentPage
			{
				Title = "Page 2"
			};

			await navPage.PushAsync(pushPage);

			Assert.Equal(rootArg, pushPage);

			var secondPushPage = new ContentPage
			{
				Title = "Page 3"
			};

			await navPage.PushAsync(secondPushPage);

			Assert.Equal(rootArg, secondPushPage);
		}

		[Fact]
		public async Task CurrentPageChanged()
		{
			var root = new ContentPage { Title = "Root" };
			var navPage = new NavigationPage(root);

			bool changing = false;
			navPage.PropertyChanging += (object sender, PropertyChangingEventArgs e) =>
			{
				if (e.PropertyName == NavigationPage.CurrentPageProperty.PropertyName)
				{
					Assert.Same(root, navPage.CurrentPage);
					changing = true;
				}
			};

			var next = new ContentPage { Title = "Next" };

			bool changed = false;
			navPage.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == NavigationPage.CurrentPageProperty.PropertyName)
				{
					Assert.Same(next, navPage.CurrentPage);
					changed = true;
				}
			};

			await navPage.PushAsync(next);

			Assert.True(changing, "PropertyChanging was not raised for 'CurrentPage'");
			Assert.True(changed, "PropertyChanged was not raised for 'CurrentPage'");
		}

		[Fact]
		public async Task HandlesPopToRoot()
		{
			var root = new ContentPage { Title = "Root" };
			var navPage = new NavigationPage(root);

			await navPage.PushAsync(new ContentPage());
			await navPage.PushAsync(new ContentPage());

			bool popped = false;
			navPage.PoppedToRoot += (sender, args) =>
			{
				popped = true;
			};

			await navPage.Navigation.PopToRootAsync();

			Assert.True(popped);
		}

		[Fact]
		public void SendsBackButtonEventToCurrentPage()
		{
			var current = new BackButtonPage();
			var navPage = new NavigationPage(current);

			var emitted = false;
			current.BackPressed += (sender, args) => emitted = true;

			navPage.SendBackButtonPressed();

			Assert.True(emitted);
		}

		[Fact]
		public void DoesNotSendBackEventToNonCurrentPage()
		{
			var current = new BackButtonPage();
			var navPage = new NavigationPage(current);
			navPage.PushAsync(new ContentPage());

			var emitted = false;
			current.BackPressed += (sender, args) => emitted = true;

			navPage.SendBackButtonPressed();

			Assert.False(emitted);
		}

		[Fact]
		public async Task NavigatesBackWhenBackButtonPressed()
		{
			var root = new ContentPage { Title = "Root" };
			var navPage = new NavigationPage(root);

			await navPage.PushAsync(new ContentPage());

			var result = navPage.SendBackButtonPressed();

			Assert.Equal(root, navPage.CurrentPage);
			Assert.True(result);
		}

		[Fact]
		public async Task DoesNotNavigatesBackWhenBackButtonPressedIfHandled()
		{
			var root = new BackButtonPage { Title = "Root" };
			var second = new BackButtonPage() { Handle = true };
			var navPage = new NavigationPage(root);

			await navPage.PushAsync(second);

			navPage.SendBackButtonPressed();

			Assert.Equal(second, navPage.CurrentPage);
		}

		[Fact]
		public void DoesNotHandleBackButtonWhenNoNavStack()
		{
			var root = new ContentPage { Title = "Root" };
			var navPage = new NavigationPage(root);

			var result = navPage.SendBackButtonPressed();
			Assert.False(result);
		}

		[Fact]
		public void TestInsertPage()
		{
			var root = new ContentPage { Title = "Root" };
			var newPage = new ContentPage();
			var navPage = new NavigationPage(root);

			navPage.Navigation.InsertPageBefore(newPage, navPage.RootPage);

			Assert.Same(newPage, navPage.RootPage);
			Assert.NotSame(newPage, navPage.CurrentPage);
			Assert.NotSame(navPage.RootPage, navPage.CurrentPage);
			Assert.Same(root, navPage.CurrentPage);

			Assert.Throws<ArgumentException>(() =>
			{
				navPage.Navigation.InsertPageBefore(new ContentPage(), new ContentPage());
			});

			Assert.Throws<ArgumentException>(() =>
			{
				navPage.Navigation.InsertPageBefore(navPage.RootPage, navPage.CurrentPage);
			});

			Assert.Throws<ArgumentNullException>(() =>
			{
				navPage.Navigation.InsertPageBefore(null, navPage.CurrentPage);
			});

			Assert.Throws<ArgumentNullException>(() =>
			{
				navPage.Navigation.InsertPageBefore(new ContentPage(), null);
			});
		}

		[Fact]
		public async Task TestRemovePage()
		{
			var root = new ContentPage { Title = "Root" };
			var newPage = new ContentPage();
			var navPage = new NavigationPage(root);
			await navPage.PushAsync(newPage);

			navPage.Navigation.RemovePage(root);

			Assert.Same(newPage, navPage.RootPage);
			Assert.Same(newPage, navPage.CurrentPage);
			Assert.Same(navPage.RootPage, navPage.CurrentPage);
			Assert.NotSame(root, navPage.CurrentPage);

			Assert.Throws<ArgumentException>(() =>
			{
				navPage.Navigation.RemovePage(root);
			});

			Assert.Throws<InvalidOperationException>(() =>
			{
				navPage.Navigation.RemovePage(newPage);
			});

			Assert.Throws<ArgumentNullException>(() =>
			{
				navPage.Navigation.RemovePage(null);
			});
		}

		[Fact]
		[Trait("Description", "CurrentPage should not be set to null when you attempt to pop the last page")]
		[Trait("Bugzilla", "28335")]
		public async Task CurrentPageNotNullPoppingRoot()
		{
			var root = new ContentPage { Title = "Root" };
			var navPage = new NavigationPage(root);
			var popped = await navPage.PopAsync();
			Assert.Null(popped);
			Assert.Same(root, navPage.CurrentPage);
		}

		[Fact]
		[Trait("Bugzilla", "31171")]
		public async Task ReleasesPoppedPage()
		{
			var root = new ContentPage { Title = "Root" };
			var navPage = new NavigationPage(root);

			var isFinalized = false;

			await navPage.PushAsync(new PageWithFinalizer(() => isFinalized = true));
			await navPage.PopAsync();

			await Task.Delay(100);

			GC.Collect();
			GC.WaitForPendingFinalizers();

			Assert.True(isFinalized);
		}
	}

	internal class BackButtonPage : ContentPage
	{
		public event EventHandler BackPressed;

		public bool Handle = false;

		protected override bool OnBackButtonPressed()
		{
			if (BackPressed != null)
				BackPressed(this, EventArgs.Empty);
			return Handle;
		}
	}

	internal class PageWithFinalizer : Page
	{
		Action OnFinalize;
		public PageWithFinalizer(Action onFinalize)
		{
			OnFinalize = onFinalize;
		}

		~PageWithFinalizer()
		{
			OnFinalize();
		}
	}
}
