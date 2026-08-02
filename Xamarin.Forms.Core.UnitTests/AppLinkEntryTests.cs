using System;
using Xunit;

namespace Xamarin.Forms.Core.UnitTests
{
	public class AppLinkEntryTests : BaseTestFixture
	{

		[Fact]
		public void KeyValuesTest()
		{
			var entry = new AppLinkEntry();

			entry.KeyValues.Add("contentType", "GalleryPage");
			entry.KeyValues.Add("companyName", "Xamarin");
			Assert.Equal(entry.KeyValues.Count, 2);
		}


		[Fact]
		public void FromUriTest()
		{
			var uri = new Uri("http://foo.com");

			var entry = AppLinkEntry.FromUri(uri);

			Assert.Equal(uri, entry.AppLinkUri);
		}

		[Fact]
		public void ToStringTest()
		{
			var str = "http://foo.com";
			var uri = new Uri(str);

			var entry = new AppLinkEntry { AppLinkUri = uri };

			Assert.Equal(uri.ToString(), entry.ToString());
		}
	}
}