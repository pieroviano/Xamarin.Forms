using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public abstract class BindingBaseUnitTests : BaseTestFixture
	{
		internal class Logger : LogListener
		{
			public IReadOnlyList<string> Messages
			{
				get { return messages; }
			}

			public override void Warning(string category, string message)
			{
				messages.Add("[" + category + "] " + message);
			}

			readonly List<string> messages = new List<string>();
		}

		internal Logger log;

		protected abstract BindingBase CreateBinding(BindingMode mode = BindingMode.Default, string stringFormat = null);

		internal class ComplexMockViewModel : MockViewModel
		{
			public ComplexMockViewModel Model
			{
				get { return model; }
				set
				{
					if (model == value)
						return;

					model = value;
					OnPropertyChanged("Model");
				}
			}

			internal int count;
			public int QueryCount
			{
				get { return count++; }
			}

			[IndexerName("Indexer")]
			public string this[int v]
			{
				get { return values[v]; }
				set
				{
					if (values[v] == value)
						return;

					values[v] = value;
					OnPropertyChanged("Indexer[" + v + "]");
				}
			}

			public string[] Array
			{
				get;
				set;
			}

			public object DoStuff()
			{
				return null;
			}

			public object DoStuff(object argument)
			{
				return null;
			}

			string[] values = new string[5];
			ComplexMockViewModel model;
		}

		[Fact]
		public void CloneMode()
		{
			var binding = CreateBinding(BindingMode.Default);
			var clone = binding.Clone();

			Assert.Equal(binding.Mode, clone.Mode);
		}

		[Fact]
		public void StringFormat()
		{
			var property = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable));
			var binding = CreateBinding(BindingMode.Default, "Foo {0}");

			var vm = new MockViewModel { Text = "Bar" };
			var bo = new MockBindable { BindingContext = vm };
			bo.SetBinding(property, binding);

			Assert.Equal("Foo Bar", bo.GetValue(property));
		}

		[Fact]
		public void StringFormatOnUpdate()
		{
			var property = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable));
			var binding = CreateBinding(BindingMode.Default, "Foo {0}");

			var vm = new MockViewModel { Text = "Bar" };
			var bo = new MockBindable { BindingContext = vm };
			bo.SetBinding(property, binding);

			vm.Text = "Baz";

			Assert.Equal("Foo Baz", bo.GetValue(property));
		}

		[Fact]
		[Trait("Description", "StringFormat should not be applied to OneWayToSource bindings")]
		public void StringFormatOneWayToSource()
		{
			var property = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable));
			var binding = CreateBinding(BindingMode.OneWayToSource, "Foo {0}");

			var vm = new MockViewModel { Text = "Bar" };
			var bo = new MockBindable { BindingContext = vm };
			bo.SetBinding(property, binding);

			bo.SetValue(property, "Bar");

			Assert.Equal("Bar", vm.Text);
		}

		[Fact]
		[Trait("Description", "StringFormat should only be applied from from source in TwoWay bindings")]
		public void StringFormatTwoWay()
		{
			var property = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable));
			var binding = CreateBinding(BindingMode.TwoWay, "Foo {0}");

			var vm = new MockViewModel { Text = "Bar" };
			var bo = new MockBindable { BindingContext = vm };
			bo.SetBinding(property, binding);

			bo.SetValue(property, "Baz");

			Assert.Equal("Baz", vm.Text);
			Assert.Equal("Foo Baz", bo.GetValue(property));
		}

		[Fact]
		[Trait("Description", "You should get an exception when trying to change a binding after it's been applied")]
		public void ChangeAfterApply()
		{
			var property = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable));
			var binding = CreateBinding(BindingMode.OneWay);

			var vm = new MockViewModel { Text = "Bar" };
			var bo = new MockBindable { BindingContext = vm };
			bo.SetBinding(property, binding);

			Assert.Throws<InvalidOperationException>(() => binding.Mode = BindingMode.OneWayToSource);
			Assert.Throws<InvalidOperationException>(() => binding.StringFormat = "{0}");
		}

		[Theory]
		[InlineData("en-US")]
		[InlineData("tr-TR")]
		public void StringFormatNonStringType(string culture)
		{
			System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture);

			var property = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable));
			var binding = new Binding("Value", stringFormat: "{0:P2}");

			var vm = new { Value = 0.95d };
			var bo = new MockBindable { BindingContext = vm };
			bo.SetBinding(property, binding);

			Assert.Equal(string.Format(new System.Globalization.CultureInfo(culture), "{0:P2}", .95d), bo.GetValue(property)); //%95,00 or 95.00%
		}

		[Fact]
		public void ReuseBindingInstance()
		{
			var vm = new MockViewModel();

			var bindable = new MockBindable();
			bindable.BindingContext = vm;

			var property = BindableProperty.Create("Foo", typeof(string), typeof(MockBindable));
			var binding = new Binding("Text");
			bindable.SetBinding(property, binding);

			var bindable2 = new MockBindable();
			bindable2.BindingContext = new MockViewModel();
			Assert.Throws<InvalidOperationException>(() => bindable2.SetBinding(property, binding));

			GC.KeepAlive(bindable);
		}

		[Theory]
		[Trait("Category", "[Binding] Set Value")]
		[InlineData(true, true)]
		[InlineData(true, false)]
		[InlineData(false, true)]
		[InlineData(false, false)]
		public void ValueSetOnOneWay(bool setContextFirst, bool isDefault)
		{
			const string value = "Foo";
			var viewmodel = new MockViewModel
			{
				Text = value
			};

			BindingMode propertyDefault = BindingMode.OneWay;
			BindingMode bindingMode = BindingMode.OneWay;
			if (isDefault)
			{
				propertyDefault = BindingMode.OneWay;
				bindingMode = BindingMode.Default;
			}

			var property = BindableProperty.Create("Text", typeof(string), typeof(MockBindable), null, propertyDefault);
			var binding = CreateBinding(bindingMode);

			var bindable = new MockBindable();
			if (setContextFirst)
			{
				bindable.BindingContext = viewmodel;
				bindable.SetBinding(property, binding);
			}
			else
			{
				bindable.SetBinding(property, binding);
				bindable.BindingContext = viewmodel;
			}

			Assert.Equal(value, viewmodel.Text);
			Assert.Equal(value, bindable.GetValue(property));
			Assert.Equal(0, log.Messages.Count);
		}

		[Theory]
		[Trait("Category", "[Binding] Set Value")]
		[InlineData(true, true)]
		[InlineData(true, false)]
		[InlineData(false, true)]
		[InlineData(false, false)]
		public void ValueSetOnOneWayToSource(bool setContextFirst, bool isDefault)
		{
			const string value = "Foo";
			var viewmodel = new MockViewModel();

			BindingMode propertyDefault = BindingMode.OneWay;
			BindingMode bindingMode = BindingMode.OneWayToSource;
			if (isDefault)
			{
				propertyDefault = BindingMode.OneWayToSource;
				bindingMode = BindingMode.Default;
			}

			var property = BindableProperty.Create("Text", typeof(string), typeof(MockBindable), defaultValue: value, defaultBindingMode: propertyDefault);
			var binding = CreateBinding(bindingMode);

			var bindable = new MockBindable();
			if (setContextFirst)
			{
				bindable.BindingContext = viewmodel;
				bindable.SetBinding(property, binding);
			}
			else
			{
				bindable.SetBinding(property, binding);
				bindable.BindingContext = viewmodel;
			}

			Assert.Equal(value, bindable.GetValue(property));
			Assert.Equal(value, viewmodel.Text);
			Assert.Equal(0, log.Messages.Count);
		}

		[Theory]
		[Trait("Category", "[Binding] Set Value")]
		[InlineData(true, true)]
		[InlineData(true, false)]
		[InlineData(false, true)]
		[InlineData(false, false)]
		public void ValueSetOnTwoWay(bool setContextFirst, bool isDefault)
		{
			const string value = "Foo";
			var viewmodel = new MockViewModel
			{
				Text = value
			};

			BindingMode propertyDefault = BindingMode.OneWay;
			BindingMode bindingMode = BindingMode.TwoWay;
			if (isDefault)
			{
				propertyDefault = BindingMode.TwoWay;
				bindingMode = BindingMode.Default;
			}

			var property = BindableProperty.Create("Text", typeof(string), typeof(MockBindable), defaultValue: "default value", defaultBindingMode: propertyDefault);
			var binding = CreateBinding(bindingMode);

			var bindable = new MockBindable();
			if (setContextFirst)
			{
				bindable.BindingContext = viewmodel;
				bindable.SetBinding(property, binding);
			}
			else
			{
				bindable.SetBinding(property, binding);
				bindable.BindingContext = viewmodel;
			}

			Assert.Equal(value, viewmodel.Text);
			Assert.Equal(value, bindable.GetValue(property));
			Assert.Equal(0, log.Messages.Count);
		}

		[Theory]
		[Trait("Category", "[Binding] Update Value")]
		[InlineData(true)]
		[InlineData(false)]
		public void ValueUpdatedWithSimplePathOnOneWayBinding(bool isDefault)
		{
			const string newvalue = "New Value";
			var viewmodel = new MockViewModel
			{
				Text = "Foo"
			};

			BindingMode propertyDefault = BindingMode.OneWay;
			BindingMode bindingMode = BindingMode.OneWay;
			if (isDefault)
			{
				propertyDefault = BindingMode.OneWay;
				bindingMode = BindingMode.Default;
			}

			var property = BindableProperty.Create("Text", typeof(string), typeof(MockBindable), "default value", propertyDefault);
			var binding = CreateBinding(bindingMode);

			var bindable = new MockBindable();
			bindable.BindingContext = viewmodel;
			bindable.SetBinding(property, binding);

			viewmodel.Text = newvalue;
			Assert.Equal(newvalue, bindable.GetValue(property));
			Assert.Equal(newvalue, viewmodel.Text);
			Assert.Equal(0, log.Messages.Count);
		}

		[Theory]
		[Trait("Category", "[Binding] Update Value")]
		[InlineData(true)]
		[InlineData(false)]
		public void ValueUpdatedWithSimplePathOnOneWayToSourceBinding(bool isDefault)

		{
			const string newvalue = "New Value";
			var viewmodel = new MockViewModel
			{
				Text = "Foo"
			};

			BindingMode propertyDefault = BindingMode.OneWay;
			BindingMode bindingMode = BindingMode.OneWayToSource;
			if (isDefault)
			{
				propertyDefault = BindingMode.OneWayToSource;
				bindingMode = BindingMode.Default;
			}

			var property = BindableProperty.Create("Text", typeof(string), typeof(MockBindable), "default value", propertyDefault);
			var binding = CreateBinding(bindingMode);

			var bindable = new MockBindable();
			bindable.BindingContext = viewmodel;
			bindable.SetBinding(property, binding);

			string original = (string)bindable.GetValue(property);
			const string value = "value";
			viewmodel.Text = value;
			Assert.Equal(original, bindable.GetValue(property));

			bindable.SetValue(property, newvalue);
			Assert.Equal(newvalue, bindable.GetValue(property));
			Assert.Equal(newvalue, viewmodel.Text);
			Assert.Equal(0, log.Messages.Count);
		}

		[Theory]
		[Trait("Category", "[Binding] Update Value")]
		[InlineData(true)]
		[InlineData(false)]
		public void ValueUpdatedWithSimplePathOnTwoWayBinding(bool isDefault)
		{
			const string newvalue = "New Value";
			var viewmodel = new MockViewModel
			{
				Text = "Foo"
			};

			BindingMode propertyDefault = BindingMode.OneWay;
			BindingMode bindingMode = BindingMode.TwoWay;
			if (isDefault)
			{
				propertyDefault = BindingMode.TwoWay;
				bindingMode = BindingMode.Default;
			}

			var property = BindableProperty.Create("Text", typeof(string), typeof(MockBindable), "default value", propertyDefault);
			var binding = CreateBinding(bindingMode);

			var bindable = new MockBindable();
			bindable.BindingContext = viewmodel;
			bindable.SetBinding(property, binding);

			viewmodel.Text = newvalue;
			Assert.Equal(newvalue, bindable.GetValue(property));
			Assert.Equal(newvalue, viewmodel.Text);

			const string newvalue2 = "New Value in the other direction";

			bindable.SetValue(property, newvalue2);
			Assert.Equal(newvalue2, viewmodel.Text);
			Assert.Equal(newvalue2, bindable.GetValue(property));
			Assert.Equal(0, log.Messages.Count);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void ValueUpdatedWithOldContextDoesNotUpdateWithOneWayBinding(bool isDefault)
		{
			const string newvalue = "New Value";
			var viewmodel = new MockViewModel
			{
				Text = "Foo"
			};

			BindingMode propertyDefault = BindingMode.OneWay;
			BindingMode bindingMode = BindingMode.OneWay;
			if (isDefault)
			{
				propertyDefault = BindingMode.OneWay;
				bindingMode = BindingMode.Default;
			}

			var property = BindableProperty.Create("Text", typeof(string), typeof(MockBindable), "default value", propertyDefault);
			var binding = CreateBinding(bindingMode);

			var bindable = new MockBindable();
			bindable.BindingContext = viewmodel;
			bindable.SetBinding(property, binding);

			bindable.BindingContext = new MockViewModel();
			Assert.Equal(null, bindable.GetValue(property));

			viewmodel.Text = newvalue;
			Assert.Equal(null, bindable.GetValue(property));
			Assert.Equal(0, log.Messages.Count);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void ValueUpdatedWithOldContextDoesNotUpdateWithTwoWayBinding(bool isDefault)
		{
			const string newvalue = "New Value";
			var viewmodel = new MockViewModel
			{
				Text = "Foo"
			};

			BindingMode propertyDefault = BindingMode.OneWay;
			BindingMode bindingMode = BindingMode.TwoWay;
			if (isDefault)
			{
				propertyDefault = BindingMode.TwoWay;
				bindingMode = BindingMode.Default;
			}

			var property = BindableProperty.Create("Text", typeof(string), typeof(MockBindable), "default value", propertyDefault);
			var binding = CreateBinding(bindingMode);

			var bindable = new MockBindable();
			bindable.BindingContext = viewmodel;
			bindable.SetBinding(property, binding);

			bindable.BindingContext = new MockViewModel();
			Assert.Equal(null, bindable.GetValue(property));

			viewmodel.Text = newvalue;
			Assert.Equal(null, bindable.GetValue(property));

			string original = viewmodel.Text;

			bindable.SetValue(property, newvalue);
			Assert.Equal(original, viewmodel.Text);
			Assert.Equal(0, log.Messages.Count);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void ValueUpdatedWithOldContextDoesNotUpdateWithOneWayToSourceBinding(bool isDefault)
		{
			const string newvalue = "New Value";
			var viewmodel = new MockViewModel
			{
				Text = "Foo"
			};

			BindingMode propertyDefault = BindingMode.OneWay;
			BindingMode bindingMode = BindingMode.OneWayToSource;
			if (isDefault)
			{
				propertyDefault = BindingMode.OneWayToSource;
				bindingMode = BindingMode.Default;
			}

			var property = BindableProperty.Create("Text", typeof(string), typeof(MockBindable), "default value", propertyDefault);
			var binding = CreateBinding(bindingMode);

			var bindable = new MockBindable();
			bindable.BindingContext = viewmodel;
			bindable.SetBinding(property, binding);

			bindable.BindingContext = new MockViewModel();
			Assert.Equal(property.DefaultValue, bindable.GetValue(property));

			viewmodel.Text = newvalue;
			Assert.Equal(property.DefaultValue, bindable.GetValue(property));
			Assert.Equal(0, log.Messages.Count);
		}

		[Fact]
		public void BindingStaysOnUpdateValueFromBinding()
		{
			const string newvalue = "New Value";
			var viewmodel = new MockViewModel
			{
				Text = "Foo"
			};

			var property = BindableProperty.Create("Text", typeof(string), typeof(MockBindable), null);
			var binding = CreateBinding(BindingMode.Default);

			var bindable = new MockBindable();
			bindable.BindingContext = viewmodel;
			bindable.SetBinding(property, binding);

			viewmodel.Text = newvalue;
			Assert.Equal(newvalue, bindable.GetValue(property));

			const string newValue2 = "new value 2";
			viewmodel.Text = newValue2;
			Assert.Equal(newValue2, bindable.GetValue(property));

			Assert.Equal(0, log.Messages.Count);
		}

		[Fact]
		public void OneWayToSourceContextSetToNull()
		{
			var binding = new Binding("Text", BindingMode.OneWayToSource);

			MockBindable bindable = new MockBindable
			{
				BindingContext = new MockViewModel()
			};
			bindable.SetBinding(MockBindable.TextProperty, binding);

			AssertEx.DoesNotThrow(() => bindable.BindingContext = null);
		}

		[Theory]
		[Trait("Category", "[Binding] Simple paths")]
		[InlineData(BindingMode.OneWay)]
		[InlineData(BindingMode.OneWayToSource)]
		[InlineData(BindingMode.TwoWay)]
		public void SourceAndTargetAreWeakWeakSimplePath(BindingMode mode)
		{
			var property = BindableProperty.Create("Text", typeof(string), typeof(MockBindable), "default value", BindingMode.OneWay);
			var binding = CreateBinding(mode);

			WeakReference weakViewModel = null, weakBindable = null;

			int i = 0;
			Action create = null;
			create = () =>
			{
				if (i++ < 1024)
				{
					create();
					return;
				}

				MockBindable bindable = new MockBindable();
				weakBindable = new WeakReference(bindable);

				MockViewModel viewmodel = new MockViewModel();
				weakViewModel = new WeakReference(viewmodel);

				bindable.BindingContext = viewmodel;
				bindable.SetBinding(property, binding);

				AssertEx.DoesNotThrow(() => bindable.BindingContext = null);
			};

			create();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			if (mode == BindingMode.TwoWay || mode == BindingMode.OneWay)
				Assert.False(weakViewModel.IsAlive, "ViewModel wasn't collected");

			if (mode == BindingMode.TwoWay || mode == BindingMode.OneWayToSource)
				Assert.False(weakBindable.IsAlive, "Bindable wasn't collected");
		}

		[Fact]
		public void PropertyChangeBindingsOccurThroughMainThread()
		{
			var vm = new MockViewModel { Text = "text" };

			var bindable = new MockBindable();
			var binding = CreateBinding();
			bindable.BindingContext = vm;
			bindable.SetBinding(MockBindable.TextProperty, binding);

			bool invokeOnMainThreadWasCalled = false;
			Device.PlatformServices = new MockPlatformServices(a => invokeOnMainThreadWasCalled = true, isInvokeRequired: true);

			vm.Text = "updated";

			// If we wait five seconds and invokeOnMainThreadWasCalled still hasn't been set, something is very wrong
			// NUnit's Is.True.After polled for up to five seconds; xUnit has no delayed
			// constraint, so the wait is explicit.
			SpinWait.SpinUntil(() => invokeOnMainThreadWasCalled, TimeSpan.FromSeconds(5));
			Assert.True(invokeOnMainThreadWasCalled);
		}
	}

	public class BindingBaseTests : BaseTestFixture
	{
		[Fact]
		public void EnableCollectionSynchronizationInvalid()
		{
			Assert.ThrowsAny<ArgumentNullException>(() => BindingBase.EnableCollectionSynchronization(null, new object(),
			   (collection, context, method, access) => { }));
			Assert.ThrowsAny<ArgumentNullException>(() => BindingBase.EnableCollectionSynchronization(new string[0], new object(),
			   null));
			AssertEx.DoesNotThrow(() => BindingBase.EnableCollectionSynchronization(new string[0], null,
			   (collection, context, method, access) => { }));
		}

		[Fact]
		public void EnableCollectionSynchronization()
		{
			string[] stuff = new[] { "foo", "bar" };
			object context = new object();
			CollectionSynchronizationCallback callback = (collection, o, method, access) => { };

			BindingBase.EnableCollectionSynchronization(stuff, context, callback);

			CollectionSynchronizationContext syncContext;
			Assert.True(BindingBase.TryGetSynchronizedCollection(stuff, out syncContext));
			Assert.NotNull(syncContext);
			Assert.Same(syncContext.Callback, callback);
			Assert.NotNull(syncContext.ContextReference);
			Assert.Same(context, syncContext.ContextReference.Target);
		}

		[Fact]
		public void DisableCollectionSynchronization()
		{
			string[] stuff = new[] { "foo", "bar" };
			object context = new object();
			CollectionSynchronizationCallback callback = (collection, o, method, access) => { };

			BindingBase.EnableCollectionSynchronization(stuff, context, callback);

			BindingBase.DisableCollectionSynchronization(stuff);

			CollectionSynchronizationContext syncContext;
			Assert.False(BindingBase.TryGetSynchronizedCollection(stuff, out syncContext));
			Assert.Null(syncContext);
		}

		[Fact]
		public void CollectionAndContextAreHeldWeakly()
		{
			WeakReference weakCollection = null, weakContext = null;

			int i = 0;
			Action create = null;
			create = () =>
			{
				if (i++ < 1024)
				{
					create();
					return;
				}

				string[] collection = new[] { "foo", "bar" };
				weakCollection = new WeakReference(collection);

				object context = new object();
				weakContext = new WeakReference(context);

				BindingBase.EnableCollectionSynchronization(collection, context, (enumerable, o, method, access) => { });
			};

			create();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			Assert.False(weakCollection.IsAlive);
			Assert.False(weakContext.IsAlive);
		}

		[Fact]
		public void CollectionAndContextAreHeldWeaklyClosingOverCollection()
		{
			WeakReference weakCollection = null, weakContext = null;

			int i = 0;
			Action create = null;
			create = () =>
			{
				if (i++ < 1024)
				{
					create();
					return;
				}

				string[] collection = new[] { "foo", "bar" };
				weakCollection = new WeakReference(collection);

				object context = new object();
				weakContext = new WeakReference(context);

				BindingBase.EnableCollectionSynchronization(collection, context, (enumerable, o, method, access) =>
				{
					collection[0] = "baz";
				});
			};

			create();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			Assert.False(weakCollection.IsAlive);
			Assert.False(weakContext.IsAlive);
		}

		[Fact]
		public void DisableCollectionSynchronizationInvalid()
		{
			Assert.ThrowsAny<ArgumentNullException>(() => BindingBase.DisableCollectionSynchronization(null));
		}

		[Fact]
		public void TryGetSynchronizedCollectionInvalid()
		{
			CollectionSynchronizationContext context;
			Assert.ThrowsAny<ArgumentNullException>(() => BindingBase.TryGetSynchronizedCollection(null, out context));
		}

	}
}
