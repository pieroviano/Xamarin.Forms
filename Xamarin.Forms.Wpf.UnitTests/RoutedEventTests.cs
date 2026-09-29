using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Xunit;

namespace Wpf.UnitTests
{
	/// <summary>Routed events on the logical tree: bubbling, tunnelling, direct; Handled; class handlers first.</summary>
	public class RoutedEventTests : WpfTestBase
	{
		static readonly RoutedEvent Bubbling =
			EventManager.RegisterRoutedEvent("TestBubble", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RoutedEventTests));

		static readonly RoutedEvent Tunnelling =
			EventManager.RegisterRoutedEvent("TestTunnel", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(RoutedEventTests));

		static readonly RoutedEvent Direct =
			EventManager.RegisterRoutedEvent("TestDirect", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(RoutedEventTests));

		sealed class Tree
		{
			public readonly Button Leaf = new Button();
			public readonly StackPanel Middle = new StackPanel();
			public readonly Grid Root = new Grid();

			public Tree()
			{
				Middle.Children.Add(Leaf);
				Root.Children.Add(Middle);
			}
		}

		[Fact]
		public void ABubblingEventGoesFromTheSourceToTheRoot()
		{
			Run(() =>
			{
				var tree = new Tree();
				var order = new List<string>();
				tree.Root.AddHandler(Bubbling, new RoutedEventHandler((s, e) => order.Add("root")));
				tree.Middle.AddHandler(Bubbling, new RoutedEventHandler((s, e) => order.Add("middle")));
				tree.Leaf.AddHandler(Bubbling, new RoutedEventHandler((s, e) =>
				{
					order.Add("leaf");
					Assert.Same(tree.Leaf, e.Source);
				}));

				tree.Leaf.RaiseEvent(new RoutedEventArgs(Bubbling));

				Assert.Equal(new[] { "leaf", "middle", "root" }, order);
			});
		}

		[Fact]
		public void ATunnellingEventGoesFromTheRootToTheSource()
		{
			Run(() =>
			{
				var tree = new Tree();
				var order = new List<string>();
				tree.Root.AddHandler(Tunnelling, new RoutedEventHandler((s, e) => order.Add("root")));
				tree.Middle.AddHandler(Tunnelling, new RoutedEventHandler((s, e) => order.Add("middle")));
				tree.Leaf.AddHandler(Tunnelling, new RoutedEventHandler((s, e) => order.Add("leaf")));

				tree.Leaf.RaiseEvent(new RoutedEventArgs(Tunnelling));

				Assert.Equal(new[] { "root", "middle", "leaf" }, order);
			});
		}

		[Fact]
		public void ADirectEventReachesTheSourceOnly()
		{
			Run(() =>
			{
				var tree = new Tree();
				var root = 0;
				var leaf = 0;
				tree.Root.AddHandler(Direct, new RoutedEventHandler((s, e) => root++));
				tree.Leaf.AddHandler(Direct, new RoutedEventHandler((s, e) => leaf++));

				tree.Leaf.RaiseEvent(new RoutedEventArgs(Direct));

				Assert.Equal(0, root);
				Assert.Equal(1, leaf);
			});
		}

		[Fact]
		public void AHandledEventStopsUnlessAHandlerAsksForHandledEventsToo()
		{
			Run(() =>
			{
				var tree = new Tree();
				var plain = 0;
				var always = 0;
				tree.Leaf.AddHandler(Bubbling, new RoutedEventHandler((s, e) => e.Handled = true));
				tree.Root.AddHandler(Bubbling, new RoutedEventHandler((s, e) => plain++));
				tree.Root.AddHandler(Bubbling, new RoutedEventHandler((s, e) => always++), true);

				tree.Leaf.RaiseEvent(new RoutedEventArgs(Bubbling));

				Assert.Equal(0, plain);
				Assert.Equal(1, always);
			});
		}

		[Fact]
		public void AButtonClickBubblesToItsToolBar()
		{
			Run(() =>
			{
				var button = new Button { Content = "B" };
				var toolBar = new ToolBar { Items = { button } };
				object clicked = null;
				toolBar.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((s, e) => clicked = e.OriginalSource));

				button.PerformClick();

				Assert.Same(button, clicked);
			});
		}

		sealed class Recording : Border
		{
			public readonly List<string> Calls = new List<string>();

			protected override void OnMouseDown(MouseButtonEventArgs e) => Calls.Add("class");
		}

		[Fact]
		public void TheClassHandlerRunsBeforeTheInstanceHandlers()
		{
			Run(() =>
			{
				var element = new Recording();
				element.MouseDown += (s, e) => element.Calls.Add("instance");

				element.RaiseEvent(new MouseButtonEventArgs(_ => new Point(), MouseButton.Left, MouseButtonState.Pressed, 1) { RoutedEvent = UIElement.MouseDownEvent });

				Assert.Equal(new[] { "class", "instance" }, element.Calls);
			});
		}

		[Fact]
		public void AMouseDownRaisesTheButtonSpecificEventOnEachElementOfTheRoute()
		{
			Run(() =>
			{
				var tree = new Tree();
				var left = new List<object>();
				tree.Leaf.MouseLeftButtonDown += (s, e) => left.Add(s);
				tree.Root.MouseLeftButtonDown += (s, e) => left.Add(s);

				tree.Leaf.RaiseEvent(new MouseButtonEventArgs(_ => new Point(), MouseButton.Left, MouseButtonState.Pressed, 1) { RoutedEvent = UIElement.MouseDownEvent });

				Assert.Equal(new object[] { tree.Leaf, tree.Root }, left);
			});
		}

		[Fact]
		public void ASecondClickRaisesMouseDoubleClickOnTheControl()
		{
			Run(() =>
			{
				var list = new ListBox();
				var doubles = 0;
				list.MouseDoubleClick += (s, e) => doubles++;

				list.RaiseEvent(new MouseButtonEventArgs(_ => new Point(), MouseButton.Left, MouseButtonState.Pressed, 1) { RoutedEvent = UIElement.MouseDownEvent });
				list.RaiseEvent(new MouseButtonEventArgs(_ => new Point(), MouseButton.Left, MouseButtonState.Pressed, 2) { RoutedEvent = UIElement.MouseDownEvent });

				Assert.Equal(1, doubles);
			});
		}

		[Fact]
		public void LoadedIsRaisedParentFirstOnceTheWindowShows()
		{
			Run(() =>
			{
				var order = new List<string>();
				var child = new Button();
				var window = new Window { Content = new StackPanel { Children = { child } } };
				window.Loaded += (s, e) => order.Add("window");
				child.Loaded += (s, e) => order.Add("child");

				window.Show();
				Assert.Empty(order);
				Assert.True(PumpUntil(() => order.Count == 2, window));

				Assert.Equal(new[] { "window", "child" }, order);
				Assert.True(child.IsLoaded);
				window.Close();
				Assert.False(child.IsLoaded);
			});
		}

		[Fact]
		public void AnElementAddedToALoadedTreeLoads()
		{
			Run(() =>
			{
				var panel = new StackPanel();
				var window = Host(panel);
				Assert.True(PumpUntil(() => panel.IsLoaded, window));
				var late = new Button();
				var loaded = false;
				late.Loaded += (s, e) => loaded = true;

				panel.Children.Add(late);

				Assert.True(loaded);
				window.Close();
			});
		}
	}
}
