using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	[TypeConverter(typeof(ToBarConverter))]
	internal class Bar
	{

	}

	internal class Baz
	{

	}

	internal class MockBindable
		: VisualElement
	{
		public static readonly BindableProperty TextProperty = BindableProperty.Create<MockBindable, string>(
			b => b.Text, "default", BindingMode.TwoWay);

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public string Foo { get; set; }

		public int TargetInt { get; set; }

		public static readonly BindableProperty BarProperty =
			BindableProperty.Create<MockBindable, Bar>(w => w.Bar, default(Bar));

		public Bar Bar
		{
			get { return (Bar)GetValue(BarProperty); }
			set { SetValue(BarProperty, value); }
		}

		public static readonly BindableProperty BazProperty =
			BindableProperty.Create<MockBindable, Baz>(w => w.Baz, default(Baz));

		[TypeConverter(typeof(ToBazConverter))]
		public Baz Baz
		{
			get { return (Baz)GetValue(BazProperty); }
			set { SetValue(BazProperty, value); }
		}

		public static readonly BindableProperty QuxProperty =
			BindableProperty.Create<MockBindable, Baz>(w => w.Qux, default(Baz));

		public Baz Qux
		{
			get { return (Baz)GetValue(QuxProperty); }
			set { SetValue(QuxProperty, value); }
		}
	}

	internal static class AttachedPropertyHolder
	{
		public static readonly BindableProperty AttachedCollectionProperty =
			BindableProperty.CreateAttached("AttachedCollection", typeof(IList<string>), typeof(AttachedPropertyHolder), new List<string>());

		public static IList<string> GetAttachedCollection(BindableObject bindable) => (IList<string>)bindable.GetValue(AttachedCollectionProperty);
		public static void SetAttachedCollection(BindableObject bindable, IList<string> value) => bindable.SetValue(AttachedCollectionProperty, value);
	}

	internal class ToBarConverter : TypeConverter
	{
		public override object ConvertFrom(System.Globalization.CultureInfo culture, object value)
		{
			return new Bar();
		}
	}

	internal class ToBazConverter : TypeConverter
	{
		public override object ConvertFrom(System.Globalization.CultureInfo culture, object value)
		{
			return new Baz();
		}
	}

	public class BindableObjectUnitTests : BaseTestFixture
	{
		public BindableObjectUnitTests()
		{
			Device.PlatformServices = new MockPlatformServices();
		}

		public override void Dispose()
		{
			base.Dispose();
			Device.PlatformServices = null;
		}

		[Fact]
		public void BindingContext()
		{
			var mock = new MockBindable();
			Assert.Null(mock.BindingContext);

			object obj = new object();
			mock.BindingContext = obj;
			Assert.Same(obj, mock.BindingContext);
		}

		[Fact]
		public void BindingContextChangedEvent()
		{
			var mock = new MockBindable();

			// NUnit's return aborted the test as passed from inside the handler, so
			// the trailing Assert.Fail was only reached when the event never fired. xUnit
			// has no control-flow "pass", so the same logic becomes a flag.
			var fired = false;
			mock.BindingContextChanged += (sender, args) => fired = true;

			mock.BindingContext = new object();

			Assert.True(fired, "The BindingContextChanged event was not fired.");
		}

		[Fact]
		public void BindingContextChangedOnce()
		{
			var count = 0;

			var mock = new MockBindable();
			mock.BindingContextChanged += (sender, args) => ++count;

			mock.BindingContext = new object();
			Assert.Equal(count, 1);
		}

		class MockVMEquals
		{
			public string Key { get; set; }
			public string Text { get; set; }
			public override bool Equals(object obj)
			{
				var other = obj as MockVMEquals;
				if (other == null)
					return false;
				return Key == other.Key;
			}

			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		[Fact]
		//https://bugzilla.xamarin.com/show_bug.cgi?id=59507
		public void BindingContextChangedCompareReferences()
		{
			var mock = new MockBindable();
			mock.BindingContext = new MockVMEquals { Key = "Foo", Text = "Foo" };

			// See BindingContextChangedEvent: NUnit's return becomes a flag.
			var fired = false;
			mock.BindingContextChanged += (sender, args) => fired = true;

			mock.BindingContext = new MockVMEquals { Key = "Foo", Text = "Bar" };

			Assert.True(fired, "The BindingContextChanged event was not fired.");
		}

		[Fact]
		public void ParentAndChildBindingContextChanged()
		{
			int parentCount = 0, childCount = 0;

			var didNotChange = " BindingContext did not change.";
			var changedMoreThanOnce = " BindingContext changed more than once.";
			var changedWhenNoChange = " BindingContext was changed when there was no change in context.";

			var parent = new MockBindable();
			parent.BindingContextChanged += (sender, args) => { ++parentCount; };

			var child = new MockBindable();
			child.BindingContextChanged += (sender, args) => { ++childCount; };

			child.Parent = parent;
			Assert.Equal(parentCount, 0);
			Assert.Equal(childCount, 0);

			child.BindingContext = new object(); // set manually
			Assert.True(childCount >= 1);
			Assert.Equal(childCount, 1);
			Assert.Equal(parentCount, 0);

			parent.BindingContext = new object();
			Assert.True(parentCount >= 1);
			Assert.Equal(parentCount, 1);
			Assert.Equal(childCount, 1);

			child.BindingContext = new object();
			Assert.True(childCount >= 2);
			Assert.Equal(childCount, 2);
			Assert.Equal(parentCount, 1);
		}

		[Fact]
		public void ParentSetOnNullChildBindingContext()
		{
			var parent = new MockBindable();

			var child = new MockBindable();
			child.BindingContextChanged += (sender, args) => { Assert.Fail("Child BindingContext was changed when there was no change in context."); };

			child.Parent = parent; // this should not trigger binding context change on child since there is no change
			parent.BindingContext = new object();
			parent.BindingContext = new object();
		}

		[Fact]
		public void ParentSetOnNonNullChildBindingContext()
		{
			var count = 0;

			var parent = new MockBindable();
			parent.BindingContextChanged += (sender, args) => { ++count; };

			var child = new MockBindable();
			child.BindingContextChanged += (sender, args) => { ++count; };

			child.BindingContext = new object(); // set manually
			Assert.Equal(count, 1);

			child.Parent = parent; // this should not trigger binding context change because child binding was set manually
			Assert.Equal(count, 1);

			parent.BindingContext = new object();
			Assert.Equal(count, 2);

			child.BindingContext = new object();
			Assert.Equal(count, 3);
		}

		[Fact]
		[Trait("Description", "When the BindingContext changes, any bindings should be immediately applied.")]
		public void BindingContextChangedBindingsApplied()
		{
			var mock = new MockBindable();
			mock.SetBinding(MockBindable.TextProperty, new Binding("."));
			mock.BindingContext = "Test";

			Assert.Equal("Test", mock.GetValue(MockBindable.TextProperty));

			mock.BindingContext = "Testing";

			Assert.Equal("Testing", mock.GetValue(MockBindable.TextProperty));
		}

		[Fact]
		[Trait("Description", "When the BindingContext changes, the new context needs to listen for updates.")]
		public void BindingContextChangedBindingsListening()
		{
			var mock = new MockBindable();
			mock.SetBinding(MockBindable.TextProperty, new Binding("Text"));

			var vm = new MockViewModel();
			mock.BindingContext = vm;

			mock.BindingContext = (vm = new MockViewModel());

			vm.Text = "test";

			Assert.Equal("test", mock.GetValue(MockBindable.TextProperty));
		}

		[Fact]
		[Trait("Description", "When an INPC implementer is unset as the BindingContext, its changes shouldn't be listened to any further.")]
		public void BindingContextUnsetStopsListening()
		{
			var mock = new MockBindable();
			mock.SetBinding(MockBindable.TextProperty, new Binding("Text"));

			var vm = new MockViewModel();
			mock.BindingContext = vm;

			mock.BindingContext = null;

			vm.Text = "test";

			Assert.Null(mock.GetValue(Entry.TextProperty));
		}

		[Fact]
		public void PropertyChanging()
		{
			var mock = new MockBindable();
			mock.PropertyChanging += (sender, args) =>
			{
				Assert.Equal(MockBindable.TextProperty.PropertyName, args.PropertyName);
				Assert.Equal(MockBindable.TextProperty.DefaultValue, mock.GetValue(MockBindable.TextProperty));
				return;
			};

			mock.SetValue(MockBindable.TextProperty, "foo");

			Assert.Fail("The PropertyChanging event was not fired.");
		}

		[Fact]
		public void PropertyChangingSameValue()
		{
			const string value = "foo";

			var mock = new MockBindable();
			mock.SetValue(MockBindable.TextProperty, value);
			mock.PropertyChanging += (s, e) => Assert.Fail();

			mock.SetValue(MockBindable.TextProperty, value);

			return;
		}

		[Fact]
		public void PropertyChangingDefaultValue()
		{
			var prop = BindableProperty.Create<MockBindable, string>(w => w.Foo, "DefaultValue");

			var mock = new MockBindable();
			mock.PropertyChanging += (s, e) => Assert.Fail();
			mock.SetValue(prop, prop.DefaultValue);

			return;
		}

		[Fact]
		public void PropertyChanged()
		{
			const string value = "foo";

			var mock = new MockBindable();
			mock.PropertyChanged += (sender, args) =>
			{
				Assert.Equal(MockBindable.TextProperty.PropertyName, args.PropertyName);
				Assert.Equal(value, mock.GetValue(MockBindable.TextProperty));
				return;
			};

			mock.SetValue(MockBindable.TextProperty, value);

			Assert.Fail("The PropertyChanged event was not fired.");
		}

		[Fact]
		public void PropertyChangedSameValue()
		{
			const string value = "foo";

			var mock = new MockBindable();
			mock.SetValue(MockBindable.TextProperty, value);
			mock.PropertyChanged += (s, e) => Assert.Fail();

			mock.SetValue(MockBindable.TextProperty, value);

			return;
		}

		[Fact]
		public void PropertyChangedDefaultValue()
		{
			var prop = BindableProperty.Create<MockBindable, string>(w => w.Foo, "DefaultValue");

			var mock = new MockBindable();
			mock.PropertyChanged += (s, e) => Assert.Fail();

			mock.SetValue(prop, prop.DefaultValue);

			return;
		}

		[Fact]
		public void GetSetValue()
		{
			const string value = "foo";
			var mock = new MockBindable();
			mock.SetValue(MockBindable.TextProperty, value);

			Assert.Equal(value, mock.GetValue(MockBindable.TextProperty));
		}

		[Fact]
		public void GetValueDefault()
		{
			var nulldefault = BindableProperty.Create<MockBindable, string>(w => w.Foo, null);
			TestGetValueDefault(nulldefault);

			var foodefault = BindableProperty.Create<MockBindable, string>(w => w.Foo, "Foo");
			TestGetValueDefault(foodefault);
		}

		void TestGetValueDefault(BindableProperty property)
		{
			var mock = new MockBindable();
			object value = mock.GetValue(property);
			Assert.Equal(property.DefaultValue, value);
		}

		[Fact]
		public void SetValueInvalid()
		{
			var mock = new MockBindable();
			Assert.Throws<ArgumentNullException>(() => mock.SetValue((BindableProperty)null, "null"));
		}

		[Fact]
		public void GetValueInvalid()
		{
			var mock = new MockBindable();
			Assert.Throws<ArgumentNullException>(() => mock.GetValue(null));
		}

		[Fact]
		public void ClearValueInvalid()
		{
			var mock = new MockBindable();
			Assert.Throws<ArgumentNullException>(() => mock.ClearValue((BindableProperty)null));
		}

		[Fact]
		public void ClearValue()
		{
			const string value = "foo";
			var mock = new MockBindable();
			mock.SetValue(MockBindable.TextProperty, value);
			Assert.Equal(value, mock.GetValue(MockBindable.TextProperty));

			mock.ClearValue(MockBindable.TextProperty);
			TestGetValueDefault(MockBindable.TextProperty);
		}

		[Fact]
		public void ClearValueTriggersINPC()
		{
			var bindable = new MockBindable();
			bool changingfired = false;
			bool changedfired = false;
			bool changingdelegatefired = false;
			bool changeddelegatefired = false;
			var property = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable), "foo",
				propertyChanged: (b, o, n) => changeddelegatefired = true,
				propertyChanging: (b, o, n) => changingdelegatefired = true
			);
			bindable.PropertyChanged += (sender, e) => { changedfired |= e.PropertyName == "Foo"; };
			bindable.PropertyChanging += (sender, e) => { changingfired |= e.PropertyName == "Foo"; };

			bindable.SetValue(property, "foobar");
			changingfired = changedfired = changeddelegatefired = changingdelegatefired = false;

			bindable.ClearValue(property);
			Assert.True(changingfired);
			Assert.True(changedfired);
			Assert.True(changingdelegatefired);
			Assert.True(changeddelegatefired);
		}

		[Fact]
		public void ClearValueDoesNotTriggersINPCOnSameValues()
		{
			var bindable = new MockBindable();
			bool changingfired = false;
			bool changedfired = false;
			bool changingdelegatefired = false;
			bool changeddelegatefired = false;
			var property = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable), "foo",
				propertyChanged: (b, o, n) => changeddelegatefired = true,
				propertyChanging: (b, o, n) => changingdelegatefired = true
			);
			bindable.PropertyChanged += (sender, e) => { changedfired |= e.PropertyName == "Foo"; };
			bindable.PropertyChanging += (sender, e) => { changingfired |= e.PropertyName == "Foo"; };

			bindable.SetValue(property, "foobar");
			bindable.SetValue(property, "foo");
			changingfired = changedfired = changeddelegatefired = changingdelegatefired = false;

			bindable.ClearValue(property);
			Assert.False(changingfired);
			Assert.False(changedfired);
			Assert.False(changingdelegatefired);
			Assert.False(changeddelegatefired);
		}

		[Fact]
		public void SetBindingInvalid()
		{
			var mock = new MockBindable();
			Assert.Throws<ArgumentNullException>(() => mock.SetBinding(null, new Binding(".")));
			Assert.Throws<ArgumentNullException>(() => mock.SetBinding(MockBindable.TextProperty, null));
		}

		[Fact]
		public void RemoveUnaddedBinding()
		{
			var mock = new MockBindable();
			AssertEx.DoesNotThrow(() => mock.RemoveBinding(MockBindable.TextProperty));
		}

		[Fact]
		public void RemoveBindingInvalid()
		{
			var mock = new MockBindable();
			Assert.Throws<ArgumentNullException>(() => mock.RemoveBinding(null));
		}

		[Fact]
		public void RemovedBindingDoesNotUpdate()
		{
			const string newvalue = "New Value";
			var viewmodel = new MockViewModel
			{
				Text = "Foo"
			};

			var binding = new Binding("Text");

			var bindable = new MockBindable();
			bindable.BindingContext = viewmodel;
			bindable.SetBinding(MockBindable.TextProperty, binding);

			string original = (string)bindable.GetValue(MockBindable.TextProperty);

			bindable.RemoveBinding(MockBindable.TextProperty);

			viewmodel.Text = newvalue;
			Assert.Equal(original, bindable.GetValue(MockBindable.TextProperty));
		}

		[Fact]
		public void CoerceValue()
		{
			var property = BindableProperty.Create<MockBindable, string>(w => w.Foo, null,
				coerceValue: (bo, o) => o.ToUpper());

			const string value = "value";
			var mock = new MockBindable();
			mock.SetValue(property, value);
			Assert.Equal(value.ToUpper(), mock.GetValue(property));
		}

		[Fact]
		public void ValidateValue()
		{
			var property = BindableProperty.Create<MockBindable, string>(w => w.Foo, null,
				validateValue: (b, v) => false);

			var mock = new MockBindable();
			Assert.Throws<ArgumentException>(() => mock.SetValue(property, null));
		}

		[Fact]
		public void BindablePropertyChanged()
		{
			bool changed = false;

			string oldv = "bar";
			string newv = "foo";

			var property = BindableProperty.Create<MockBindable, string>(w => w.Foo, oldv,
				propertyChanged: (b, ov, nv) =>
				{
					Assert.Same(oldv, ov);
					Assert.Same(newv, nv);
					changed = true;
				});

			var mock = new MockBindable();
			mock.SetValue(property, newv);

			Assert.True(changed, "PropertyChanged was not called");
		}

		[Fact]
		public void RecursiveChange()
		{
			bool changedA1 = false, changedA2 = false, changedB1 = false, changedB2 = false;

			var mock = new MockBindable();
			mock.PropertyChanged += (sender, args) =>
			{
				if (!changedA1)
				{
					Assert.Equal("1", mock.GetValue(MockBindable.TextProperty));
					Assert.False(changedA2);
					Assert.False(changedB1);
					Assert.False(changedB2);
					mock.SetValue(MockBindable.TextProperty, "2");
					changedA1 = true;
				}
				else
				{
					Assert.Equal("2", mock.GetValue(MockBindable.TextProperty));
					Assert.False(changedA2);
					Assert.True(changedB1);
					Assert.False(changedB2);
					changedA2 = true;
				}
			};
			mock.PropertyChanged += (sender, args) =>
			{
				if (!changedB1)
				{
					Assert.Equal("1", mock.GetValue(MockBindable.TextProperty));
					Assert.True(changedA1);
					Assert.False(changedA2);
					Assert.False(changedB2);
					changedB1 = true;
				}
				else
				{
					Assert.Equal("2", mock.GetValue(MockBindable.TextProperty));
					Assert.True(changedA1);
					Assert.True(changedA2);
					Assert.False(changedB2);
					changedB2 = true;
				}
			};
			mock.SetValue(MockBindable.TextProperty, "1");
			Assert.Equal("2", mock.GetValue(MockBindable.TextProperty));
			Assert.True(changedA1);
			Assert.True(changedA2);
			Assert.True(changedB1);
			Assert.True(changedB2);
		}

		[Fact]
		public void RaiseOnEqual()
		{
			string foo = "foo";
			var mock = new MockBindable();
			mock.SetValue(MockBindable.TextProperty, foo);

			bool changing = false;
			mock.PropertyChanging += (o, e) =>
			{
				Assert.Equal(MockBindable.TextProperty.PropertyName, e.PropertyName);
				changing = true;
			};

			bool changed = true;
			mock.PropertyChanged += (o, e) =>
			{
				Assert.Equal(MockBindable.TextProperty.PropertyName, e.PropertyName);
				changed = true;
			};

			mock.SetValueCore(MockBindable.TextProperty, foo,
				SetValueFlags.ClearOneWayBindings | SetValueFlags.ClearDynamicResource | SetValueFlags.RaiseOnEqual);

			Assert.True(changing, "PropertyChanging event did not fire");
			Assert.True(changed, "PropertyChanged event did not fire");
		}

		[Fact]
		public void BindingContextGetter()
		{
			var label = new Label();
			var view = new StackLayout { Children = { label } };

			view.BindingContext = new { item0 = "Foo", item1 = "Bar" };
			label.SetBinding(BindableObject.BindingContextProperty, "item0");
			label.SetBinding(Label.TextProperty, Binding.SelfPath);

			Assert.Same(label.BindingContext, label.GetValue(BindableObject.BindingContextProperty));
		}

		[Fact]
		public void BoundBindingContextUpdate()
		{
			var label = new Label();
			var view = new StackLayout { Children = { label } };
			var vm = new MockViewModel { Text = "FooBar" };

			view.BindingContext = vm;
			label.SetBinding(BindableObject.BindingContextProperty, "Text");
			label.SetBinding(Label.TextProperty, Binding.SelfPath);

			Assert.Equal("FooBar", label.BindingContext);

			vm.Text = "Baz";
			Assert.Equal("Baz", label.BindingContext);
		}

		[Fact]
		public void BoundBindingContextChange()
		{
			var label = new Label();
			var view = new StackLayout { Children = { label } };

			view.BindingContext = new MockViewModel { Text = "FooBar" };
			;
			label.SetBinding(BindableObject.BindingContextProperty, "Text");
			label.SetBinding(Label.TextProperty, Binding.SelfPath);

			Assert.Equal("FooBar", label.BindingContext);

			view.BindingContext = new MockViewModel { Text = "Baz" };
			;
			Assert.Equal("Baz", label.BindingContext);
		}

		[Fact]
		public void TestReadOnly()
		{
			var bindablePropertyKey = BindableProperty.CreateReadOnly<MockBindable, string>(w => w.Foo, "DefaultValue");
			var bindableProperty = bindablePropertyKey.BindableProperty;

			var bindable = new MockBindable();
			Assert.Equal("DefaultValue", bindable.GetValue(bindableProperty));

			bindable.SetValue(bindablePropertyKey, "Bar");
			Assert.Equal("Bar", bindable.GetValue(bindableProperty));

			Assert.Throws<InvalidOperationException>(() => bindable.SetValue(bindableProperty, "Baz"));
			Assert.Equal("Bar", bindable.GetValue(bindableProperty));

			Assert.Throws<InvalidOperationException>(() => bindable.ClearValue(bindableProperty));

			bindable.ClearValue(bindablePropertyKey);
			Assert.Equal("DefaultValue", bindable.GetValue(bindableProperty));
		}

		[Fact]
		public void TestBindingTwoWayOnReadOnly()
		{
			var bindablePropertyKey = BindableProperty.CreateReadOnly<MockBindable, string>(w => w.Foo, "DefaultValue", BindingMode.OneWayToSource);
			var bindableProperty = bindablePropertyKey.BindableProperty;

			var bindable = new MockBindable();
			var vm = new MockViewModel();

			bindable.SetBinding(bindableProperty, new Binding("Text", BindingMode.TwoWay));
			AssertEx.DoesNotThrow(() => bindable.BindingContext = vm);

			Assert.Equal("DefaultValue", bindable.GetValue(bindableProperty));
		}

		[Fact]
		public void DefaultValueCreator()
		{
			int invoked = 0;
			object defaultValue = new object();
			var bindableProperty = BindableProperty.Create("Foo", typeof(object), typeof(MockBindable), defaultValue, defaultValueCreator: o =>
			{
				invoked++;
				return new object();
			});
			var bindable = new MockBindable();

			Assert.Same(defaultValue, bindableProperty.DefaultValue);
			var newvalue = bindable.GetValue(bindableProperty);
			Assert.NotSame(defaultValue, newvalue);
			Assert.NotNull(newvalue);
			Assert.Equal(1, invoked);
		}

		[Fact]
		public void DefaultValueCreatorIsInvokedOnlyAtFirstTime()
		{
			int invoked = 0;
			var bindableProperty = BindableProperty.Create("Foo", typeof(object), typeof(MockBindable), null, defaultValueCreator: o =>
			{
				invoked++;
				return new object();
			});
			var bindable = new MockBindable();

			var value0 = bindable.GetValue(bindableProperty);
			var value1 = bindable.GetValue(bindableProperty);
			Assert.NotNull(value0);
			Assert.NotNull(value1);
			Assert.Same(value0, value1);
			Assert.Equal(1, invoked);
		}

		[Fact]
		public void DefaultValueCreatorNotSharedAccrossInstances()
		{
			int invoked = 0;
			var bindableProperty = BindableProperty.Create("Foo", typeof(object), typeof(MockBindable), null, defaultValueCreator: o =>
			{
				invoked++;
				return new object();
			});
			var bindable0 = new MockBindable();
			var bindable1 = new MockBindable();
			var value0 = bindable0.GetValue(bindableProperty);
			var value1 = bindable1.GetValue(bindableProperty);

			Assert.NotSame(value0, value1);
			Assert.Equal(2, invoked);
		}

		[Fact]
		public void DefaultValueCreatorInvokedAfterClearValue()
		{
			int invoked = 0;
			var bindableProperty = BindableProperty.Create("Foo", typeof(object), typeof(MockBindable), null, defaultValueCreator: o =>
			{
				invoked++;
				return new object();
			});
			var bindable = new MockBindable();

			Assert.Equal(0, invoked);

			var value0 = bindable.GetValue(bindableProperty);
			Assert.NotNull(value0);
			Assert.Equal(1, invoked);
			bindable.ClearValue(bindableProperty);

			var value1 = bindable.GetValue(bindableProperty);
			Assert.NotNull(value1);
			Assert.Equal(2, invoked);
			Assert.NotSame(value0, value1);
		}

		[Fact]
		public void DefaultValueCreatorOnlyInvokedOnGetValue()
		{
			int invoked = 0;
			var bindableProperty = BindableProperty.Create("Foo", typeof(object), typeof(MockBindable), null, defaultValueCreator: o =>
			{
				invoked++;
				return new object();
			});
			var bindable = new MockBindable();

			Assert.Equal(0, invoked);

			var newvalue = bindable.GetValue(bindableProperty);
			Assert.NotNull(newvalue);
			Assert.Equal(1, invoked);
		}

		[Fact]
		public void DefaultValueCreatorDoesNotTriggerINPC()
		{
			int invoked = 0;
			int propertychanged = 0;
			int changedfired = 0;
			var bindableProperty = BindableProperty.Create("Foo", typeof(object), typeof(MockBindable), null,
				propertyChanged: (bindable, oldvalue, newvalue) =>
				{
					propertychanged++;
				},
				defaultValueCreator: o =>
				{
					invoked++;
					return new object();
				});

			var bp = new MockBindable();
			bp.PropertyChanged += (sender, e) =>
			{
				if (e.PropertyName == "Foo")
					changedfired++;
			};
			var value0 = bp.GetValue(bindableProperty);
			Assert.NotNull(value0);
			Assert.Equal(1, invoked);
			Assert.Equal(0, propertychanged);
			Assert.Equal(0, changedfired);

		}

		[Fact]
		public void IsSetIsFalseWhenPropNotSet()
		{
			string defaultValue = "default";
			var bindableProperty = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable), defaultValue);
			var bindable = new MockBindable();

			Assert.Equal(defaultValue, bindableProperty.DefaultValue);

			var isSet = bindable.IsSet(bindableProperty);
			Assert.False(isSet);
		}

		[Fact]
		public void IsSetIsTrueWhenPropSet()
		{
			string defaultValue = "default";
			string newValue = "new value";

			var bindableProperty = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable), defaultValue);
			var bindable = new MockBindable();

			Assert.Equal(defaultValue, bindableProperty.DefaultValue);

			bindable.SetValue(bindableProperty, newValue);

			var isSet = bindable.IsSet(bindableProperty);
			Assert.True(isSet);
		}

		[Fact]
		public void IsSetIsFalseWhenPropCleared()
		{
			string defaultValue = "default";
			string newValue = "new value";

			var bindableProperty = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable), defaultValue);
			var bindable = new MockBindable();

			bindable.SetValue(bindableProperty, newValue);
			bindable.ClearValue(bindableProperty);

			Assert.False(bindable.IsSet(bindableProperty));
		}

		[Fact]
		public void IsSetIsTrueWhenPropSetToDefault()
		{
			string defaultValue = "default";

			var bindableProperty = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable), defaultValue);
			var bindable = new MockBindable();

			Assert.Equal(defaultValue, bindableProperty.DefaultValue);

			bindable.SetValue(bindableProperty, defaultValue);

			var isSet = bindable.IsSet(bindableProperty);
			Assert.True(isSet);
		}

		[Fact]
		public void IsSetIsTrueWhenPropSetByDefaultValueCreator()
		{
			string defaultValue = "default";
			string defaultValueC = "defaultVC";

			var bindableProperty = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable), defaultValue, defaultValueCreator: o =>
			{
				return defaultValueC;
			});
			var bindable = new MockBindable();

			Assert.Equal(defaultValue, bindableProperty.DefaultValue);

			var created = bindable.GetValue(bindableProperty);
			Assert.Equal(defaultValueC, created);

			var isSet = bindable.IsSet(bindableProperty);
			Assert.True(isSet);
		}

		[Fact]
		public void StyleValueIsOverridenByValue()
		{
			var label = new Label();
			label.SetValue(Label.TextProperty, "Foo", true);
			Assert.Equal("Foo", label.Text);

			label.SetValue(Label.TextProperty, "Bar");
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleBindingIsOverridenByValue()
		{
			var label = new Label();
			label.SetBinding(Label.TextProperty, new Binding("foo"), true);
			label.BindingContext = new { foo = "Foo" };
			Assert.Equal("Foo", label.Text);

			label.SetValue(Label.TextProperty, "Bar");
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleDynResourceIsOverridenByValue()
		{
			var label = new Label();
			label.SetDynamicResource(Label.TextProperty, "foo", true);
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"}
			};
			Assert.Equal("Foo", label.Text);

			label.SetValue(Label.TextProperty, "Bar");
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleValueIsOverridenByBinding()
		{
			var label = new Label();
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetValue(Label.TextProperty, "Foo", true);
			Assert.Equal("Foo", label.Text);

			label.SetBinding(Label.TextProperty, "bar");
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleBindingIsOverridenByBinding()
		{
			var label = new Label();
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetBinding(Label.TextProperty, new Binding("foo"), true);
			Assert.Equal("Foo", label.Text);

			label.SetBinding(Label.TextProperty, "bar");
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleDynResourceIsOverridenByBinding()
		{
			var label = new Label();
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetDynamicResource(Label.TextProperty, "foo", true);
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"},
			};
			Assert.Equal("Foo", label.Text);

			label.SetBinding(Label.TextProperty, "bar");
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleValueIsOverridenByDynResource()
		{
			var label = new Label();
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"},
				{"bar", "Bar"}
			};
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetValue(Label.TextProperty, "Foo", true);
			Assert.Equal("Foo", label.Text);

			label.SetDynamicResource(Label.TextProperty, "bar");
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleBindingIsOverridenByDynResource()
		{
			var label = new Label();
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"},
				{"bar", "Bar"}
			};
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetBinding(Label.TextProperty, new Binding("foo"), true);
			Assert.Equal("Foo", label.Text);

			label.SetDynamicResource(Label.TextProperty, "bar");
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleDynResourceIsOverridenByDynResource()
		{
			var label = new Label();
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"},
				{"bar", "Bar"}
			};
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetDynamicResource(Label.TextProperty, "foo", true);

			Assert.Equal("Foo", label.Text);

			label.SetDynamicResource(Label.TextProperty, "bar");
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void ValueIsPreservedOnStyleValue()
		{
			var label = new Label();
			label.SetValue(Label.TextProperty, "Foo");
			Assert.Equal("Foo", label.Text);

			label.SetValue(Label.TextProperty, "Bar", true);
			Assert.Equal("Foo", label.Text);
		}
		[Fact]
		public void BindingIsPreservedOnStyleValue()
		{
			var label = new Label();
			label.SetBinding(Label.TextProperty, new Binding("foo"));
			label.BindingContext = new { foo = "Foo" };
			Assert.Equal("Foo", label.Text);

			label.SetValue(Label.TextProperty, "Bar", true);
			Assert.Equal("Foo", label.Text);
		}
		[Fact]
		public void DynResourceIsPreservedOnStyleValue()
		{
			var label = new Label();
			label.SetDynamicResource(Label.TextProperty, "foo");
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"}
			};
			Assert.Equal("Foo", label.Text);

			label.SetValue(Label.TextProperty, "Bar", true);
			Assert.Equal("Foo", label.Text);
		}

		[Fact]
		public void ValueIsPreservedOnStyleBinding()
		{
			var label = new Label();
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetValue(Label.TextProperty, "Foo");
			Assert.Equal("Foo", label.Text);

			label.SetBinding(Label.TextProperty, new Binding("bar"), true);
			Assert.Equal("Foo", label.Text);
		}

		[Fact]
		public void BindingIsPreservedOnStyleBinding()
		{
			var label = new Label();
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetBinding(Label.TextProperty, new Binding("foo"));
			Assert.Equal("Foo", label.Text);

			label.SetBinding(Label.TextProperty, new Binding("bar"), true);
			Assert.Equal("Foo", label.Text);
		}

		[Fact]
		public void DynResourceIsPreservedOnStyleBinding()
		{
			var label = new Label();
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetDynamicResource(Label.TextProperty, "foo");
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"},
			};
			Assert.Equal("Foo", label.Text);

			label.SetBinding(Label.TextProperty, new Binding("bar"), true);
			Assert.Equal("Foo", label.Text);
		}

		[Fact]
		public void ValueIsPreservedOnStyleDynResource()
		{
			var label = new Label();
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"},
				{"bar", "Bar"}
			};
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetValue(Label.TextProperty, "Foo");
			Assert.Equal("Foo", label.Text);

			label.SetDynamicResource(Label.TextProperty, "bar", true);
			Assert.Equal("Foo", label.Text);
		}

		[Fact]
		public void BindingIsPreservedOnStyleDynResource()
		{
			var label = new Label();
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"},
				{"bar", "Bar"}
			};
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetBinding(Label.TextProperty, new Binding("foo"));
			Assert.Equal("Foo", label.Text);

			label.SetDynamicResource(Label.TextProperty, "bar", true);
			Assert.Equal("Foo", label.Text);
		}

		[Fact]
		public void DynResourceIsPreservedOnStyleDynResource()
		{
			var label = new Label();
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"},
				{"bar", "Bar"}
			};
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetDynamicResource(Label.TextProperty, "foo");

			Assert.Equal("Foo", label.Text);

			label.SetDynamicResource(Label.TextProperty, "bar", true);
			Assert.Equal("Foo", label.Text);
		}

		[Fact]
		public void StyleValueIsOverridenByStyleValue()
		{
			var label = new Label();
			label.SetValue(Label.TextProperty, "Foo", true);
			Assert.Equal("Foo", label.Text);

			label.SetValue(Label.TextProperty, "Bar", true);
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleBindingIsOverridenByStyleValue()
		{
			var label = new Label();
			label.SetBinding(Label.TextProperty, new Binding("foo"), true);
			label.BindingContext = new { foo = "Foo" };
			Assert.Equal("Foo", label.Text);

			label.SetValue(Label.TextProperty, "Bar", true);
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleDynResourceIsOverridenByStyleValue()
		{
			var label = new Label();
			label.SetDynamicResource(Label.TextProperty, "foo", true);
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"}
			};
			Assert.Equal("Foo", label.Text);

			label.SetValue(Label.TextProperty, "Bar", true);
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleValueIsOverridenByStyleBinding()
		{
			var label = new Label();
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetValue(Label.TextProperty, "Foo", true);
			Assert.Equal("Foo", label.Text);

			label.SetBinding(Label.TextProperty, new Binding("bar"), true);
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleBindingIsOverridenByStyleBinding()
		{
			var label = new Label();
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetBinding(Label.TextProperty, new Binding("foo"), true);
			Assert.Equal("Foo", label.Text);

			label.SetBinding(Label.TextProperty, new Binding("bar"), true);
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleDynResourceIsOverridenByStyleBinding()
		{
			var label = new Label();
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetDynamicResource(Label.TextProperty, "foo", true);
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"},
			};
			Assert.Equal("Foo", label.Text);

			label.SetBinding(Label.TextProperty, new Binding("bar"), true);
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleValueIsOverridenByStyleDynResource()
		{
			var label = new Label();
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"},
				{"bar", "Bar"}
			};
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetValue(Label.TextProperty, "Foo", true);
			Assert.Equal("Foo", label.Text);

			label.SetDynamicResource(Label.TextProperty, "bar", true);
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleBindingIsOverridenByStyleDynResource()
		{
			var label = new Label();
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"},
				{"bar", "Bar"}
			};
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetBinding(Label.TextProperty, new Binding("foo"), true);
			Assert.Equal("Foo", label.Text);

			label.SetDynamicResource(Label.TextProperty, "bar", true);
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void StyleDynResourceIsOverridenByStyleDynResource()
		{
			var label = new Label();
			label.Resources = new ResourceDictionary {
				{"foo", "Foo"},
				{"bar", "Bar"}
			};
			label.BindingContext = new { foo = "Foo", bar = "Bar" };
			label.SetDynamicResource(Label.TextProperty, "foo", true);

			Assert.Equal("Foo", label.Text);

			label.SetDynamicResource(Label.TextProperty, "bar", true);
			Assert.Equal("Bar", label.Text);
		}

		[Fact]
		public void SetValueCoreImplicitelyCastBasicType()
		{
			var prop = BindableProperty.Create("Foo", typeof(int), typeof(MockBindable), 0);
			var bindable = new MockBindable();

			AssertEx.DoesNotThrow(() => bindable.SetValue(prop, (object)(short)42));
			Assert.Equal(42, bindable.GetValue(prop));

			bindable.SetValue(prop, (object)(long)-42);
			Assert.NotEqual(-42, bindable.GetValue(prop));
		}

		class CastFromString
		{
			public string Result { get; private set; }
			public static implicit operator CastFromString(string source)
			{
				var o = new CastFromString();
				o.Result = source;
				return o;
			}
		}

		[Fact]
		public void SetValueCoreInvokesOpImplicitOnPropertyType()
		{
			var prop = BindableProperty.Create("Foo", typeof(CastFromString), typeof(MockBindable), null);
			var bindable = new MockBindable();

			Assert.Null(bindable.GetValue(prop));
			bindable.SetValue(prop, "foo");

			Assert.Equal("foo", ((CastFromString)bindable.GetValue(prop)).Result);
		}

		class CastToString
		{
			string Result { get; set; }

			public CastToString(string result)
			{
				Result = result;
			}

			public static implicit operator string(CastToString source)
			{
				return source.Result;
			}

			public override string ToString()
			{
				throw new InvalidOperationException();
			}
		}

		[Fact]
		public void SetValueCoreInvokesOpImplicitOnValue()
		{
			var prop = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable), null);
			var bindable = new MockBindable();

			Assert.Null(bindable.GetValue(prop));
			bindable.SetValue(prop, new CastToString("foo"));

			Assert.Equal("foo", bindable.GetValue(prop));
		}

		[Fact]
		public void DefaultValueCreatorCalledForChangeDelegates()
		{
			int changedOld = -1;
			int changedNew = -1;

			int changingOld = -1;
			int changingNew = -1;
			var prop = BindableProperty.Create("Foo", typeof(int), typeof(MockBindable), 0, defaultValueCreator: b => 10,
												propertyChanged: (b, value, newValue) =>
												{
													changedOld = (int)value;
													changedNew = (int)newValue;
												},
												propertyChanging: (b, value, newValue) =>
												{
													changingOld = (int)value;
													changingNew = (int)newValue;
												});

			var bindable = new MockBindable();


			var defaultValue = (int)bindable.GetValue(prop);

			Assert.Equal(10, defaultValue);

			bindable.SetValue(prop, 5);

			bindable.ClearValue(prop);

			Assert.Equal(5, changedOld);
			Assert.Equal(5, changingOld);
			Assert.Equal(10, changedNew);
			Assert.Equal(10, changingNew);
		}

		[Fact]
		public void GetValuesDefaults()
		{
			var prop = BindableProperty.Create("Foo", typeof(int), typeof(MockBindable), 0);
			var prop1 = BindableProperty.Create("Foo1", typeof(int), typeof(MockBindable), 1);
			var prop2 = BindableProperty.Create("Foo2", typeof(int), typeof(MockBindable), 2);
			var bindable = new MockBindable();


			object[] values = bindable.GetValues(prop, prop1, prop2);
			Assert.True(values.Length == 3);
			Assert.Equal(0, values[0]);
			Assert.Equal(1, values[1]);
			Assert.Equal(2, values[2]);
		}

		[Fact]
		public void GetValues()
		{
			var prop = BindableProperty.Create("Foo", typeof(int), typeof(MockBindable), 0);
			var prop1 = BindableProperty.Create("Foo1", typeof(int), typeof(MockBindable), 1);
			var prop2 = BindableProperty.Create("Foo2", typeof(int), typeof(MockBindable), 2);
			var bindable = new MockBindable();
			bindable.SetValue(prop, 3);
			bindable.SetValue(prop2, 5);


			object[] values = bindable.GetValues(prop, prop1, prop2);
			Assert.True(values.Length == 3);
			Assert.Equal(3, values[0]);
			Assert.Equal(1, values[1]);
			Assert.Equal(5, values[2]);
		}

		class BindingContextConverter
			: IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return new MockViewModel { Text = value + "Converted" };
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				throw new NotImplementedException();
			}
		}

		[Fact]
		//https://bugzilla.xamarin.com/show_bug.cgi?id=24485
		public void BindingContextBoundThroughConverter()
		{
			var bindable = new MockBindable();
			bindable.BindingContext = "test";
			bindable.SetBinding(BindableObject.BindingContextProperty, new Binding(".", converter: new BindingContextConverter()));
			bindable.SetBinding(MockBindable.TextProperty, "Text");

			Assert.Equal("testConverted", bindable.Text);
		}

		public class VMLocator
		{
			public event EventHandler Invoked;
			public int Count;
			public object VM
			{
				get
				{
					Count++;
					var eh = Invoked;
					if (eh != null)
						eh(this, EventArgs.Empty);
					return new object();
				}
			}
		}

		[Fact]
		//https://bugzilla.xamarin.com/show_bug.cgi?id=27299
		public void BindingOnBindingContextDoesntReapplyBindingContextBinding()
		{
			var bindable = new MockBindable();
			var locator = new VMLocator();
			Assert.Equal(0, locator.Count);
			locator.Invoked += (sender, e) => Assert.True(locator.Count <= 1);
			bindable.SetBinding(BindableObject.BindingContextProperty, new Binding("VM", source: locator));
			Assert.True(locator.Count == 1);
		}

		[Fact]
		//https://github.com/xamarin/Xamarin.Forms/issues/2019
		public void EventSubscribingOnBindingContextChanged()
		{
			var source = new MockBindable();
			var bindable = new MockBindable();
			var property = BindableProperty.Create("foo", typeof(string), typeof(MockBindable), null);
			bindable.SetBinding(property, new Binding("BindingContext", source: source));
			Assert.Equal(null, (string)bindable.GetValue(property));
			BindableObject.SetInheritedBindingContext(source, "bar"); //inherited BC, only trigger BCChanged
			Assert.Equal("bar", (string)bindable.GetValue(property));
		}

		[Fact]
		public void BindingsEditableAfterUnapplied()
		{
			var bindable = new MockBindable();

			var binding = new Binding(".");
			bindable.SetBinding(MockBindable.TextProperty, binding);
			bindable.BindingContext = "foo";

			Assert.Equal(bindable.BindingContext, bindable.Text);

			bindable.RemoveBinding(MockBindable.TextProperty);

			AssertEx.DoesNotThrow(() => binding.Path = "foo");
		}

		[Fact]
		// https://bugzilla.xamarin.com/show_bug.cgi?id=24054
		public void BindingsAppliedUnappliedWithNullContext()
		{
			var bindable = new MockBindable();

			var binding = new Binding(".");
			bindable.SetBinding(MockBindable.TextProperty, binding);
			bindable.BindingContext = "foo";

			Assert.Equal(bindable.BindingContext, bindable.Text);

			bindable.BindingContext = null;

			Assert.Equal(bindable.BindingContext, bindable.Text);

			bindable.BindingContext = "bar";

			Assert.Equal(bindable.BindingContext, bindable.Text);

			bindable.RemoveBinding(MockBindable.TextProperty);

			AssertEx.DoesNotThrow(() => binding.Path = "Foo");
		}


		class InfoToString
		{
			public override string ToString() => "converted";
		}

		[Fact]
		//https://github.com/xamarin/Xamarin.Forms/issues/6281
		public void SetValueToTextInvokesToString()
		{
			var prop = BindableProperty.Create("foo", typeof(string), typeof(MockBindable), null);
			var bindable = new MockBindable();
			bindable.SetValue(prop, new InfoToString());

			Assert.Equal("converted", bindable.GetValue(prop));
		}

		[Fact]
		//https://github.com/xamarin/Xamarin.Forms/issues/6281
		public void SetBindingToTextInvokesToString()
		{
			var prop = BindableProperty.Create("foo", typeof(string), typeof(MockBindable), null);
			var bindable = new MockBindable() { BindingContext = new { info = new InfoToString() } };
			bindable.SetBinding(prop, "info");

			Assert.Equal("converted", bindable.GetValue(prop));
		}

		[Fact]
		//https://github.com/xamarin/Xamarin.Forms/issues/9151
		public void SetValueWorksWithAttachedCollectionValuedProperties()
		{
			var prop = BindableProperty.CreateAttached("AttachedCollection", typeof(IList<string>), typeof(AttachedPropertyHolder), null, defaultValueCreator: _ => new List<string>());
			var collection = new[] { "a", "b" };
			var bindable = new MockBindable() { BindingContext = new { info = collection } };
			bindable.SetBinding(prop, "info");

			Assert.Equal(collection, (IEnumerable<string>)bindable.GetValue(prop));
		}

		[Fact]
		//https://github.com/xamarin/Xamarin.Forms/issues/9151
		public void SetValueWorksWithAttachedCollectionValuedPropertiesOnMultipleBindables()
		{
			var prop = BindableProperty.CreateAttached("AttachedCollection", typeof(IList<string>), typeof(AttachedPropertyHolder), null, defaultValueCreator: _ => new List<string>());
			var collection1 = new[] { "a", "b" };
			var collection2 = new[] { "c", "d" };

			var bindable1 = new MockBindable() { BindingContext = new { info = collection1 } };
			bindable1.SetValue(prop, collection1);
			var bindable2 = new MockBindable() { BindingContext = new { info = collection2 } };
			bindable2.SetValue(prop, collection2);

			Assert.Equal(collection1, (IEnumerable<string>)bindable1.GetValue(prop));
			Assert.Equal(collection2, (IEnumerable<string>)bindable2.GetValue(prop));
		}
	}
}
