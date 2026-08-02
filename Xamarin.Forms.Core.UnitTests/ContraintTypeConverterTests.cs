using Xunit;


namespace Xamarin.Forms.Core.UnitTests
{
	public class ContraintTypeConverterTests : BaseTestFixture
	{
		[Fact]
		public void ConvertFrom()
		{
			var converter = new ConstraintTypeConverter();
			Assert.Equal(Constraint.Constant(1.0).Compute(null), ((Constraint)converter.ConvertFromInvariantString("1.0")).Compute(null));
			Assert.Equal(Constraint.Constant(1.3).Compute(null), ((Constraint)converter.ConvertFromInvariantString("1.3")).Compute(null));
		}
	}
}