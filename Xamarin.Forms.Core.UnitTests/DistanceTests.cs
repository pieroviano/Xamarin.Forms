using System;
using Xamarin.Forms.Maps;
using Xunit;


namespace Xamarin.Forms.Core.UnitTests
{
	public class DistanceTests : BaseTestFixture
	{
		[Fact]
		public void Constructor()
		{
			var distance = new Distance(25);
			Assert.Equal(25, distance.Meters);
		}

		[Fact]
		public void ConstructFromKilometers()
		{
			const double EPSILON = 0.001;

			Distance distance = Distance.FromKilometers(2);

			Assert.True(Math.Abs(distance.Kilometers - 2) < EPSILON);
			Assert.True(Math.Abs(distance.Meters - 2000) < EPSILON);
			Assert.True(Math.Abs(distance.Miles - 1.24274) < EPSILON);
		}

		[Fact]
		public void ConstructFromMeters()
		{
			const double EPSILON = 0.001;

			Distance distance = Distance.FromMeters(10560);

			Assert.True(Math.Abs(distance.Meters - 10560) < EPSILON);
			Assert.True(Math.Abs(distance.Miles - 6.5616798) < EPSILON);
			Assert.True(Math.Abs(distance.Kilometers - 10.56) < EPSILON);
		}

		[Fact]
		public void ConstructFromMiles()
		{
			const double EPSILON = 0.001;

			// Reached the limit of double precision using the number
			// of miles of the earth's circumference
			const double EPSILON_FOR_LARGE_MILES_TO_METERS = 16;

			// Reached the limit of double precision
			const double EPSILON_FOR_LARGE_MILES_TO_KM = 0.1;

			Distance distance = Distance.FromMiles(3963.1676);

			Assert.True(Math.Abs(distance.Miles - 3963.1676) < EPSILON);
			Assert.True(Math.Abs(distance.Meters - 6378099.99805) < EPSILON_FOR_LARGE_MILES_TO_METERS);
			Assert.True(Math.Abs(distance.Kilometers - 6378.09999805) < EPSILON_FOR_LARGE_MILES_TO_KM);
		}

		[Fact]
		public void ConstructFromPositions()
		{
			const double EPSILON = 0.001;

			Position position1 = new Position(37.403992, -122.034988);
			Position position2 = new Position(37.776691, -122.416534);

			Distance distance = Distance.BetweenPositions(position1, position2);

			Assert.True(Math.Abs(distance.Meters - 53363.08) < EPSILON);
			Assert.True(Math.Abs(distance.Kilometers - 53.36308) < EPSILON);
			Assert.True(Math.Abs(distance.Miles - 33.15828) < EPSILON);
		}

		[Theory]
		[InlineData(5, 5)]
		[InlineData(5, 6)]
		[InlineData(5, 7)]
		[InlineData(5, 8)]
		[InlineData(5, 9)]
		[InlineData(6, 5)]
		[InlineData(6, 6)]
		[InlineData(6, 7)]
		[InlineData(6, 8)]
		[InlineData(6, 9)]
		[InlineData(7, 5)]
		[InlineData(7, 6)]
		[InlineData(7, 7)]
		[InlineData(7, 8)]
		[InlineData(7, 9)]
		[InlineData(8, 5)]
		[InlineData(8, 6)]
		[InlineData(8, 7)]
		[InlineData(8, 8)]
		[InlineData(8, 9)]
		[InlineData(9, 5)]
		[InlineData(9, 6)]
		[InlineData(9, 7)]
		[InlineData(9, 8)]
		[InlineData(9, 9)]
		public void EqualityOp(double x, double y)
		{
			bool result = Distance.FromMeters(x) == Distance.FromMeters(y);

			if (x == y)
				Assert.True(result);
			else
				Assert.False(result);
		}

		[Theory]
		[InlineData(3, 3)]
		[InlineData(3, 4)]
		[InlineData(3, 5)]
		[InlineData(3, 6)]
		[InlineData(3, 7)]
		[InlineData(4, 3)]
		[InlineData(4, 4)]
		[InlineData(4, 5)]
		[InlineData(4, 6)]
		[InlineData(4, 7)]
		[InlineData(5, 3)]
		[InlineData(5, 4)]
		[InlineData(5, 5)]
		[InlineData(5, 6)]
		[InlineData(5, 7)]
		[InlineData(6, 3)]
		[InlineData(6, 4)]
		[InlineData(6, 5)]
		[InlineData(6, 6)]
		[InlineData(6, 7)]
		[InlineData(7, 3)]
		[InlineData(7, 4)]
		[InlineData(7, 5)]
		[InlineData(7, 6)]
		[InlineData(7, 7)]
		public void EqualsForCoordinates(double x, double y)
		{
			bool result = Distance.FromMiles(x).Equals(Distance.FromMiles(y));
			if (x == y)
				Assert.True(result);
			else
				Assert.False(result);
		}

