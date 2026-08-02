using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class TabIndexTests : BaseTestFixture
	{
		[Fact]
		public void GetTabIndexesOnParentPage_ImplicitZero()
		{
			var target = new StackLayout
			{
				Children = {
					new Label { TabIndex = 1 },
					new Label { TabIndex = 0 },
					new Label { TabIndex = 3 },
					new Label { TabIndex = 2 },
				}
			};

			var page = new ContentPage { Content = target };

			var tabIndexes = target.GetTabIndexesOnParentPage(out int _);

			//StackLayout is technically the first element with TabIndex 0.
			Assert.Equal(target, tabIndexes[0][0]);
		}

		class CustomGrid : Grid
		{
			public CustomGrid()
			{
				foreach (var i in Enumerable.Range(1, 7))
				{
					Children.Add(new CustomContent(i), i, 0);
				}
			}
		}

		class CustomContent : ContentView
		{
			public Frame Frame { get; set; } = new Frame();
			public CustomContent(int idx)
			{
				AutomationProperties.SetIsInAccessibleTree(this, false);

				IsTabStop = false;

				Frame.IsTabStop = true;
				Frame.TabIndex = idx;

				var stack = new StackLayout();
				var label = new Label() { Text = idx.ToString() };

				Frame.Content = label;
				stack.Children.Add(Frame);

				AutomationProperties.SetHelpText(Frame, idx.ToString());

				AutomationProperties.SetIsInAccessibleTree(label, false);
				AutomationProperties.SetIsInAccessibleTree(Frame, true);
				AutomationProperties.SetIsInAccessibleTree(stack, false);

				Content = stack;
			}
		}

		[Fact]
		public void GetTabIndexesOnParentPage_CompositeControls()
		{
			var label = new Label() { TabIndex = 1 };

			var composite = new CustomGrid();

			var label2 = new Label() { TabIndex = 10 };

			var timePicker = new TimePicker() { TabIndex = 11 };

			var label3 = new Label() { TabIndex = 12 };

			var timePicker2 = new TimePicker() { TabIndex = 13 };

			var stack = new StackLayout
			{
				Children = {
					label,
					composite,
					label2,
					timePicker,
					label3,
					timePicker2
				}
			};

			var scroll = new ScrollView() { Content = stack };

			var page = new ContentPage { Content = scroll };

			SortedDictionary<int, List<ITabStopElement>> tabIndexes = null;
			foreach (var child in page.LogicalChildren)
			{
				if (!(child is VisualElement ve))
					continue;

				tabIndexes = ve.GetSortedTabIndexesOnParentPage();
				break;
			}

			Assert.True(tabIndexes.Any());

			Assert.Equal(3, tabIndexes[0].Count);
			Assert.Equal(tabIndexes[0][0], scroll);
			Assert.Equal(tabIndexes[0][1], stack);
			Assert.Equal(tabIndexes[0][2], composite);

			Assert.Equal(2, tabIndexes[1].Count);
			Assert.Equal(tabIndexes[1][0], label);
			Assert.IsAssignableFrom(typeof(Frame), tabIndexes[1][1]);
			Assert.Equal("1", AutomationProperties.GetHelpText(((Frame)tabIndexes[1][1])));

			Assert.Equal(1, tabIndexes[2].Count);
			Assert.IsAssignableFrom(typeof(Frame), tabIndexes[2][0]);
			Assert.Equal("2", AutomationProperties.GetHelpText(((Frame)tabIndexes[2][0])));

			Assert.Equal(1, tabIndexes[3].Count);
			Assert.IsAssignableFrom(typeof(Frame), tabIndexes[3][0]);
			Assert.Equal("3", AutomationProperties.GetHelpText(((Frame)tabIndexes[3][0])));

			Assert.Equal(1, tabIndexes[4].Count);
			Assert.IsAssignableFrom(typeof(Frame), tabIndexes[4][0]);
			Assert.Equal("4", AutomationProperties.GetHelpText(((Frame)tabIndexes[4][0])));

			Assert.Equal(1, tabIndexes[5].Count);
			Assert.IsAssignableFrom(typeof(Frame), tabIndexes[5][0]);
			Assert.Equal("5", AutomationProperties.GetHelpText(((Frame)tabIndexes[5][0])));

			Assert.Equal(1, tabIndexes[6].Count);
			Assert.IsAssignableFrom(typeof(Frame), tabIndexes[6][0]);
			Assert.Equal("6", AutomationProperties.GetHelpText(((Frame)tabIndexes[6][0])));

			Assert.Equal(1, tabIndexes[7].Count);
			Assert.IsAssignableFrom(typeof(Frame), tabIndexes[7][0]);
			Assert.Equal("7", AutomationProperties.GetHelpText(((Frame)tabIndexes[7][0])));

			Assert.False(tabIndexes.ContainsKey(8), "Something unexpected in group 8");
			Assert.False(tabIndexes.ContainsKey(9), "Something unexpected in group 9");

			Assert.Equal(1, tabIndexes[10].Count);
			Assert.Equal(tabIndexes[10][0], label2);
			Assert.Equal(1, tabIndexes[11].Count);
			Assert.Equal(tabIndexes[11][0], timePicker);
			Assert.Equal(1, tabIndexes[12].Count);
			Assert.Equal(tabIndexes[12][0], label3);
			Assert.Equal(1, tabIndexes[13].Count);
			Assert.Equal(tabIndexes[13][0], timePicker2);
		}

		[Fact]
		public void GetTabIndexesOnParentPage_GetTabIndexForLayoutChildren()
		{
			var target = new Label { TabIndex = 0 };
			var stack = new StackLayout
			{
				IsTabStop = false,
				Children = {
					new Label { TabIndex = 1 },
					target,
					new Label { TabIndex = 3 },
					new Label { TabIndex = 2 },
				}
			};

			var page = new ContentPage { Content = stack };

			var tabIndexes = stack.GetTabIndexesOnParentPage(out int _);

			Assert.True(tabIndexes.Any());
			Assert.Equal(target, tabIndexes[0][0]);
		}

		[Fact]
		public void GetTabIndexesOnParentPage_MasterPage()
		{
			var target = new Label { TabIndex = 0 };
			var stack = new StackLayout
			{
				IsTabStop = false,
				Children = {
					new Label { TabIndex = 1 },
					target,
					new Label { TabIndex = 3 },
					new Label { TabIndex = 2 },
				}
			};

			var stack2 = new StackLayout
			{
				IsTabStop = false,
				Children = {
					new Label { TabIndex = 1 },
					new Label { TabIndex = 3 },
					new Label { TabIndex = 2 },
				}
			};

			var masterPage = new ContentPage { Content = stack, Title = "master" };

			var detailPage = new ContentPage { Content = stack2 };

			var fp = new FlyoutPage { Flyout = masterPage, Detail = detailPage };

			var tabIndexes = stack.GetTabIndexesOnParentPage(out int _);

			Assert.True(tabIndexes.Any());
			Assert.Equal(target, tabIndexes[0][0]);
		}

		[Fact]
		public void GetTabIndexesOnParentPage_DetailPage()
		{
			var target = new Label { TabIndex = 0 };
			var stack = new StackLayout
			{
				IsTabStop = false,
				Children = {
					new Label { TabIndex = 1 },
					target,
					new Label { TabIndex = 3 },
					new Label { TabIndex = 2 },
				}
			};

			var stack2 = new StackLayout
			{
				IsTabStop = false,
				Children = {
					new Label { TabIndex = 1 },
					new Label { TabIndex = 3 },
					new Label { TabIndex = 2 },
				}
			};

			var masterPage = new ContentPage { Content = stack2, Title = "master" };

			var detailPage = new ContentPage { Content = stack };

			var fp = new FlyoutPage { Flyout = masterPage, Detail = detailPage };

			var tabIndexes = stack.GetTabIndexesOnParentPage(out int _);

			Assert.True(tabIndexes.Any());
			Assert.Equal(target, tabIndexes[0][0]);
		}

		[Fact]
		public void GetTabIndexesOnParentPage_NoPageZeroCount()
		{
			var element = new Label { TabIndex = 0 };

			var _ = element.GetTabIndexesOnParentPage(out int target);

			Assert.Equal(0, target);
			Assert.Equal(new Dictionary<int, List<ITabStopElement>>(), _);
		}

		[Fact]
		public void GetTabIndexesOnParentPage_ExplicitZero()
		{
			Label target = new Label { TabIndex = 0 };
			var stackLayout = new StackLayout
			{
				Children = {
					new Label { TabIndex = 1 },
					target,
					new Label { TabIndex = 3 },
					new Label { TabIndex = 2 },
				}
			};

			var page = new ContentPage { Content = stackLayout };

			var tabIndexes = stackLayout.GetTabIndexesOnParentPage(out int _);

			Assert.Equal(target, tabIndexes[0][1]);
		}

		[Fact]
		public void GetTabIndexesOnParentPage_NegativeTabIndex()
		{
			Label target = new Label { TabIndex = -1 };
			var stackLayout = new StackLayout
			{
				Children = {
					new Label { TabIndex = 1 },
					target,
					new Label { TabIndex = 3 },
					new Label { TabIndex = 2 },
				}
			};

			var page = new ContentPage { Content = stackLayout };

			var tabIndexes = stackLayout.GetTabIndexesOnParentPage(out int _);

			Assert.Equal(target, tabIndexes[-1][0]);
		}

		[Fact]
		public void FindNextElement_Forward_NextTabIndex()
		{
			Label target = new Label { TabIndex = 1 };
			Label nextElement = new Label { TabIndex = 2 };
			var stackLayout = new StackLayout
			{
				Children = {
					new Label { TabIndex = 1 },
					target,
					new Label { TabIndex = 3 },
					nextElement,
				}
			};

			var page = new ContentPage { Content = stackLayout };

			var tabIndexes = stackLayout.GetTabIndexesOnParentPage(out int maxAttempts);

			int _ = target.TabIndex;

			var found = target.FindNextElement(true, tabIndexes, ref _);

			Assert.Equal(nextElement, found);
		}

		[Fact]
		public void FindNextElement_Forward_DeclarationOrder()
		{
			Label target = new Label { TabIndex = 1 };
			Label nextElement = new Label { TabIndex = 2 };
			var stackLayout = new StackLayout
			{
				Children = {
					new Label { TabIndex = 1 },
					target,
					nextElement,
					new Label { TabIndex = 2 },
				}
			};

			var page = new ContentPage { Content = stackLayout };

			var tabIndexes = stackLayout.GetTabIndexesOnParentPage(out int maxAttempts);

			int _ = target.TabIndex;

			var found = target.FindNextElement(true, tabIndexes, ref _);

			Assert.Equal(nextElement, found);
		}

		[Fact]
		public void FindNextElement_Forward_TabIndex()
		{
			Label target = new Label { TabIndex = 1 };
			Label nextElement = new Label { TabIndex = 2 };
			var stackLayout = new StackLayout
			{
				Children = {
					new Label { TabIndex = 1 },
					target,
					nextElement,
					new Label { TabIndex = 2 },
				}
			};

			var page = new ContentPage { Content = stackLayout };

			var tabIndexes = stackLayout.GetTabIndexesOnParentPage(out int maxAttempts);

			int tabIndex = target.TabIndex;

			var found = target.FindNextElement(true, tabIndexes, ref tabIndex);

			Assert.Equal(2, tabIndex);
		}

		[Fact]
		public void FindNextElement_Backward_NextTabIndex()
		{
			Label target = new Label { TabIndex = 2 };
			Label nextElement = new Label { TabIndex = 1 };
			var stackLayout = new StackLayout
			{
				Children = {
					new Label { TabIndex = 3 },
					target,
					new Label { TabIndex = 3 },
					nextElement,
				}
			};

			var page = new ContentPage { Content = stackLayout };

			var tabIndexes = stackLayout.GetTabIndexesOnParentPage(out int maxAttempts);

			int _ = target.TabIndex;

			var found = target.FindNextElement(false, tabIndexes, ref _);

			Assert.Equal(nextElement, found);
		}

		[Fact]
		public void FindNextElement_Backward_DeclarationOrder()
		{
			Label target = new Label { TabIndex = 2 };
			Label nextElement = new Label { TabIndex = 1 };
			var stackLayout = new StackLayout
			{
				Children = {
					new Label { TabIndex = 3 },
					target,
					nextElement,
					new Label { TabIndex = 1 },
				}
			};

			var page = new ContentPage { Content = stackLayout };

			var tabIndexes = stackLayout.GetTabIndexesOnParentPage(out int _);

			int _ = target.TabIndex;

			var found = target.FindNextElement(false, tabIndexes, ref _);

			Assert.Equal(nextElement, found);
		}

		[Fact]
		public void FindNextElement_Backward_TabIndex()
		{
			Label target = new Label { TabIndex = 2 };
			Label nextElement = new Label { TabIndex = 1 };
			var stackLayout = new StackLayout
			{
				Children = {
					new Label { TabIndex = 3 },
					target,
					nextElement,
					new Label { TabIndex = 2 },
				}
			};

			var page = new ContentPage { Content = stackLayout };

			var tabIndexes = stackLayout.GetTabIndexesOnParentPage(out int maxAttempts);

			int tabIndex = target.TabIndex;

			var found = target.FindNextElement(false, tabIndexes, ref tabIndex);

			Assert.Equal(1, tabIndex);
		}
	}
}