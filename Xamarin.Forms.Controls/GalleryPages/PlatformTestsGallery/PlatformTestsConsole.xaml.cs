using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms.Controls.Tests;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xamarin.Forms.Controls.GalleryPages.PlatformTestsGallery
{
	[Preserve(AllMembers = true)]
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PlatformTestsConsole : ContentPage
	{
		const string FailedText = "FAILED";
		// xUnit has no "inconclusive" outcome; a test either passes, fails, or is skipped.
		const string SkippedText = "Skipped";
		const string SuccessText = "SUCCESS";
		bool _runFailed;
		bool _runSkipped;
		readonly Color _successColor = Color.Green;
		readonly Color _failColor = Color.Red;
		readonly Color _skippedColor = Color.Goldenrod;

		int _finishedAssemblyCount = 0;
		int _testsRunCount = 0;

		readonly PlatformTestRunner _runner = new PlatformTestRunner();
		DisplaySettings _displaySettings;

		public PlatformTestsConsole()
		{
			InitializeComponent();
			MessagingCenter.Subscribe<ITestAssemblyFinished>(this, "AssemblyFinished", AssemblyFinished);

			MessagingCenter.Subscribe<ITestClassStarting>(this, "TestClassStarted", TestClassStarted);
			MessagingCenter.Subscribe<ITestClassFinished>(this, "TestClassFinished", TestClassFinished);
			MessagingCenter.Subscribe<ITestResultMessage>(this, "TestFinished", TestFinished);

			MessagingCenter.Subscribe<Exception>(this, "TestRunnerError", OutputTestRunnerError);

			Rerun.Clicked += RerunClicked;
			togglePassed.Clicked += ToggledPassedClicked;

			_displaySettings = new DisplaySettings();
			BindingContext = _displaySettings;
		}

		private void ToggledPassedClicked(object sender, EventArgs e)
		{
			_displaySettings.ShowPassed = !_displaySettings.ShowPassed;
		}

		async void RerunClicked(object sender, EventArgs e)
		{
			await Device.InvokeOnMainThreadAsync(() =>
			{
				Status.Text = "Running...";
				RunCount.Text = "";
				Results.Children.Clear();
				Rerun.IsEnabled = false;
				_displaySettings.ShowPassed = true;
			});

			await Task.Delay(50);

			await Run().ConfigureAwait(false);
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();

			if (_testsRunCount == 0)
			{
				await Run().ConfigureAwait(false);
			}
		}

		async Task Run()
		{
			_finishedAssemblyCount = 0;
			_testsRunCount = 0;

			// Only want to run a subset of tests? Create a filter and pass it into _runner.Run()
			// e.g. var filter = new TestNameContainsFilter("Bugzilla");
			// or var filter = new CategoryFilter("Picker") for tests marked with
			// [Trait(CategoryFilter.CategoryTrait, "Picker")].
			// NUnit's per-TestCaseData SetCategory has no xUnit equivalent - the control name
			// that ObjectDisposedExceptionTests used to categorise by is now a theory argument,
			// so it is part of the display name and TestNameContainsFilter reaches it.

			await Task.Run(() => _runner.Run()).ConfigureAwait(false);
		}

		void DisplayOverallResult()
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				if (_runFailed)
				{
					DisplayFailResult();
				}
				else if (_runSkipped)
				{
					Status.Text = SkippedText;
					Status.TextColor = _skippedColor;
				}
				else
				{
					Status.Text = SuccessText;
					Status.TextColor = _successColor;
					_displaySettings.ShowPassed = true;
				}

				RunCount.Text = $"{_testsRunCount} tests run";

				Rerun.IsEnabled = true;
			});
		}

		void DisplayFailResult(string failText = null)
		{
			failText = failText ?? FailedText;

			Status.Text = failText;
			Status.TextColor = _failColor;
			_displaySettings.ShowPassed = false;
		}

		void AssemblyFinished(ITestAssemblyFinished assembly)
		{
			// TestsRun is the total: passed + failed + skipped.
			_testsRunCount += assembly.TestsRun;

			_finishedAssemblyCount += 1;
			if (_finishedAssemblyCount == 2)
			{
				DisplayOverallResult();
			}
		}

		void TestClassStarted(ITestClassStarting testClass)
		{
			var name = testClass.TestClass.Class.Name;

			var label = new Label
			{
				Text = $"{name} Started",
				LineBreakMode = LineBreakMode.HeadTruncation,
				FontAttributes = FontAttributes.Bold
			};

			SetupPassedLabelBindings(label);

			Device.BeginInvokeOnMainThread(() =>
			{
				Results.Children.Add(label);
			});
		}

		void TestFinished(ITestResultMessage result)
		{
			var name = result.Test.DisplayName;

			var outcome = "Fail";

			if (result is ITestPassed)
			{
				outcome = "Pass";
			}
			else if (result is ITestSkipped)
			{
				outcome = SkippedText;
			}

			var label = new Label { Text = $"{name}: {outcome}.", LineBreakMode = LineBreakMode.HeadTruncation };

			if (result is ITestFailed)
			{
				label.TextColor = _failColor;
				_runFailed = true;
			}
			else if (result is ITestSkipped)
			{
				label.TextColor = _skippedColor;
				_runSkipped = true;
			}
			else
			{
				label.TextColor = _successColor;
				SetupPassedLabelBindings(label);
			}

			var margin = new Thickness(15, 0, 0, 0);
			label.Margin = margin;

			var toAdd = new List<View> { label };

			if (result is ITestFailed failed)
			{
				// A failure can carry several exceptions (an inner exception chain, or the
				// test failure plus a Dispose failure); ExceptionUtility flattens them the
				// same way the console runners do.
				ExtractErrorMessage(toAdd, ExceptionUtility.CombineMessages(failed));
				toAdd.Add(new Editor { Text = ExceptionUtility.CombineStackTraces(failed), IsReadOnly = true });
			}

			if (!string.IsNullOrEmpty(result.Output))
			{
				toAdd.Add(new Label { Text = result.Output, Margin = margin });
			}

			if (result is ITestSkipped skipped)
			{
				var reasonText = skipped.Reason;

				if (string.IsNullOrEmpty(reasonText))
				{
					reasonText = @"¯\_(ツ)_/¯";
				}

				toAdd.Add(new Label { Text = $"Test was skipped. Reason: {reasonText}", FontAttributes = FontAttributes.Bold, Margin = margin });
			}

			Device.BeginInvokeOnMainThread(() =>
			{
				foreach (var outputView in toAdd)
				{
					Results.Children.Add(outputView);
				}

			});
		}

		void TestClassFinished(ITestClassFinished result)
		{
			var name = result.TestClass.Class.Name;
			var passed = result.TestsRun - result.TestsFailed - result.TestsSkipped;

			var label = new Label { Text = $"{name} Finished.", LineBreakMode = LineBreakMode.HeadTruncation };
			var counts = new Label { Text = $"Passed: {passed}; Failed: {result.TestsFailed}; Skipped: {result.TestsSkipped}" };

			if (result.TestsFailed > 0)
			{
				label.TextColor = _failColor;
				_runFailed = true;
			}
			else if (result.TestsSkipped > 0)
			{
				label.TextColor = _skippedColor;
				_runSkipped = true;
			}
			else
			{
				label.TextColor = _successColor;
				SetupPassedLabelBindings(label);
				SetupPassedLabelBindings(counts);
			}

			counts.TextColor = label.TextColor;

			Device.BeginInvokeOnMainThread(() =>
			{
				Results.Children.Add(label);
				Results.Children.Add(counts);
			});
		}

		void OutputTestRunnerError(Exception ex)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				DisplayFailResult(ex.Message);
			});
		}

		static void ExtractErrorMessage(List<View> views, string message)
		{
			const string openTag = "<img>";
			const string closeTag = "</img>";
			var openTagIndex = message.IndexOf("<img>");
			var closeTagIndex = message.IndexOf("</img>");

			if (openTagIndex >= 0 && closeTagIndex > openTagIndex)
			{
				var imgString = message.Substring(openTagIndex + openTag.Length, closeTagIndex - openTagIndex - openTag.Length);
				var messageBefore = message.Substring(0, openTagIndex);
				var messageAfter = message.Substring(closeTagIndex + closeTag.Length);
				var imgBytes = Convert.FromBase64String(imgString);
				var stream = new MemoryStream(imgBytes);

				if (!string.IsNullOrEmpty(messageBefore))
				{
					views.Add(new Label { Text = messageBefore });
				}

				views.Add(new Image { Source = ImageSource.FromStream(() => stream) });

				if (!string.IsNullOrEmpty(messageAfter))
				{
					views.Add(new Label { Text = messageAfter });
				}
			}
			else
			{
				views.Add(new Label { Text = message });
			}
		}

		async void ResultsAdded(object sender, ElementEventArgs e)
		{
			await ResultsScrollView.ScrollToAsync(e.Element, ScrollToPosition.MakeVisible, false);
		}

		[Preserve(AllMembers = true)]
		public class DisplaySettings : INotifyPropertyChanged
		{
			bool _showPassed;

			public DisplaySettings()
			{
				_showPassed = true;
			}

			public event PropertyChangedEventHandler PropertyChanged;

			public bool ShowPassed
			{
				get => _showPassed;
				set
				{
					_showPassed = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowPassed)));
				}
			}
		}

		public Label SetupPassedLabelBindings(Label label)
		{
			label.SetBinding(Label.IsVisibleProperty, "ShowPassed");
			return label;
		}
	}
}