		[Fact]
		public void EqualsNull()
		{
			Assert.False(Distance.FromMeters(5).Equals(null));
		}

		[Fact]
		public void GettingAndSettingKilometers()
		{
			const double EPSILON = 0.001;

			Distance distance = Distance.FromKilometers(1891);
			Assert.True(Math.Abs(distance.Kilometers - 1891) < EPSILON);
		}

		[Fact]
		public void GettingAndSettingMeters()
		{
			const double EPSILON = 0.001;

			Distance distance = Distance.FromMeters(123434);
			Assert.True(Math.Abs(distance.Meters - 123434) < EPSILON);
		}

		[Fact]
		public void GettingAndSettingMiles()
		{
			const double EPSILON = 0.001;

			Distance distance = Distance.FromMiles(515);
			Assert.True(Math.Abs(distance.Miles - 515) < EPSILON);
		}

		[Theory]
		[InlineData(4, 4)]
		[InlineData(4, 5)]
		[InlineData(5, 4)]
		[InlineData(5, 5)]
		public void HashCode(double x, double y)
		{
			Distance distance1 = Distance.FromMiles(x);
			Distance distance2 = Distance.FromMiles(y);

			bool result = distance1.GetHashCode() == distance2.GetHashCode();

			if (x == y)
				Assert.True(result);
			else
				Assert.False(result);
		}

		[Theory]
		[InlineData(5, 5)]
		[InlineData(5, 6)]
		[InlineData(5, 7)]
		[InlineData(5, 8)]
		[InlineData(5, 9)]
		[InlineData(6, 5)]
		[InlineData(6, 6)]
		[InlineData(6, 7)]
		[InlineData(6, 8)]
		[InlineData(6, 9)]
		[InlineData(7, 5)]
		[InlineData(7, 6)]
		[InlineData(7, 7)]
		[InlineData(7, 8)]
		[InlineData(7, 9)]
		[InlineData(8, 5)]
		[InlineData(8, 6)]
		[InlineData(8, 7)]
		[InlineData(8, 8)]
		[InlineData(8, 9)]
		[InlineData(9, 5)]
		[InlineData(9, 6)]
		[InlineData(9, 7)]
		[InlineData(9, 8)]
		[InlineData(9, 9)]
		public void InequalityOp(double x, double y)
		{
			bool result = Distance.FromMeters(x) != Distance.FromMeters(y);

			if (x != y)
				Assert.True(result);
			else
				Assert.False(result);
		}

		[Fact]
		public void ObjectInitializerKilometers()
		{
			const double EPSILON = 0.001;

			Distance distance = Distance.FromKilometers(10);
			Assert.True(Math.Abs(distance.Meters - 10000) < EPSILON);
		}

		[Fact]
		public void ObjectInitializerMeters()
		{
			const double EPSILON = 0.001;

			Distance distance = Distance.FromMeters(1057);
			Assert.True(Math.Abs(distance.Kilometers - 1.057) < EPSILON);
		}

		[Fact]
		public void ObjectInitializerMiles()
		{
			const double EPSILON = 0.001;

			Distance distance = Distance.FromMiles(100);
			Assert.True(Math.Abs(distance.Meters - 160934.4) < EPSILON);
		}

		[Fact]
		public void ClampFromMeters()
		{
			var distance = Distance.FromMeters(-1);

			Assert.Equal(0, distance.Meters);
		}

		[Fact]
		public void ClampFromMiles()
		{
			var distance = Distance.FromMiles(-1);

			Assert.Equal(0, distance.Meters);
		}

		[Fact]
		public void ClampFromKilometers()
		{
			var distance = Distance.FromKilometers(-1);

			Assert.Equal(0, distance.Meters);
		}

		[Fact]
		public void EqualsTest()
		{
			Assert.True(Distance.FromMiles(2).Equals((object)Distance.FromMiles(2)));
		}
	}
}
