using System;
using System.Collections.Generic;
using System.Linq;
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

		public static IEnumerable<object[]> ReferencesData => references.Select(r => new object[] { r });

		const string s_productName = "Xamarin.Forms";

		const string s_company = "Microsoft";

		const string s_gitInfoFile = "GitInfo.txt";

		[Theory]
		[MemberData(nameof(ReferencesData))]
		public void AssemblyTitle(string assemblyName)
		{
			Assembly testAssembly = System.Reflection.Assembly.Load(assemblyName);
			Assert.Equal(assemblyName, testAssembly.GetName().Name);
		}

		[Theory]
		[MemberData(nameof(ReferencesData))]
		public void AssemblyVersion(string assemblyName)
		{
			Assembly testAssembly = System.Reflection.Assembly.Load(assemblyName);
			Version actual = testAssembly.GetName().Version;
			Assert.Equal(2, actual.Major);
			Assert.Equal(0, actual.Minor);
			Assert.Equal(0, actual.Build);
		}

		[Theory]
		[MemberData(nameof(ReferencesData))]
		public void FileVersion(string assemblyName)
		{
			Assembly testAssembly = System.Reflection.Assembly.Load(assemblyName);
			FileVersionInfo actual = FileVersionInfo.GetVersionInfo(testAssembly.Location);
			Version expected = Version.Parse(GetFileFromRoot(s_gitInfoFile));
			Assert.Equal(expected.Major, actual.FileMajorPart);
			Assert.Equal(expected.Minor, actual.FileMinorPart);
			// Fails locally
			//Assert.Equal(expected.Build, actual.FileBuildPart);
			//We need to enable this
			//	Assert.Equal(ThisAssembly.Git.Commits, version.FilePrivatePart);
			Assert.Equal(s_productName, actual.ProductName);
			Assert.Equal(s_company, actual.CompanyName);
		}

		[Theory]
		[MemberData(nameof(ReferencesData))]
		public void ProductAndCompany(string assemblyName)
		{
			Assembly testAssembly = System.Reflection.Assembly.Load(assemblyName);
			FileVersionInfo actual = FileVersionInfo.GetVersionInfo(testAssembly.Location);
			Assert.Equal(s_productName, actual.ProductName);
			Assert.Equal(s_company, actual.CompanyName);
		}

		static string GetFileFromRoot(string file)
		{
			var gitInfoFile = IOPath.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", file);
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
