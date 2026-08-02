using System;
using Xunit;


namespace Xamarin.Forms.Core.UnitTests
{
	public class TimePickerUnitTest : BaseTestFixture
	{
		[Fact]
		public void TestConstructor()
		{
			TimePicker picker = new TimePicker();

			Assert.Equal(new TimeSpan(), picker.Time);
		}

		[Fact]
		public void TestTimeOutOfRange()
		{
			TimePicker picker = new TimePicker();

			Assert.Throws<ArgumentException>(() => picker.Time = new TimeSpan(1000, 0, 0));
			Assert.Equal(picker.Time, new TimeSpan());

			picker.Time = new TimeSpan(8, 30, 0);

			Assert.Equal(new TimeSpan(8, 30, 0), picker.Time);

			Assert.Throws<ArgumentException>(() => picker.Time = new TimeSpan(-1, 0, 0));
			Assert.Equal(new TimeSpan(8, 30, 0), picker.Time);
		}

		[Fact]
		[Trait("Description", "Issue #745")]
		public void ZeroTimeIsValid()
		{
			var picker = new TimePicker();

			AssertEx.DoesNotThrow(() => picker.Time = new TimeSpan(0, 0, 0));
		}
	}
}
