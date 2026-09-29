using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Xunit;

namespace Wpf.UnitTests
{
	/// <summary>Dependency properties: identity, local values, WPF's precedence (local, style, inherited, default), callbacks.</summary>
	public class DependencyPropertyTests : WpfTestBase
	{
		sealed class Owner : FrameworkElement
		{
			public static readonly List<(object Old, object New)> Changes = new List<(object, object)>();

			public static readonly DependencyProperty CountProperty = DependencyProperty.Register(
				nameof(Count), typeof(int), typeof(Owner),
				new PropertyMetadata(7, (d, e) => Changes.Add((e.OldValue, e.NewValue))));

			public int Count
			{
				get => (int)GetValue(CountProperty);
				set => SetValue(CountProperty, value);
			}
		}

		[Fact]
		public void ABindablePropertyHasOneWrapper()
		{
			Run(() =>
			{
				DependencyProperty a = FrameworkElement.WidthProperty;
				DependencyProperty b = FrameworkElement.WidthProperty;

				Assert.Same(a, b);
				Assert.Equal("Width", a.Name);
				Assert.Equal(typeof(double), a.PropertyType);
			});
		}

		[Fact]
		public void ARegisteredPropertyHasItsDefaultAndRaisesItsCallback()
		{
			Run(() =>
			{
				Owner.Changes.Clear();
				var owner = new Owner();

				Assert.Equal(7, owner.Count);
				owner.Count = 9;

				Assert.Equal(9, owner.Count);
				Assert.Equal(new[] { ((object)7, (object)9) }, Owner.Changes);
			});
		}

		[Fact]
		public void ReadLocalValueTellsALocalValueFromTheDefault()
		{
			Run(() =>
			{
				var element = new Border();

				Assert.Same(DependencyProperty.UnsetValue, element.ReadLocalValue(FrameworkElement.WidthProperty));
				element.Width = 12;
				Assert.Equal(12.0, element.ReadLocalValue(FrameworkElement.WidthProperty));
				element.ClearValue(FrameworkElement.WidthProperty);
				Assert.True(double.IsNaN(element.Width));
			});
		}

		[Fact]
		public void SetValueThroughADependencyPropertyVariable()
		{
			Run(() =>
			{
				DependencyProperty width = FrameworkElement.WidthProperty;
				var element = new Border();

				element.SetValue(width, 30.0);

				Assert.Equal(30.0, element.GetValue(width));
				Assert.Equal(30, element.Width);
			});
		}

		[Fact]
		public void AnInheritedValueComesFromTheNearestAncestorThatSetsIt()
		{
			Run(() =>
			{
				var box = new TextBox();
				var panel = new StackPanel { Children = { box } };
				var window = new Window { Content = panel, FontSize = 20 };

				Assert.Equal(20, box.FontSize);
				panel.SetValue(TextElement.FontSizeProperty, 15.0);
				Assert.Equal(15, box.FontSize);
				box.FontSize = 9;
				Assert.Equal(9, box.FontSize);
				window.Close();
			});
		}

		[Fact]
		public void AnInheritedChangeReachesDescendantsThatDoNotSetIt()
		{
			Run(() =>
			{
				var label = new Label { Content = "x" };
				var window = new Window { Content = new Grid { Children = { label } } };
				_ = label.NativeView;

				window.FontSize = 16;

				Assert.Equal(16, label.FontSize);
				Assert.Equal(12.0, ((Xamarin.Forms.Label)((Xamarin.Forms.Wpf.NativeShell)label.NativeView).Inner).FontSize);
				window.Close();
			});
		}

		[Fact]
		public void AStyleValueIsBelowALocalValueAndAboveInheritance()
		{
			Run(() =>
			{
				var style = new Style(typeof(TextBlock)) { Setters = { new Setter(TextElement.FontSizeProperty, 30.0) } };
				var text = new TextBlock();
				var window = new Window { Content = text, FontSize = 20 };

				Assert.Equal(20, text.FontSize);
				text.Style = style;
				Assert.Equal(30, text.FontSize);
				text.FontSize = 40;
				Assert.Equal(40, text.FontSize);
				text.ClearValue(TextElement.FontSizeProperty);
				Assert.Equal(30, text.FontSize);
				window.Close();
			});
		}

		[Fact]
		public void DisablingAParentDisablesItsDescendants()
		{
			Run(() =>
			{
				var button = new Button();
				var panel = new StackPanel { Children = { button } };

				Assert.True(button.IsEnabled);
				panel.IsEnabled = false;
				Assert.False(button.IsEnabled);
				Assert.False(button.NativeView.IsEnabled);
				panel.IsEnabled = true;
				Assert.True(button.IsEnabled);
				Assert.True(button.NativeView.IsEnabled);
			});
		}

		[Fact]
		public void IsEnabledChangedIsRaisedForTheCoercedValue()
		{
			Run(() =>
			{
				var button = new Button();
				var panel = new StackPanel { Children = { button } };
				var seen = new List<bool>();
				button.IsEnabledChanged += (s, e) => seen.Add((bool)e.NewValue);

				panel.IsEnabled = false;

				Assert.Equal(new[] { false }, seen);
			});
		}

		[Fact]
		public void TheDataContextIsTheBindingContextAndIsInherited()
		{
			Run(() =>
			{
				var child = new TextBlock();
				var panel = new StackPanel { Children = { child } };

				panel.DataContext = "model";

				Assert.Equal("model", child.DataContext);
				Assert.Equal("model", child.BindingContext);
			});
		}

		[Fact]
		public void ABindingSetsTheTargetFromTheDataContext()
		{
			Run(() =>
			{
				var text = new TextBlock { DataContext = new { Name = "bound" } };

				text.SetBinding(TextBlock.TextProperty, "Name");

				Assert.Equal("bound", text.Text);
			});
		}
	}
}
