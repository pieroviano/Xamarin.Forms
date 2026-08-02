using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class MenuUnitTests : BaseTestFixture
	{
		public MenuUnitTests()
		{
			Device.PlatformServices = new MockPlatformServices();
			Application.Current = new MockApplication();
		}

		public override void Dispose()
		{
			Application.Current = null;
		}

		[Fact]
		public void SetMenuOnMenuItem()
		{
			var item = new MenuItem();
			var menu = new Menu { Text = "Hello" };
			MenuItem.SetMenu(item, menu);

			Assert.Equal(menu, MenuItem.GetMenu(item));
		}

		[Fact]
		public void AddSubMenuOnMenu()
		{
			var item = new MenuItem();
			var menu = new Menu { Text = "Hello" };
			var submenu = new Menu { Text = "SubMenu Hello" };
			menu.Add(submenu);

			MenuItem.SetMenu(item, menu);

			Assert.Equal(MenuItem.GetMenu(item), menu);
			Assert.Equal(MenuItem.GetMenu(item)[0], submenu);
			Assert.Equal(MenuItem.GetMenu(item)[0].Text, submenu.Text);
		}

		[Fact]
		public void SetMenuOnApplicationMainMenu()
		{
			var item = new MenuItem();
			var menu = new Menu { Text = "Hello" };
			Element.SetMenu(Application.Current, menu);
			Assert.True(1 >= Element.GetMenu(Application.Current).Count);
		}

		[Fact]
		public void MenuText()
		{
			string text = "hello";
			var menu = new Menu { Text = text };

			Assert.Equal(text, menu.Text);
		}

		[Fact]
		public void MenuInvalidateFiresPropertyChanged()
		{
			string text = "hello";
			int count = 0;
			var menu = new Menu { Text = text };

			menu.PropertyChanged += (s, e) =>
			{
				count = count + 1;
			};

			menu.Invalidate();

			Assert.Equal(1, count);
		}

		[Fact]
		public void MenuInvalidateWorksOnAdd()
		{
			string text = "hello";
			int count = 0;
			var menu = new Menu { Text = text };

			menu.PropertyChanged += (s, e) =>
			{
				count = count + 1;
			};

			menu.Add(new Menu());

			Assert.Equal(1, count);
		}

		[Fact]
		public void MenuInvalidateWorksOnClear()
		{
			string text = "hello";
			int count = 0;
			var menu = new Menu { Text = text };

			menu.PropertyChanged += (s, e) =>
			{
				count = count + 1;
			};

			menu.Add(new Menu());
			menu.Clear();

			Assert.Equal(2, count);
		}

		[Fact]
		public void MenuInvalidateWorksOnInsertAndRemove()
		{
			string text = "hello";
			int count = 0;
			var menu = new Menu { Text = text };

			menu.PropertyChanged += (s, e) =>
			{
				count = count + 1;
			};

			menu.Insert(0, new Menu());

			Assert.Equal(1, count);

			menu.RemoveAt(0);

			Assert.Equal(2, count);
		}


		[Fact]
		public void MenuFiresPropertyChangedOnAddItems()
		{
			string text = "hello";
			int count = 0;
			var menu = new Menu { Text = text };

			menu.PropertyChanged += (s, e) =>
			{
				count = count + 1;
			};

			menu.Items.Add(new MenuItem());
			Assert.Equal(1, count);
		}

	}
}
