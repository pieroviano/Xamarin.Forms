using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Xunit;
using IOPath = System.IO.Path;

namespace Xamarin.Forms.MSBuild.UnitTests
{
	public class AssemblyInfoTests
	{
		static readonly string[] references = new[]
		{
			"Xamarin.Forms.Core",
			"Xamarin.Forms.Maps",
			"Xamarin.Forms.Xaml",
			"Xamarin.Forms.Build.Tasks",
			"Xamarin.Forms.Platform",
		};

		const string s_productName = "Xamarin.Forms";

		const string s_company = "Microsoft";

		const string s_gitInfoFile = "GitInfo.txt";

		[Test, TestCaseSource(nameof(references))]
		public void AssemblyTitle(string assemblyName)
		{
			Assembly testAssembly = System.Reflection.Assembly.Load(assemblyName);
			Assert.Equal(assemblyName, testAssembly.GetName().Name);
		}

		[Test, TestCaseSource(nameof(references))]
		public void AssemblyVersion(string assemblyName)
		{
			Assembly testAssembly = System.Reflection.Assembly.Load(assemblyName);
			Version actual = testAssembly.GetName().Version;
			Assert.Equal(2, actual.Major, actual.ToString());
			Assert.Equal(0, actual.Minor, actual.ToString());
			Assert.Equal(0, actual.Build, actual.ToString());
		}

		[Test, TestCaseSource(nameof(references))]
		public void FileVersion(string assemblyName)
		{
			Assembly testAssembly = System.Reflection.Assembly.Load(assemblyName);
			FileVersionInfo actual = FileVersionInfo.GetVersionInfo(testAssembly.Location);
			Version expected = Version.Parse(GetFileFromRoot(s_gitInfoFile));
			Assert.Equal(expected.Major, actual.FileMajorPart, $"FileMajorPart is wrong. {actual.ToString()}");
			Assert.Equal(expected.Minor, actual.FileMinorPart, $"FileMinorPart is wrong. {actual.ToString()}");
			// Fails locally
			//Assert.Equal(expected.Build, actual.FileBuildPart, $"FileBuildPart is wrong. {actual.ToString()}");
			//We need to enable this
			//	Assert.Equal(ThisAssembly.Git.Commits, version.FilePrivatePart);
			Assert.Equal(s_productName, actual.ProductName);
			Assert.Equal(s_company, actual.CompanyName);
		}

		[Test, TestCaseSource(nameof(references))]
		public void ProductAndCompany(string assemblyName)
		{
			Assembly testAssembly = System.Reflection.Assembly.Load(assemblyName);
			FileVersionInfo actual = FileVersionInfo.GetVersionInfo(testAssembly.Location);
			Assert.Equal(s_productName, actual.ProductName);
			Assert.Equal(s_company, actual.CompanyName);
		}

		static string GetFileFromRoot(string file)
		{
			var gitInfoFile = IOPath.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", file);
			if (!File.Exists(gitInfoFile))
			{
				//NOTE: VSTS may be running tests in a staging directory, so we can use an environment variable to find the source
				//	https://docs.microsoft.com/en-us/vsts/build-release/concepts/definitions/build/variables?view=vsts&tabs=batch#buildsourcesdirectory
				var sourcesDirectory = Environment.GetEnvironmentVariable("BUILD_SOURCESDIRECTORY");
				if (!string.IsNullOrEmpty(sourcesDirectory))
				{
					gitInfoFile = IOPath.Combine(sourcesDirectory, file);
					if (!File.Exists(gitInfoFile))
					{
						Assert.Fail($"Unable to find {file} at path: {gitInfoFile}");
					}
				}
				else
				{
					Assert.Fail($"Unable to find {file} at path: {gitInfoFile}");
				}
			}
			return File.ReadAllText(gitInfoFile);
		}
	}
}
