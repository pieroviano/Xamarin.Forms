using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xamarin.Forms.Internals;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class VisualStateManagerTests
	{
		const string NormalStateName = "Normal";
		const string InvalidStateName = "Invalid";
		const string FocusedStateName = "Focused";
		const string DisabledStateName = "Disabled";
		const string CommonStatesName = "CommonStates";

		static VisualStateGroupList CreateTestStateGroups()
		{
			var stateGroups = new VisualStateGroupList();
			var visualStateGroup = new VisualStateGroup { Name = CommonStatesName };
			var normalState = new VisualState { Name = NormalStateName };
			var invalidState = new VisualState { Name = InvalidStateName };
			var focusedState = new VisualState { Name = FocusedStateName };
			var disabledState = new VisualState { Name = DisabledStateName };

			visualStateGroup.States.Add(normalState);
			visualStateGroup.States.Add(invalidState);
			visualStateGroup.States.Add(focusedState);
			visualStateGroup.States.Add(disabledState);

			stateGroups.Add(visualStateGroup);

			return stateGroups;
		}

		static VisualStateGroupList CreateStateGroupsWithoutNormalState()
		{
			var stateGroups = new VisualStateGroupList();
			var visualStateGroup = new VisualStateGroup { Name = CommonStatesName };
			var invalidState = new VisualState { Name = InvalidStateName };

			visualStateGroup.States.Add(invalidState);

			stateGroups.Add(visualStateGroup);

			return stateGroups;
		}

		[Fact]
		public void InitialStateIsNormalIfAvailable()
		{
			var label1 = new Label();

			VisualStateManager.SetVisualStateGroups(label1, CreateTestStateGroups());

			var groups1 = VisualStateManager.GetVisualStateGroups(label1);

			Assert.Equal(NormalStateName, groups1[0].CurrentState.Name);
		}

		[Fact]
		public void InitialStateIsNullIfNormalNotAvailable()
		{
			var label1 = new Label();

			VisualStateManager.SetVisualStateGroups(label1, CreateStateGroupsWithoutNormalState());

			var groups1 = VisualStateManager.GetVisualStateGroups(label1);

			Assert.Null(groups1[0].CurrentState);
		}

		[Fact]
		public void VisualElementsStateGroupsAreDistinct()
		{
			var label1 = new Label();
			var label2 = new Label();

			VisualStateManager.SetVisualStateGroups(label1, CreateTestStateGroups());
			VisualStateManager.SetVisualStateGroups(label2, CreateTestStateGroups());

			var groups1 = VisualStateManager.GetVisualStateGroups(label1);
			var groups2 = VisualStateManager.GetVisualStateGroups(label2);

			Assert.NotSame(groups1, groups2);

			Assert.Equal(NormalStateName, groups1[0].CurrentState.Name);
			Assert.Equal(NormalStateName, groups2[0].CurrentState.Name);

			VisualStateManager.GoToState(label1, InvalidStateName);

			Assert.Equal(InvalidStateName, groups1[0].CurrentState.Name);
			Assert.Equal(NormalStateName, groups2[0].CurrentState.Name);
		}

		[Fact]
		public void VisualStateGroupsFromSettersAreDistinct()
		{
			var x = new Setter();
			x.Property = VisualStateManager.VisualStateGroupsProperty;
			x.Value = CreateTestStateGroups();

			var label1 = new Label();
			var label2 = new Label();

			x.Apply(label1);
			x.Apply(label2);

			var groups1 = VisualStateManager.GetVisualStateGroups(label1);
			var groups2 = VisualStateManager.GetVisualStateGroups(label2);

			Assert.NotNull(groups1);
			Assert.NotNull(groups2);

			Assert.NotSame(groups1, groups2);

			Assert.Equal(NormalStateName, groups1[0].CurrentState.Name);
			Assert.Equal(NormalStateName, groups2[0].CurrentState.Name);

			VisualStateManager.GoToState(label1, InvalidStateName);

			Assert.Equal(InvalidStateName, groups1[0].CurrentState.Name);
			Assert.Equal(NormalStateName, groups2[0].CurrentState.Name);
		}

		[Fact]
		public void ElementsDoNotHaveVisualStateGroupsCollectionByDefault()
		{
			var label1 = new Label();
			Assert.False(label1.HasVisualStateGroups());
			var vsg = VisualStateManager.GetVisualStateGroups(label1);
			Assert.False(label1.HasVisualStateGroups());
			vsg.Add(new VisualStateGroup());
			Assert.True(label1.HasVisualStateGroups());
		}

		[Fact]
		public void StateNamesMustBeUniqueWithinGroup()
		{
			IList<VisualStateGroup> vsgs = CreateTestStateGroups();

			var duplicate = new VisualState { Name = NormalStateName };

			Assert.Throws<InvalidOperationException>(() => vsgs[0].States.Add(duplicate));
		}

		[Fact]
		public void StateNamesMustBeUniqueWithinGroupList()
		{
			IList<VisualStateGroup> vsgs = CreateTestStateGroups();

			// Create and add a second VisualStateGroup
			var secondGroup = new VisualStateGroup { Name = "Foo" };
			vsgs.Add(secondGroup);

			// Create a VisualState with the same name as one in another group in this list
			var duplicate = new VisualState { Name = NormalStateName };

			Assert.Throws<InvalidOperationException>(() => secondGroup.States.Add(duplicate));
		}

		[Fact]
		public void StateNamesMustBeUniqueWithinGroupListWhenAddingGroup()
		{
			IList<VisualStateGroup> vsgs = CreateTestStateGroups();

			// Create and add a second VisualStateGroup
			var secondGroup = new VisualStateGroup { Name = "Foo" };

			// Create a VisualState with the same name as one in another group in the list
			var duplicate = new VisualState { Name = NormalStateName };
			secondGroup.States.Add(duplicate);

			Assert.Throws<InvalidOperationException>(() => vsgs.Add(secondGroup));
		}

		[Fact]
		public void GroupNamesMustBeUniqueWithinGroupList()
		{
			IList<VisualStateGroup> vsgs = CreateTestStateGroups();
			var secondGroup = new VisualStateGroup { Name = CommonStatesName };

			Assert.Throws<InvalidOperationException>(() => vsgs.Add(secondGroup));
		}

		[Fact]
		public void StateNamesInGroupMayNotBeNull()
		{
			IList<VisualStateGroup> vsgs = CreateTestStateGroups();

			var nullStateName = new VisualState();

			Assert.Throws<InvalidOperationException>(() => vsgs[0].States.Add(nullStateName));
		}

		[Fact]
		public void StateNamesInGroupMayNotBeEmpty()
		{
			IList<VisualStateGroup> vsgs = CreateTestStateGroups();

			var emptyStateName = new VisualState { Name = "" };

			Assert.Throws<InvalidOperationException>(() => vsgs[0].States.Add(emptyStateName));
		}

		[Fact]
		public void VerifyVisualStateChanges()
		{
			var label1 = new Label();
			VisualStateManager.SetVisualStateGroups(label1, CreateTestStateGroups());

			var groups1 = VisualStateManager.GetVisualStateGroups(label1);
			Assert.Equal(NormalStateName, groups1[0].CurrentState.Name);

			label1.IsEnabled = false;

			groups1 = VisualStateManager.GetVisualStateGroups(label1);
			Assert.Equal(DisabledStateName, groups1[0].CurrentState.Name);


			label1.SetValue(VisualElement.IsFocusedPropertyKey, true);
			groups1 = VisualStateManager.GetVisualStateGroups(label1);
			Assert.Equal(DisabledStateName, groups1[0].CurrentState.Name);

			label1.IsEnabled = true;
			groups1 = VisualStateManager.GetVisualStateGroups(label1);
			Assert.Equal(FocusedStateName, groups1[0].CurrentState.Name);


			label1.SetValue(VisualElement.IsFocusedPropertyKey, false);
			groups1 = VisualStateManager.GetVisualStateGroups(label1);
			Assert.Equal(NormalStateName, groups1[0].CurrentState.Name);

		}

		[Fact]
		public void VisualElementGoesToCorrectStateWhenAvailable()
		{
			var label = new Label();
			double targetBottomMargin = 1.5;

			var group = new VisualStateGroup();
			var list = new VisualStateGroupList();

			var normalState = new VisualState { Name = NormalStateName };
			normalState.Setters.Add(new Setter { Property = View.MarginBottomProperty, Value = targetBottomMargin });

			list.Add(group);
			group.States.Add(normalState);

			VisualStateManager.SetVisualStateGroups(label, list);

			Assert.Equal(targetBottomMargin, label.Margin.Bottom);
		}

		[Fact]
		public void VisualElementGoesToCorrectStateWhenAvailableFromSetter()
		{
			double targetBottomMargin = 1.5;

			var group = new VisualStateGroup();
			var list = new VisualStateGroupList();

			var normalState = new VisualState { Name = NormalStateName };
			normalState.Setters.Add(new Setter { Property = View.MarginBottomProperty, Value = targetBottomMargin });

			var x = new Setter
			{
				Property = VisualStateManager.VisualStateGroupsProperty,
				Value = list
			};

			list.Add(group);
			group.States.Add(normalState);

			var label1 = new Label();
			var label2 = new Label();

			x.Apply(label1);
			x.Apply(label2);

			Assert.Equal(targetBottomMargin, label1.Margin.Bottom);
			Assert.Equal(targetBottomMargin, label2.Margin.Bottom);
		}

		[Fact]
		public void VisualElementGoesToCorrectStateWhenSetterHasTarget()
		{
			double defaultMargin = default(double);
			double targetMargin = 1.5;

			var label1 = new Label();
			var label2 = new Label();
			INameScope nameScope = new NameScope();
			NameScope.SetNameScope(label1, nameScope);
			nameScope.RegisterName("Label1", label1);
			NameScope.SetNameScope(label2, nameScope);
			nameScope.RegisterName("Label2", label2);

			var list = new VisualStateGroupList
			{
				new VisualStateGroup
				{
					States =
					{
						new VisualState
						{
							Name = NormalStateName,
							Setters =
							{
								new Setter { Property = View.MarginBottomProperty, Value = targetMargin },
								new Setter { TargetName = "Label2", Property = View.MarginTopProperty, Value = targetMargin }
							}
						}
					}
				}
			};

			VisualStateManager.SetVisualStateGroups(label1, list);

			Assert.Equal(defaultMargin, label1.Margin.Top);
			Assert.Equal(targetMargin, label1.Margin.Bottom);
			Assert.Equal(defaultMargin, label1.Margin.Left);

			Assert.Equal(targetMargin, label2.Margin.Top);
			Assert.Equal(defaultMargin, label2.Margin.Bottom);
		}

		[Fact]
		public void CanRemoveAStateAndAddANewStateWithTheSameName()
		{
			var stateGroups = new VisualStateGroupList();
			var visualStateGroup = new VisualStateGroup { Name = CommonStatesName };
			var normalState = new VisualState { Name = NormalStateName };
			var invalidState = new VisualState { Name = InvalidStateName };

			stateGroups.Add(visualStateGroup);
			visualStateGroup.States.Add(normalState);
			visualStateGroup.States.Add(invalidState);

			var name = visualStateGroup.States[0].Name;

			visualStateGroup.States.Remove(visualStateGroup.States[0]);

			visualStateGroup.States.Add(new VisualState { Name = name });
		}

		[Fact]
		public void CanRemoveAGroupAndAddANewGroupWithTheSameName()
		{
			var stateGroups = new VisualStateGroupList();
			var visualStateGroup = new VisualStateGroup { Name = CommonStatesName };
			var secondVisualStateGroup = new VisualStateGroup { Name = "Whatevs" };
			var normalState = new VisualState { Name = NormalStateName };
			var invalidState = new VisualState { Name = InvalidStateName };

			stateGroups.Add(visualStateGroup);
			visualStateGroup.States.Add(normalState);
			visualStateGroup.States.Add(invalidState);

			stateGroups.Add(secondVisualStateGroup);

			var name = stateGroups[0].Name;

			stateGroups.Remove(stateGroups[0]);

			stateGroups.Add(new VisualStateGroup { Name = name });
		}

		[Theory(Skip = "Benchmark, not a unit test: it measures elapsed milliseconds and reports them via Assert.Fail, so it has no pass condition and would be non-deterministic on CI hardware. Kept as a manually-runnable micro-benchmark.")]
		[InlineData(1, 10)]
		[InlineData(1, 10000)]
		[InlineData(10, 100)]
		[InlineData(10, 10000)]
		public void ValidatePerformance(int groups, int states)
		{
			IList<VisualStateGroup> vsgs = new VisualStateGroupList();

			var groupList = new List<VisualStateGroup>();

			for (int n = 0; n < groups; n++)
			{
				groupList.Add(new VisualStateGroup { Name = n.ToString() });
			}

			var watch = new Stopwatch();

			watch.Start();

			foreach (var group in groupList)
			{
				vsgs.Add(group);
			}

			watch.Stop();

			double iterations = states;
			var random = new Random();

			for (int n = 0; n < iterations; n++)
			{
				var state = new VisualState { Name = n.ToString() };
				var group = groupList[random.Next(0, groups - 1)];
				watch.Start();
				group.States.Add(state);
				watch.Stop();
			}

			var average = watch.ElapsedMilliseconds / iterations;

			Debug.WriteLine($">>>>> VisualStateManagerTests ValidatePerformance: {watch.ElapsedMilliseconds}ms over {iterations} iterations; average of {average}ms");

		}
	}
}