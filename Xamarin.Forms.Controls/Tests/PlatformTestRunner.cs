using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xamarin.Forms.Controls.Tests
{
	public class PlatformTestRunner
	{
		readonly IMessageSink _testListener = new ControlGalleryTestListener();

		public void Run(ITestCaseFilter testFilter = null)
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
			// the keys are the xUnit option names in TestOptionNames.
			var testRunSettings = platformTestSettings.TestRunSettings;

			try
			{
				RunAssembly(controls, testFilter, testRunSettings);
				RunAssembly(platform, testFilter, testRunSettings);
			}
			catch (Exception ex)
			{
				MessagingCenter.Send(ex, "TestRunnerError");
			}
		}

		void RunAssembly(Assembly assembly, ITestCaseFilter testFilter, Dictionary<string, object> testRunSettings)
		{
			var diagnosticMessageSink = new NullMessageSink();

			var discoveryOptions = new TestFrameworkOptions();
			var executionOptions = new TestFrameworkOptions();

			// NUnit ran these assemblies one test at a time. xUnit parallelizes test
			// collections by default, and neither the renderers under test nor the single
			// native UI thread they marshal onto survive that, so the pre-migration
			// behaviour is restored here. Seeded before the platform's own settings are
			// applied so a platform can still override either key.
			executionOptions.SetValue(TestOptionNames.DisableParallelization, true);
			executionOptions.SetValue(TestOptionNames.MaxParallelThreads, 1);

			foreach (var setting in testRunSettings)
			{
				discoveryOptions.SetValue(setting.Key, setting.Value);
				executionOptions.SetValue(setting.Key, setting.Value);
			}

			var testCases = Discover(assembly, testFilter, discoveryOptions, diagnosticMessageSink);

			// XunitTestFrameworkExecutor resolves the assembly by name; it is already loaded,
			// which is the whole reason this uses the Sdk types instead of a console runner.
			using (var executor = new XunitTestFrameworkExecutor(assembly.GetName(), new NullSourceInformationProvider(), diagnosticMessageSink))
			using (var sink = new AssemblyRunSink(_testListener))
			{
				// RunTests is asynchronous, so wait for the assembly to report itself finished
				// before starting the next one - the console counts two assembly completions.
				executor.RunTests(testCases, sink, executionOptions);
				sink.Finished.WaitOne();
			}
		}

		static List<ITestCase> Discover(Assembly assembly, ITestCaseFilter testFilter,
			ITestFrameworkDiscoveryOptions discoveryOptions, IMessageSink diagnosticMessageSink)
		{
			var assemblyInfo = Reflector.Wrap(assembly);

			using (var discoverer = new XunitTestFrameworkDiscoverer(assemblyInfo, new NullSourceInformationProvider(), diagnosticMessageSink))
			using (var sink = new DiscoverySink(testFilter))
			{
				discoverer.Find(false, sink, discoveryOptions);
				sink.Finished.WaitOne();

				return sink.TestCases;
			}
		}

		class DiscoverySink : IMessageSink, IDisposable
		{
			readonly ITestCaseFilter _testFilter;

			public DiscoverySink(ITestCaseFilter testFilter) => _testFilter = testFilter;

			public List<ITestCase> TestCases { get; } = new List<ITestCase>();

			public ManualResetEvent Finished { get; } = new ManualResetEvent(false);

			public bool OnMessage(IMessageSinkMessage message)
			{
				if (message is ITestCaseDiscoveryMessage discovered)
				{
					if (_testFilter == null || _testFilter.Match(discovered.TestCase))
					{
						TestCases.Add(discovered.TestCase);
					}
				}

				if (message is IDiscoveryCompleteMessage)
				{
					Finished.Set();
				}

				return true;
			}

			public void Dispose() => Finished.Dispose();
		}

		// Forwards everything to the listener and additionally unblocks RunAssembly once the
		// assembly is done.
		class AssemblyRunSink : IMessageSink, IDisposable
		{
			readonly IMessageSink _inner;

			public AssemblyRunSink(IMessageSink inner) => _inner = inner;

			public ManualResetEvent Finished { get; } = new ManualResetEvent(false);

			public bool OnMessage(IMessageSinkMessage message)
			{
				var result = _inner.OnMessage(message);

				if (message is ITestAssemblyFinished)
				{
					Finished.Set();
				}

				return result;
			}

			public void Dispose() => Finished.Dispose();
		}

		// xUnit only uses source information to point an IDE at a file and line; on device
		// there is nothing to point at.
		class NullSourceInformationProvider : ISourceInformationProvider
		{
			public ISourceInformation GetSourceInformation(ITestCase testCase) => new NullSourceInformation();

			public void Dispose()
			{
			}

			class NullSourceInformation : ISourceInformation
			{
				public string FileName { get; set; }
				public int? LineNumber { get; set; }
				public void Deserialize(IXunitSerializationInfo info) { }
				public void Serialize(IXunitSerializationInfo info) { }
			}
		}
	}
}
