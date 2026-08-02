using System;
using System.Globalization;
using System.Threading;
using Xunit;


namespace Xamarin.Forms.Core.UnitTests
{
	public class BaseTestFixture : IDisposable
	{
		CultureInfo _defaultCulture;
		CultureInfo _defaultUICulture;

		public BaseTestFixture()
		{
			_defaultCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
			_defaultUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
			Device.PlatformServices = new MockPlatformServices();
		}

		public virtual void Dispose()
		{
			Device.PlatformServices = null;
			System.Threading.Thread.CurrentThread.CurrentCulture = _defaultCulture;
			System.Threading.Thread.CurrentThread.CurrentUICulture = _defaultUICulture;
		}
	}
}
