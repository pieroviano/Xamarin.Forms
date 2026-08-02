using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class DropGestureRecognizerTests : BaseTestFixture
	{
		public DropGestureRecognizerTests()
		{
			Device.PlatformServices = new MockPlatformServices();

			// GetStringValue/TrySetValue round-trip through the current culture, and the
			// DatePicker test case below is written in en-US. BaseTestFixture.TearDown restores
			// whatever the machine's culture was.
			System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
		}

		public override void Dispose()
		{
			base.Dispose();
			Device.PlatformServices = null;
		}

		[Fact]
		public void PropertySetters()
		{
			var dropRec = new DropGestureRecognizer() { AllowDrop = true };

			Command cmd = new Command(() => { });
			var parameter = new Object();
			dropRec.AllowDrop = true;
			dropRec.DragOverCommand = cmd;
			dropRec.DragOverCommandParameter = parameter;
			dropRec.DropCommand = cmd;
			dropRec.DropCommandParameter = parameter;

			Assert.Equal(true, dropRec.AllowDrop);
			Assert.Equal(cmd, dropRec.DragOverCommand);
			Assert.Equal(parameter, dropRec.DragOverCommandParameter);
			Assert.Equal(cmd, dropRec.DropCommand);
			Assert.Equal(parameter, dropRec.DropCommandParameter);
		}

		[Fact]
		public void DragOverCommandFires()
		{
			var dropRec = new DropGestureRecognizer() { AllowDrop = true };
			var parameter = new Object();
			object commandExecuted = null;
			Command cmd = new Command(() => commandExecuted = parameter);

			dropRec.DragOverCommand = cmd;
			dropRec.DragOverCommandParameter = parameter;
			dropRec.SendDragOver(new DragEventArgs(new DataPackage()));

			Assert.Equal(parameter, commandExecuted);
		}

		[Fact]
		public async Task DropCommandFires()
		{
			var dropRec = new DropGestureRecognizer() { AllowDrop = true };
			var parameter = new Object();
			object commandExecuted = null;
			Command cmd = new Command(() => commandExecuted = parameter);

			dropRec.DropCommand = cmd;
			dropRec.DropCommandParameter = parameter;
			await dropRec.SendDrop(new DropEventArgs(new DataPackageView(new DataPackage())));

			Assert.Equal(commandExecuted, parameter);
		}

		[Theory]
		[InlineData(typeof(Entry), "EntryTest")]
		[InlineData(typeof(Label), "LabelTest")]
		[InlineData(typeof(Editor), "EditorTest")]
		[InlineData(typeof(TimePicker), "01:00:00")]
		[InlineData(typeof(DatePicker), "12/12/2020 12:00:00 AM")]
		[InlineData(typeof(CheckBox), "True")]
		[InlineData(typeof(Switch), "True")]
		[InlineData(typeof(RadioButton), "True")]
		public async Task TextPackageCorrectlySetsOnCompatibleTarget(Type fieldType, string result)
		{
			var dropRec = new DropGestureRecognizer() { AllowDrop = true };
			var element = (View)Activator.CreateInstance(fieldType);
			element.GestureRecognizers.Add(dropRec);
			var args = new DropEventArgs(new DataPackageView(new DataPackage() { Text = result }));
			await dropRec.SendDrop(args);
			Assert.Equal(element.GetStringValue(), result);
		}

		[Fact]
		public async Task HandledTest()
		{
			string testString = "test String";
			var dropTec = new DropGestureRecognizer() { AllowDrop = true };
			var element = new Label();
			element.Text = "Text Shouldn't change";
			var args = new DropEventArgs(new DataPackageView(new DataPackage() { Text = testString }));
			args.Handled = true;
			await dropTec.SendDrop(args);
			Assert.NotEqual(element.Text, testString);
		}
	}
}
