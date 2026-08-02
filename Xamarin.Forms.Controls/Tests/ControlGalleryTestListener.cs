using System;
using System.Diagnostics;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xamarin.Forms.Controls.Tests
{
	// The xUnit counterpart of NUnit's ITestListener. Everything the engine reports arrives
	// here as an IMessageSinkMessage; the interesting ones are relayed over MessagingCenter
	// so PlatformTestsConsole can render them.
	public class ControlGalleryTestListener : IMessageSink
	{
		public bool OnMessage(IMessageSinkMessage message)
		{
			switch (message)
			{
				case ITestAssemblyFinished assemblyFinished:
					MessagingCenter.Send(assemblyFinished, "AssemblyFinished");
					break;
				case ITestClassStarting classStarting:
					MessagingCenter.Send(classStarting, "TestClassStarted");
					break;
				case ITestClassFinished classFinished:
					MessagingCenter.Send(classFinished, "TestClassFinished");
					break;
				case ITestResultMessage result:
					MessagingCenter.Send(result, "TestFinished");
					break;
				case ITestOutput output:
					Debug.WriteLine(output.Output);
					break;
				case IErrorMessage error:
					MessagingCenter.Send(new Exception(ExceptionUtility.CombineMessages(error)), "TestRunnerError");
					break;
			}

			return true;
		}
	}
}
