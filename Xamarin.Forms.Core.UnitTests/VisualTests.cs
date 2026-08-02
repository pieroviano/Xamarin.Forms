using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xamarin.Forms.Internals;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class VisualTests : BaseTestFixture
	{
		public VisualTests()
		{
			var mockDeviceInfo = new TestDeviceInfo();
			Device.Info = mockDeviceInfo;
		}


		[Fact]
		public void ShellPropagatesChangeToNewElements()
		{
			Button button = new Button();
			StackLayout flyout = new StackLayout();
			Shell shell = new Shell()
			{
				Visual = Forms.VisualMarker.Default,
				Items =
				{
					new ShellItem()
					{
						Items =
						{
							new ShellSection()
							{
								Items =
								{
									new ShellContent()
									{
										Content = new ContentPage()
										{
											Content = new Label()
										}
									}
								}
							}
						}
					}
				}
			};

			shell.Visual = VisualMarker.Material;
			shell.FlyoutHeader = flyout;
			shell.Items.Add(
				new ShellItem()
				{
					Items =
						{
							new ShellSection()
							{
								Items =
								{
									new ShellContent()
									{
										Content = new ContentPage()
										{
											Content = button
										}
									}
								}
							}
						}
				});

			var buttonVisualController = (button as IVisualController);
			var stacklayoutVisualController = (flyout as IVisualController);
			Assert.Equal(Forms.VisualMarker.Material, buttonVisualController.EffectiveVisual);
			Assert.Equal(Forms.VisualMarker.Material, stacklayoutVisualController.EffectiveVisual);
		}


		[Fact]
		public void ShellPropagatesDownVisualChange()
		{
			Button button = new Button();
			StackLayout flyout = new StackLayout();
			Shell shell = new Shell()
			{

				Visual = Forms.VisualMarker.Default,
				FlyoutHeader = flyout,
				Items =
				{
					new ShellItem()
					{
						Items =
						{
							new ShellSection()
							{
								Items =
								{
									new ShellContent()
									{
										Content = new ContentPage()
										{
											Content = button
										}
									}
								}
							}
						}
					}
				}
			};

			var buttonVisualController = (button as IVisualController);
			var stacklayoutVisualController = (flyout as IVisualController);

			Assert.Equal(Forms.VisualMarker.Default, buttonVisualController.EffectiveVisual);
			Assert.Equal(Forms.VisualMarker.Default, stacklayoutVisualController.EffectiveVisual);
			shell.Visual = VisualMarker.Material;
			Assert.Equal(Forms.VisualMarker.Material, buttonVisualController.EffectiveVisual);
			Assert.Equal(Forms.VisualMarker.Material, stacklayoutVisualController.EffectiveVisual);
		}


		[Fact]
		public void ListViewVisualIsInheritedByViewCells()
		{
			var lv = new ListView { Visual = Forms.VisualMarker.Material, ItemTemplate = new DataTemplate(() => new ViewCell { View = new View() }) };

			lv.ItemsSource = Enumerable.Range(0, 10);

			ViewCell cell = lv.TemplatedItems[0] as ViewCell;
			IVisualController target = cell.View;
			Assert.Equal(Forms.VisualMarker.Material, target.EffectiveVisual);
		}

		[Fact]
		public void ListViewVisualIsInheritedByImageInViewCells()
		{
			var lv = new ListView { Visual = Forms.VisualMarker.Material, ItemTemplate = new DataTemplate(() => new ViewCell { View = new Label() }) };

			lv.ItemsSource = Enumerable.Range(0, 10);

			ViewCell cell = lv.TemplatedItems[0] as ViewCell;
			IVisualController target = cell.View;
			Assert.Equal(Forms.VisualMarker.Material, target.EffectiveVisual);
		}

		[Fact]
		public void ScrollViewSetsVisualAndGrandchildMaintainsParentExplicitValue()
		{
			var layout = ImplicitDefaultScrollView();
			var layout2 = ExplicitMaterialScrollView();
			IVisualController view = ImplicitDefaultView();

			AddExplicitMaterialToScrollView(layout, layout2);
			AddImplicitToMaterialScrollView(layout2, (View)view);

			layout.Visual = Forms.VisualMarker.Default;

			var target = view.EffectiveVisual;

			Assert.True(target == Forms.VisualMarker.Material, "EffectiveVisual should be Material");

			Assert.Equal(Forms.VisualMarker.MatchParent, ((View)view).Visual);
			Assert.Equal(Forms.VisualMarker.Material, layout2.Visual);
		}

		[Fact]
		public void GrandparentSetsVisualAndGrandchildMaintainsParentExplicitValue()
		{
			var layout = ImplicitDefaultLayout();
			var layout2 = ExplicitDefaultLayout();
			IVisualController view = ImplicitDefaultView();

			AddExplicitDefaultToLayout(layout, layout2);
			AddImplicitToDefault(layout2, (View)view);

			layout.Visual = Forms.VisualMarker.Material;

			var target = view.EffectiveVisual;

			Assert.True(!target.IsMaterial(), "EffectiveVisual should be Default");
			Assert.True(target.IsDefault(), "EffectiveVisual should be Default");

			Assert.Equal(Forms.VisualMarker.MatchParent, ((View)view).Visual);
			Assert.Equal(Forms.VisualMarker.Default, layout2.Visual);
		}

		[Fact]
		public void GrandparentSetsVisualAndImplicitDescendentsInheritValue()
		{
			var layout = ImplicitDefaultLayout();
			var layout2 = ImplicitDefaultLayout();
			IVisualController view = ImplicitDefaultView();

			AddImplicitToDefault(layout, layout2);

			AddImplicitToDefault(layout2, (View)view);

			layout.Visual = Forms.VisualMarker.Material;

			Assert.True(((IVisualController)layout).EffectiveVisual.IsMaterial());
			Assert.True(((IVisualController)layout2).EffectiveVisual.IsMaterial());

			var target = view.EffectiveVisual;

			Assert.True(target.IsMaterial(), "EffectiveVisual should be Material");
			Assert.True(!target.IsDefault(), "EffectiveVisual should be Material");

			Assert.Equal(Forms.VisualMarker.MatchParent, ((View)view).Visual);
			Assert.Equal(Forms.VisualMarker.MatchParent, layout2.Visual);
		}

		[Fact]
		public void GrandparentSetsOppositeVisualAndGrandchildInheritsParentExplicitValue()
		{
			var layout = ExplicitMaterialLayout();
			var layout2 = ExplicitMaterialLayout();
			IVisualController view = ImplicitDefaultView();

			AddExplicitMaterialToLayout(layout, layout2);

			AddImplicitToMaterial(layout2, (View)view);

			layout.Visual = Forms.VisualMarker.Default;

			Assert.True(((IVisualController)layout).EffectiveVisual.IsDefault());

			Assert.True(((IVisualController)layout2).EffectiveVisual.IsMaterial());

			Assert.True(view.EffectiveVisual.IsMaterial());

			var target = ((View)view).Visual;

			Assert.Equal(Forms.VisualMarker.MatchParent, target);
		}

		[Fact]
		public void NotifyVisualChangedDoesNotTriggerVisualPropertyChangedUnnecessarily()
		{
			var layout = ExplicitMaterialLayout();
			var layout2 = ImplicitDefaultLayout();
			IVisualController view = new PropertyWatchingView();

			AddImplicitToMaterial(layout, layout2);

			AddImplicitToMaterial(layout2, (View)view);

			layout2.Visual = Forms.VisualMarker.Material;
			Assert.True(view.EffectiveVisual.IsMaterial(), "Implicit Visual not set on view");

			layout.Visual = Forms.VisualMarker.Default;
			Assert.True(layout2.Visual == Forms.VisualMarker.Material, "Explicit Visual not respected on inner layout");
			Assert.True(view.EffectiveVisual.IsMaterial(), "Implicit Visual not set on view");

			var target = ((PropertyWatchingView)view).VisualPropertyChangedCount;

			Assert.Equal(1, target);
		}

		[Fact]
		public void ReParentAndInheritNewParentValue()
		{
			var layout = ExplicitMaterialLayout();
			IVisualController view = ImplicitDefaultView();
			var layout2 = ExplicitDefaultLayout();

			AddImplicitToMaterial(layout, (View)view);

			((View)view).Parent = layout2;

			Assert.True(((IVisualController)layout2).EffectiveVisual.IsDefault());

			var target = view.EffectiveVisual;

			Assert.True(!target.IsMaterial(), "EffectiveVisual should be Default");
			Assert.True(target.IsDefault(), "EffectiveVisual should be Default");

			Assert.Equal(Forms.VisualMarker.MatchParent, ((View)view).Visual);
		}

		[Fact]
		public void ReParentParentAndInheritNewGrandParentValue()
		{
			var layout = ExplicitMaterialLayout();
			var layout2 = ImplicitDefaultLayout();
			IVisualController view = ImplicitDefaultView();
			var layout3 = ExplicitDefaultLayout();

			AddImplicitToMaterial(layout, layout2);
			AddImplicitToMaterial(layout2, (View)view);

			layout2.Parent = layout3;

			Assert.True(((IVisualController)layout2).EffectiveVisual.IsDefault());

			Assert.True(view.EffectiveVisual.IsDefault());

			var target = ((View)view).Visual;

			Assert.Equal(Forms.VisualMarker.MatchParent, target);
		}

		[Fact]
		public void SetVisualToMatchParentAndInheritParentValue()
		{
			var layout = ImplicitDefaultLayout();
			var layout2 = ExplicitMaterialLayout();
			IVisualController view = ExplicitDefaultView();

			AddExplicitMaterialToLayout(layout, layout2);

			AddExplicitDefaultToLayout(layout2, (View)view);

			((View)view).Visual = Forms.VisualMarker.MatchParent;

			var target = view.EffectiveVisual;

			Assert.True(target.IsMaterial(), "EffectiveVisual should be Material");
			Assert.True(!target.IsDefault(), "EffectiveVisual should be Material");
		}

		[Fact]
		public void SetGrandparentAndInheritExplicitParentValue()
		{
			var layout = ExplicitMaterialLayout();
			var layout2 = ExplicitDefaultLayout();
			IVisualController view = ImplicitDefaultView();

			AddExplicitDefaultToLayout(layout, layout2);
			AddImplicitToDefault(layout2, (View)view);

			var target = view.EffectiveVisual;

			Assert.True(((IVisualController)layout).EffectiveVisual.IsMaterial());

			Assert.True(((IVisualController)layout2).EffectiveVisual.IsDefault());

			Assert.True(!target.IsMaterial(), "EffectiveVisual should be Default");
			Assert.True(target.IsDefault(), "EffectiveVisual should be Default");
		}

		[Fact]
		public void SetGrandparentUsingAnonCtorAndMaintainExplicitParentValue()
		{
			var layout = new StackLayout
			{
				Visual = Forms.VisualMarker.Material,
				Children = {
					new StackLayout {
						Visual = Forms.VisualMarker.Default,
						Children = { ImplicitDefaultView() }
					}
				}
			};

			var layout2 = layout.Children[0] as StackLayout;
			IVisualController view = layout2.Children[0] as View;

			var target = view.EffectiveVisual;

			Assert.True(((IVisualController)layout).EffectiveVisual.IsMaterial());

			Assert.True(((IVisualController)layout2).EffectiveVisual.IsDefault());

			Assert.True(!target.IsMaterial(), "EffectiveVisual should be Default");
			Assert.True(target.IsDefault(), "EffectiveVisual should be Default");
		}

		[Fact]
		public void SetGrandparentUsingCtorAndMaintainExplicitParentValue()
		{
			IVisualController view = ImplicitDefaultView();
			var layout2 = new StackLayout { Visual = Forms.VisualMarker.Default, Children = { (View)view } };
			var layout = new StackLayout { Visual = Forms.VisualMarker.Material, Children = { layout2 } };

			var target = view.EffectiveVisual;

			Assert.True(((IVisualController)layout).EffectiveVisual.IsMaterial());

			Assert.True(((IVisualController)layout2).EffectiveVisual.IsDefault());

			Assert.True(!target.IsMaterial(), "EffectiveVisual should be Default");
			Assert.True(target.IsDefault(), "EffectiveVisual should be Default");
		}

		[Fact]
		public void SetParentAndGrandchildrenInheritValue()
		{
			var layout = ExplicitMaterialLayout();
			var layout2 = ImplicitDefaultLayout();
			IVisualController view = ImplicitDefaultView();

			AddImplicitToMaterial(layout, layout2);

			AddImplicitToMaterial(layout2, (View)view);

			var target = view.EffectiveVisual;

			Assert.True(target.IsMaterial(), "EffectiveVisual should be Material");
			Assert.True(!target.IsDefault(), "EffectiveVisual should be Material");
		}

		[Fact]
		public void SetParentAndContentAndGrandchildrenInheritValue()
		{
			var layout = ExplicitMaterialLayout();
			var layout2 = ImplicitDefaultScrollView();
			IVisualController view = ImplicitDefaultView();

			AddImplicitToMaterial(layout, layout2);

			AddImplicitToMaterialScrollView(layout2, (View)view);

			var target = view.EffectiveVisual;

			Assert.True(target.IsMaterial(), "EffectiveVisual should be Material");
			Assert.True(!target.IsDefault(), "EffectiveVisual should be Material");
		}


		[Fact]
		public void SetParentAndInheritExplicitParentValue()
		{
			var layout = ExplicitMaterialLayout();
			IVisualController view = ImplicitDefaultView();

			AddImplicitToMaterial(layout, (View)view);

			var target = view.EffectiveVisual;

			Assert.True(target.IsMaterial(), "EffectiveVisual should be Material");
			Assert.True(!target.IsDefault(), "EffectiveVisual should be Material");
		}

		[Fact]
		public void SetParentAndMaintainExplicitValue()
		{
			var layout = ExplicitMaterialLayout();
			IVisualController view = ExplicitDefaultView();

			AddExplicitDefaultToLayout(layout, (View)view);

			var target = view.EffectiveVisual;

			Assert.True(!target.IsMaterial(), "EffectiveVisual should be Default");
			Assert.True(target.IsDefault(), "EffectiveVisual should be Default");
			Assert.Equal(Forms.VisualMarker.Default, ((View)view).Visual);
		}

		[Fact]
		public void SetParentUsingCtorAndInheritParentValue()
		{
			IVisualController view = ImplicitDefaultView();
			var layout = new StackLayout { Visual = Forms.VisualMarker.Material, Children = { (View)view } };

			Assert.True(((IVisualController)layout).EffectiveVisual.IsMaterial());

			Assert.True(((View)view).Visual == Forms.VisualMarker.MatchParent);

			var target = view.EffectiveVisual;

			Assert.True(target.IsMaterial(), "EffectiveVisual should be Material");
			Assert.True(!target.IsDefault(), "EffectiveVisual should be Material");
			Assert.Equal(Forms.VisualMarker.MatchParent, ((View)view).Visual);
		}

		public override void Dispose()
		{
			base.Dispose();
			Device.PlatformServices = null;
		}

		static void AddExplicitLTRToScrollView(ScrollView parent, View child)
		{
			parent.Content = child;

			IVisualController controller = child;

			Assert.True(controller.EffectiveVisual.IsDefault(), "child view Visual should be Default");
			Assert.True(child.Visual == Forms.VisualMarker.Default, "child view Visual should be Default");
		}

		static void AddExplicitDefaultToLayout(StackLayout parent, View child)
		{
			parent.Children.Add(child);

			IVisualController controller = child;

			Assert.True(controller.EffectiveVisual.IsDefault(), "child view Visual should be Default");
			Assert.True(child.Visual == Forms.VisualMarker.Default, "child view Visual should be Default");
		}

		static void AddExplicitMaterialToScrollView(ScrollView parent, View child)
		{
			parent.Content = child;

			IVisualController controller = child;

			Assert.True(child.Visual == Forms.VisualMarker.Material, "child view Visual should be Material");
		}

		static void AddExplicitMaterialToLayout(StackLayout parent, View child)
		{
			parent.Children.Add(child);

			IVisualController controller = child;

			Assert.True(controller.EffectiveVisual.IsMaterial(), "child view EffectiveVisual should be Material");
			Assert.True(child.Visual == Forms.VisualMarker.Material, "child view Visual should be Material");
		}

		static void AddImplicitToDefault(StackLayout parent, View child)
		{
			parent.Children.Add(child);

			IVisualController controller = child;

			Assert.True(child.Visual == Forms.VisualMarker.MatchParent, "child view Visual should be MatchParent");
		}

		static void AddImplicitToMaterial(StackLayout parent, View child)
		{
			parent.Children.Add(child);

			IVisualController controller = child;


			Assert.True(controller.EffectiveVisual.IsMaterial(), "child view EffectiveVisual should be Material");
			Assert.True(child.Visual == Forms.VisualMarker.MatchParent, "child view Visual should be MatchParent");
		}

		static void AddImplicitToMaterialScrollView(ScrollView parent, View child)
		{
			parent.Content = child;

			IVisualController controller = child;

			Assert.True(controller.EffectiveVisual == Forms.VisualMarker.Material, "child view EffectiveVisual should be Material");
			Assert.True(child.Visual == Forms.VisualMarker.MatchParent, "child view Visual should be MatchParent");
		}

		static StackLayout ExplicitDefaultLayout()
		{
			var layout = new StackLayout { Visual = Forms.VisualMarker.Default };

			IVisualController controller = layout;

			Assert.True(controller.EffectiveVisual == Forms.VisualMarker.Default, "Explicit Default view EffectiveVisual should be Default");
			Assert.True(layout.Visual == Forms.VisualMarker.Default, "Explicit Default view Visual should be Default");
			return layout;
		}

		static View ExplicitDefaultView()
		{
			var view = new View { Visual = Forms.VisualMarker.Default };

			IVisualController controller = view;

			Assert.True(controller.EffectiveVisual.IsDefault(), "Explicit Default view EffectiveVisual should be Default");
			Assert.True(((View)view).Visual == Forms.VisualMarker.Default, "Explicit Default view Visual should be Default");

			return view;
		}

		static ScrollView ExplicitMaterialScrollView()
		{
			var layout = new ScrollView { Visual = Forms.VisualMarker.Material };

			IVisualController controller = layout;

			Assert.True(controller.EffectiveVisual == Forms.VisualMarker.Material, "Explicit RTL view EffectiveVisual should be Material");
			Assert.True(layout.Visual == Forms.VisualMarker.Material, "Explicit RTL view Visual should be Material");

			return layout;
		}

		static StackLayout ExplicitMaterialLayout()
		{
			var layout = new StackLayout { Visual = Forms.VisualMarker.Material };

			IVisualController controller = layout;

			Assert.True(controller.EffectiveVisual.IsMaterial(), "Explicit RTL view EffectiveVisual should be Material");
			Assert.True(layout.Visual == Forms.VisualMarker.Material, "Explicit RTL view Visual should be Material");

			return layout;
		}

		static ScrollView ImplicitDefaultScrollView()
		{
			var layout = new ScrollView();

			IVisualController controller = layout;

			Assert.True(controller.EffectiveVisual == Forms.VisualMarker.Default, "New view EffectiveVisual should be Default");
			Assert.True(layout.Visual == Forms.VisualMarker.MatchParent, "New view Visual should be MatchParent");

			return layout;
		}

		static StackLayout ImplicitDefaultLayout()
		{
			var layout = new StackLayout();

			IVisualController controller = layout;


			Assert.True(controller.EffectiveVisual == Forms.VisualMarker.Default, "New view EffectiveVisual should be Default");
			Assert.True(layout.Visual == Forms.VisualMarker.MatchParent, "New view Visual should be MatchParent");

			return layout;
		}

		static View ImplicitDefaultView()
		{
			var view = new View();

			IVisualController controller = view;

			Assert.True(((View)view).Visual == Forms.VisualMarker.MatchParent, "New view Visual should be MatchParent");

			return view;
		}

		class PropertyWatchingView : View
		{
			public int VisualPropertyChangedCount { get; private set; }

			protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
			{
				base.OnPropertyChanged(propertyName);

				if (propertyName == View.VisualProperty.PropertyName)
					VisualPropertyChangedCount++;
			}
		}
	}
}