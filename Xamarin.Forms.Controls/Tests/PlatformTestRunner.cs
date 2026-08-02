using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit.Runner.Common;
using Xunit.Sdk;
using Xunit.v3;

namespace Xamarin.Forms.Controls.Tests
{
	public class PlatformTestRunner
	{
		readonly IMessageSink _testListener = new ControlGalleryTestListener();

		public async Task Run(ITestCaseFilter testFilter = null)
		{
			// "controls" is the cross-platform test assembly
#if NETSTANDARD2_0
			var controls = Assembly.GetExecutingAssembly();
#else
			var controls = typeof(PlatformTestRunner).GetTypeInfo().Assembly;
#endif

			var platformTestSettings = DependencyService.Resolve<IPlatformTestSettings>();

			// "platform" is the native assembly (ControGallery.iOS, ControlGallery.Android, etc.)
			Assembly platform = platformTestSettings.Assembly;

			// The TestRunSettings gives us a way to pass other parameters to the runner;
			// the keys are the xUnit option names in Xunit.Sdk.TestOptionsNames.
			var testRunSettings = platformTestSettings.TestRunSettings;

			try
			{
				await RunAssembly(controls, testFilter, testRunSettings);
				await RunAssembly(platform, testFilter, testRunSettings);
			}
			catch (Exception ex)
			{
				MessagingCenter.Send(ex, "TestRunnerError");
			}
		}

		async Task RunAssembly(Assembly assembly, ITestCaseFilter testFilter, Dictionary<string, object> testRunSettings)
		{
			var discoveryOptions = TestFrameworkOptions.Empty();
			var executionOptions = TestFrameworkOptions.Empty();

			// NUnit ran these assemblies one test at a time. xUnit parallelizes test
			// collections by default, and neither the renderers under test nor the single
			// native UI thread they marshal onto survive that, so the pre-migration
			// behaviour is restored here. Seeded before the platform's own settings are
			// applied so a platform can still override either key.
			executionOptions.SetValue(TestOptionsNames.Execution.DisableParallelization, true);
			executionOptions.SetValue(TestOptionsNames.Execution.MaxParallelThreads, 1);

			foreach (var setting in testRunSettings)
			{
				discoveryOptions.SetValue(setting.Key, setting.Value);
				executionOptions.SetValue(setting.Key, setting.Value);
			}

			// v3 resolves the framework against the already-loaded Assembly, so there is no
			// IAssemblyInfo to wrap and no source-information provider to stub out. Both
			// Find and RunTestCases complete when their phase is done, which is what
			// replaces v2's message-sink-plus-ManualResetEvent handshake.
			var framework = new XunitTestFramework();

			var testCases = new List<ITestCase>();

			await framework.GetDiscoverer(assembly).Find(testCase =>
			{
				if (testFilter == null || testFilter.Match(testCase))
				{
					testCases.Add(testCase);
				}

				return new ValueTask<bool>(true);
			}, discoveryOptions);

			await framework.GetExecutor(assembly).RunTestCases(testCases, _testListener, executionOptions);
		}
	}
}
