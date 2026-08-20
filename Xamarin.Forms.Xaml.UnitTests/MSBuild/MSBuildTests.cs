using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Mono.Cecil;
using Xunit;
using static Xamarin.Forms.MSBuild.UnitTests.MSBuildXmlExtensions;
using IOPath = System.IO.Path;

namespace Xamarin.Forms.MSBuild.UnitTests
{
	//This set of tests is for validating Xamarin.Forms.targets
	[Trait("Category", "LongRunning")]
	public class MSBuildTests : IDisposable
	{
		static readonly string[] references = {
			"mscorlib",
			"System",
			"Xamarin.Forms.Core.dll",
			"Xamarin.Forms.Xaml.dll",
		};

		// Directory holding .NETFramework\v4.7\, supplied by the
		// Microsoft.NETFramework.ReferenceAssemblies.net47 package and handed to us through
		// assembly metadata by the csproj. The legacy-format half of these tests targets
		// .NET Framework v4.7, which has no targeting pack on Linux or macOS - without this
		// every one of those builds fails with MSB3644.
		static readonly string netFrameworkReferenceAssemblies =
			typeof(MSBuildTests).Assembly
				.GetCustomAttributes<AssemblyMetadataAttribute>()
				.FirstOrDefault(a => a.Key == "NetFrameworkReferenceAssemblies")
				?.Value;

		// Repository root, found by walking up from the test assembly until a solution file shows
		// up. The generated projects are no longer at a fixed depth below it (they build
		// under the OS temp directory), so _Directory.Build.[props|targets] cannot get here with
		// "..\..\.." any more - Build() passes this down as the XFRepoRoot property instead.
		// Forward slashes with a trailing one: MSBuild accepts them on every platform, and a
		// trailing backslash immediately before a closing quote would escape that quote on a
		// Windows command line.
		//
		// Matched by EXTENSION rather than by name. This used to look for "Xamarin.Forms.sln"
		// specifically; the prune in cc8874b9b deleted that solution and every test in this class
		// then failed in the type initializer with "Could not find Xamarin.Forms.sln". Globbing
		// survives the rename - and there is exactly one .sln in the tree, at the root, so this
		// cannot latch onto the wrong directory. (Directory.Build.props would: two project
		// directories have one of their own.)
		static readonly string repoRoot = FindRepoRoot();

		static string FindRepoRoot()
		{
			for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
			{
				if (dir.GetFiles("*.sln").Length > 0)
					return dir.FullName.Replace('\\', '/').TrimEnd('/') + "/";
			}
			// VSTS may run the tests from a staging directory that is not under the sources.
			var sourcesDirectory = Environment.GetEnvironmentVariable("BUILD_SOURCESDIRECTORY");
			if (!string.IsNullOrEmpty(sourcesDirectory))
				return sourcesDirectory.Replace('\\', '/').TrimEnd('/') + "/";

			throw new InvalidOperationException(
				$"Could not find a .sln above {AppContext.BaseDirectory}, and "
				+ "BUILD_SOURCESDIRECTORY is not set.");
		}

		class Xaml
		{
			const string XamarinFormsDefaultNamespace = "http://xamarin.com/schemas/2014/forms";
			const string XamarinFormsXNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

			public static readonly string MainPage = $@"
				<ContentPage
					xmlns=""{XamarinFormsDefaultNamespace}""
					xmlns:x=""{XamarinFormsXNamespace}""
					x:Class=""Xamarin.Forms.Xaml.UnitTests.MainPage"">
					<Label x:Name=""label0""/>
				</ContentPage>";

			public static readonly string CustomView = $@"
				<ContentView
					xmlns=""{XamarinFormsDefaultNamespace}""
					xmlns:x=""{XamarinFormsXNamespace}""
					x:Class=""Xamarin.Forms.Xaml.UnitTests.CustomView"">
					<Label x:Name=""label0""/>
				</ContentView>";
		}

		class Css{
			public const string Foo = @"
				label {
					color: azure;
					background-color: aliceblue;
				}";
		}

		string testDirectory;
		string tempDirectory;
		string intermediateDirectory;

		public MSBuildTests()
		{
			testDirectory = AppContext.BaseDirectory;
			// xUnit v3 exposes the running test through TestContext.Current; the display name
			// is qualified, so take the last segment to keep the old short directory names.
			var testName = TestContext.Current.Test?.TestDisplayName ?? Guid.NewGuid().ToString("N");
			testName = testName.Substring(testName.LastIndexOf('.') + 1);
			// xUnit's display name includes the argument list - "BuildAProject(sdkStyle: False)" -
			// so strip whitespace and punctuation as well as the OS-invalid characters, keeping
			// the directory name shell-safe. NUnit's Test.Name had no spaces.
			foreach (var c in IOPath.GetInvalidFileNameChars())
				testName = testName.Replace(c, '_');
			testName = new string(testName.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

			// The generated projects are built under the OS temp directory rather than under the
			// test assembly's own output directory. TargetsShouldSkip asserts that a second build
			// leaves XamlC.stamp untouched, and that only holds on a filesystem that keeps
			// sub-second file timestamps. A WSL /mnt/<drive> (drvfs) mount does not: MSBuild's
			// <Touch> writes a whole-second mtime there while ordinary writes keep nanoseconds,
			// so XamlC.stamp lands up to a second *before* the assembly it was stamped for and
			// the XamlC target is never up-to-date - it re-ran on every incremental build. The OS
			// temp directory is a native filesystem on every platform we run on (ext4 on Linux,
			// NTFS on Windows), which restores the precision the incrementality check needs.
			tempDirectory = IOPath.Combine(IOPath.GetTempPath(), "Xamarin.Forms.MSBuild.UnitTests", testName);
			intermediateDirectory = IOPath.Combine(tempDirectory, "obj", "Debug");
			// The temp directory now outlives `dotnet clean` and a bin\ wipe, and Dispose()
			// deliberately leaves it behind when a test fails, so start each test from an empty
			// one - otherwise a stale XamlC.stamp or obj\ from the previous run would be
			// mistaken for output of this one.
			if (Directory.Exists(tempDirectory))
				Directory.Delete(tempDirectory, true);
			Directory.CreateDirectory(tempDirectory);

			//copy _Directory.Build.[props|targets] in test/
			var props = IOPath.Combine(testDirectory, "..", "..", "..", "MSBuild", "_Directory.Build.props");
			var targets = IOPath.Combine(testDirectory, "..", "..", "..", "MSBuild", "_Directory.Build.targets");
			if (!File.Exists(props))
			{
				//NOTE: VSTS may be running tests in a staging directory, so we can use an environment variable to find the source
				//https://docs.microsoft.com/en-us/vsts/build-release/concepts/definitions/build/variables?view=vsts&tabs=batch#buildsourcesdirectory
				var sourcesDirectory = Environment.GetEnvironmentVariable("BUILD_SOURCESDIRECTORY");
				if (!string.IsNullOrEmpty(sourcesDirectory))
				{
					props = IOPath.Combine(sourcesDirectory, "Xamarin.Forms.Xaml.UnitTests", "MSBuild", "_Directory.Build.props");
					targets = IOPath.Combine(sourcesDirectory, "Xamarin.Forms.Xaml.UnitTests", "MSBuild", "_Directory.Build.targets");

					if (!File.Exists(props))
						Assert.Fail("Unable to find _Directory.Build.props at path: " + props);
				}
				else
					Assert.Fail("Unable to find _Directory.Build.props at path: " + props);

				Directory.CreateDirectory(IOPath.Combine(testDirectory, "..", "..", "..", "..", ".nuspec"));
				foreach (var file in Directory.GetFiles(IOPath.Combine(sourcesDirectory, ".nuspec"), "*.targets"))
					File.Copy(file, IOPath.Combine(testDirectory, "..", "..", "..", "..", ".nuspec", IOPath.GetFileName(file)), true);
				foreach (var file in Directory.GetFiles(IOPath.Combine(sourcesDirectory, ".nuspec"), "*.props"))
					File.Copy(file, IOPath.Combine(testDirectory, "..", "..", "..", "..", ".nuspec", IOPath.GetFileName(file)), true);
				File.Copy(IOPath.Combine(sourcesDirectory, "Directory.Build.props"), IOPath.Combine(testDirectory, "..", "..", "..", "..", "Directory.Build.props"), true);
			}

			File.Copy(props, IOPath.Combine(tempDirectory, "Directory.Build.props"), true);
			File.Copy(targets, IOPath.Combine(tempDirectory, "Directory.Build.targets"), true);

			// ...and the repository's NuGet.config with them. The generated projects import the
			// repo-root Directory.Build.props, which pulls in SourceLink.Build.props and its
			// *floating* <PackageReference Include="Microsoft.SourceLink.GitHub" Version="1.0.0-*" />.
			// A floating version is resolved against the configured feeds on every restore, so a
			// warm ~/.nuget/packages does not save it. Since these projects moved out of the
			// repository and under the OS temp directory, NuGet no longer walks up into the repo
			// root and so never sees its NuGet.config: restore then runs with whatever the
			// user-level config holds. On a machine whose user-level config has no nuget.org - a
			// Visual Studio install leaves only a disabled offline source there - every
			// sdkStyle:true case died in RestoreIfNeeded with
			//     error NU1101: Unable to find package Microsoft.SourceLink.GitHub.
			// Copying the repo config in makes the restore feed list a property of the repository
			// rather than of the machine, on every OS.
			var nugetConfig = IOPath.Combine(repoRoot, "NuGet.config");
			if (File.Exists(nugetConfig))
				File.Copy(nugetConfig, IOPath.Combine(tempDirectory, "NuGet.config"), true);
		}

		public void Dispose()
{
			// Leave log files behind on test failures (xUnit v3 equivalent of the old
			// TestContext.CurrentContext.Result.Outcome.Status check).
			if (TestContext.Current.TestState?.Result == TestResult.Failed)
				return;

			//NOTE: Windows can throw IOException: The process cannot access the file XYZ because it is being used by another process.
			//A simple retry-and-give-up approach should be good enough
			for (int i = 0; i < 3; i++)
			{
				try
				{
					if (Directory.Exists(tempDirectory))
					{
						Directory.Delete(tempDirectory, true);
					}
					break; //Success
				}
				catch (IOException)
				{
					System.Threading.Thread.Sleep(100);
				}
			}
		}

		/// <summary>
		/// Creates a base csproj file for these unit tests
		/// </summary>
		/// <param name="sdkStyle">If true, uses a new SDK-style project</param>
		XElement NewProject(bool sdkStyle)
		{
			var project = NewElement("Project");

			var propertyGroup = NewElement("PropertyGroup");
			if (sdkStyle)
			{
				project.WithAttribute("Sdk", "Microsoft.NET.Sdk");
				propertyGroup.Add(NewElement("TargetFramework").WithValue("netstandard2"));
				//NOTE: we don't want SDK-style projects to auto-add files, tests should be able to control this
				propertyGroup.Add(NewElement("EnableDefaultCompileItems").WithValue("False"));
				propertyGroup.Add(NewElement("EnableDefaultEmbeddedResourceItems").WithValue("False"));
				//NOTE: SDK-style output paths are different
				if (!intermediateDirectory.EndsWith("netstandard2"))
					intermediateDirectory = IOPath.Combine(intermediateDirectory, "netstandard2");
			}
			else
			{
				propertyGroup.Add(NewElement("Configuration").WithValue("Debug"));
				propertyGroup.Add(NewElement("Platform").WithValue("AnyCPU"));
				propertyGroup.Add(NewElement("OutputType").WithValue("Library"));
				propertyGroup.Add(NewElement("OutputPath").WithValue("bin\\Debug"));
				propertyGroup.Add(NewElement("TargetFrameworkVersion").WithValue("v4.7"));
				propertyGroup.Add(NewElement("RootNamespace").WithValue("test"));

				// Resolve mscorlib/System from the reference-assemblies NuGet package rather than
				// from a machine-wide targeting pack, so this half of the matrix builds on Linux
				// and macOS too. These three properties are exactly what the package's own
				// .targets sets for a v4.7 project; we set them by hand because the generated
				// project has no PackageReference machinery to import it. mscorlib is already in
				// `references`, which is what NoStdLib requires.
				if (!string.IsNullOrEmpty(netFrameworkReferenceAssemblies))
				{
					propertyGroup.Add(NewElement("TargetFrameworkRootPath").WithValue(netFrameworkReferenceAssemblies));
					propertyGroup.Add(NewElement("EnableFrameworkPathOverride").WithValue("false"));
					propertyGroup.Add(NewElement("NoStdLib").WithValue("true"));
				}
			}
			// Trailing separator is required - the targets append the task assembly's file name
			// straight onto it. Use the platform's, not a hard-coded backslash.
			propertyGroup.Add(NewElement("_XFBuildTasksLocation").WithValue(
				testDirectory.TrimEnd(IOPath.DirectorySeparatorChar, IOPath.AltDirectorySeparatorChar) + IOPath.DirectorySeparatorChar));


			project.Add(propertyGroup);

			var itemGroup = NewElement("ItemGroup");
			foreach (var assembly in references)
			{
				var reference = NewElement("Reference").WithAttribute("Include", assembly);
				if (assembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				{
					// Absolute: the generated project no longer sits two levels below the test
					// assembly's output directory, so "..\..\" cannot reach these any more.
					reference.Add(NewElement("HintPath").WithValue(IOPath.Combine(testDirectory, assembly)));
				}
				else if (sdkStyle)
				{
					//NOTE: SDK-style projects don't need system references
					continue;
				}
				itemGroup.Add(reference);
			}
			project.Add(itemGroup);

			//Let's enable XamlC assembly-wide
			project.Add(AddFile("AssemblyInfo.cs", "Compile", "[assembly: Xamarin.Forms.Xaml.XamlCompilation (Xamarin.Forms.Xaml.XamlCompilationOptions.Compile)]"));

			//Add a single CSS file
			project.Add(AddFile("Foo.css", "EmbeddedResource", Css.Foo));

			if (!sdkStyle)
				project.Add(NewElement("Import").WithAttribute("Project", @"$(MSBuildBinPath)\Microsoft.CSharp.targets"));
			return project;
		}

		XElement AddFile(string name, string buildAction, string contents)
		{
			var filePath = IOPath.Combine(tempDirectory, name.Replace('\\', IOPath.DirectorySeparatorChar).Replace('/', IOPath.DirectorySeparatorChar));
			Directory.CreateDirectory(IOPath.GetDirectoryName(filePath));
			File.WriteAllText(filePath, contents);
			var itemGroup = NewElement("ItemGroup");
			itemGroup.Add(NewElement(buildAction).WithAttribute("Include", name));
			return itemGroup;
		}

		// Previously this located a Visual Studio install via MSBuildLocator on Windows and
		// fell back to a bare "msbuild" elsewhere. `dotnet msbuild` ships with the SDK, takes
		// the same switches, and behaves identically on Windows, Linux and macOS - which is
		// what lets these tests run in the Linux CI at all.
		const string MSBuildExe = "dotnet";

		// projectFile is quoted: it is an absolute path under a per-test temp directory, and
		// an unquoted path containing a space makes MSBuild treat it as two positional
		// arguments ("MSB1008: Only one project can be specified").
		static string MSBuildArgs(string projectFile, string target, string verbosity, string additionalArgs) =>
			$"msbuild /v:{verbosity} /nologo \"{projectFile}\" /t:{target} /bl " +
			$"/p:XFRepoRoot=\"{repoRoot}\" {additionalArgs}";

		void RestoreIfNeeded(string projectFile, bool sdkStyle)
		{
			//If using an SDK-style project, we need to run the Restore target
			if (sdkStyle)
			{
				Build(projectFile, "Restore");
			}
		}

		string Build(string projectFile, string target = "Build", string verbosity = "normal", string additionalArgs = "", bool shouldSucceed = true)
		{
			var builder = new StringBuilder();
			void onData(object s, DataReceivedEventArgs e)
			{
				lock (builder)
					if (e.Data != null)
					{
						builder.AppendLine(e.Data);
						Console.WriteLine(e.Data);
					}
			};

			var psi = new ProcessStartInfo
			{
				FileName = MSBuildExe,
				Arguments = MSBuildArgs(projectFile, target, verbosity, additionalArgs),
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WorkingDirectory = tempDirectory,
			};
			using (var p = new Process { StartInfo = psi })
			{
				p.ErrorDataReceived += onData;
				p.OutputDataReceived += onData;

				p.Start();
				p.BeginErrorReadLine();
				p.BeginOutputReadLine();
				p.WaitForExit();
				if (shouldSucceed)
					Assert.Equal(0, p.ExitCode);
				else
					Assert.NotEqual(0, p.ExitCode);

				return builder.ToString();
			}
		}

		void AssertExists(string path, bool nonEmpty = false)
		{
			if (!File.Exists(path))
				Assert.Fail($"{path} should exist!");

			if (nonEmpty && new FileInfo(path).Length == 0)
				Assert.Fail($"{path} is empty!");
		}

		void AssertDoesNotExist(string path)
		{
			if (File.Exists(path))
				Assert.Fail($"{path} should *not* exist!");
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void BuildAProject(bool sdkStyle)
		{
			var project = NewProject(sdkStyle);
			project.Add(AddFile("MainPage.xaml", "EmbeddedResource", Xaml.MainPage));
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);
			Build(projectFile);

			AssertExists(IOPath.Combine(intermediateDirectory, "test.dll"), nonEmpty: true);
			AssertExists(IOPath.Combine(intermediateDirectory, "MainPage.xaml.g.cs"), nonEmpty: true);
			AssertExists(IOPath.Combine(intermediateDirectory, "Foo.css.g.cs"), nonEmpty: true);
			AssertExists(IOPath.Combine(intermediateDirectory, "XamlC.stamp"));
		}

		// Tests the XFXamlCValidateOnly=True MSBuild property
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void ValidateOnly(bool sdkStyle)
		{
			var project = NewProject(sdkStyle);
			project.Add(AddFile("MainPage.xaml", "EmbeddedResource", Xaml.MainPage));
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);
			Build(projectFile, additionalArgs: "/p:XFXamlCValidateOnly=True");

			var testDll = IOPath.Combine(intermediateDirectory, "test.dll");
			AssertExists(testDll, nonEmpty: true);
			using (var assembly = AssemblyDefinition.ReadAssembly(testDll))
			{
				// XAML files should remain as EmbeddedResource
				var resources = assembly.MainModule.Resources.OfType<EmbeddedResource>().Select(e => e.Name).ToArray();
				Assert.Contains("test.MainPage.xaml", resources);
			}
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void ValidateOnly_WithErrors(bool sdkStyle)
		{
			var project = NewProject(sdkStyle);
			project.Add(AddFile("MainPage.xaml", "EmbeddedResource", Xaml.MainPage.Replace("</ContentPage>", "<NotARealThing/></ContentPage>")));
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);

			string log = Build(projectFile, additionalArgs: "/p:XFXamlCValidateOnly=True", shouldSucceed: false);
			Assert.Contains("MainPage.xaml(7,6): XamlC error XFC0000: Cannot resolve type \"NotARealThing\".", log);
		}

		/// <summary>
		/// Tests that XamlG and XamlC targets skip, as well as checking IncrementalClean doesn't delete generated files
		/// </summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void TargetsShouldSkip(bool sdkStyle)
		{
			var project = NewProject(sdkStyle);
			project.Add(AddFile("MainPage.xaml", "EmbeddedResource", Xaml.MainPage));
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);
			Build(projectFile);

			var mainPageXamlG = IOPath.Combine(intermediateDirectory, "MainPage.xaml.g.cs");
			var fooCssG = IOPath.Combine(intermediateDirectory, "Foo.css.g.cs");
			var xamlCStamp = IOPath.Combine(intermediateDirectory, "XamlC.stamp");
			AssertExists(mainPageXamlG, nonEmpty: true);
			AssertExists(fooCssG, nonEmpty: true);
			AssertExists(xamlCStamp);

			var expectedXamlG = new FileInfo(mainPageXamlG).LastWriteTimeUtc;
			var expectdCssG = new FileInfo(fooCssG).LastWriteTimeUtc;
			var expectedXamlC = new FileInfo(xamlCStamp).LastWriteTimeUtc;

			//Build again
			Build(projectFile);
			AssertExists(mainPageXamlG, nonEmpty: true);
			AssertExists(xamlCStamp);

			var actualXamlG = new FileInfo(mainPageXamlG).LastWriteTimeUtc;
			var actualCssG = new FileInfo(fooCssG).LastWriteTimeUtc;
			var actualXamlC = new FileInfo(xamlCStamp).LastWriteTimeUtc;
			Assert.Equal(expectedXamlG, actualXamlG);
			Assert.Equal(expectdCssG, actualCssG);
			Assert.Equal(expectedXamlC, actualXamlC);
		}

		/// <summary>
		/// Checks that XamlG and XamlC files are cleaned
		/// </summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void Clean(bool sdkStyle)
		{
			var project = NewProject(sdkStyle);
			project.Add(AddFile("MainPage.xaml", "EmbeddedResource", Xaml.MainPage));
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);
			Build(projectFile);

			var mainPageXamlG = IOPath.Combine(intermediateDirectory, "MainPage.xaml.g.cs");
			var fooCssG = IOPath.Combine(intermediateDirectory, "Foo.css.g.cs");
			var xamlCStamp = IOPath.Combine(intermediateDirectory, "XamlC.stamp");
			AssertExists(mainPageXamlG, nonEmpty: true);
			AssertExists(fooCssG, nonEmpty: true);
			AssertExists(xamlCStamp);

			//Clean
			Build(projectFile, "Clean");
			AssertDoesNotExist(mainPageXamlG);
			AssertDoesNotExist(fooCssG);
			AssertDoesNotExist(xamlCStamp);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void LinkedFile(bool sdkStyle)
		{
			var folder = IOPath.Combine(tempDirectory, "A", "B");
			Directory.CreateDirectory(folder);
			File.WriteAllText(IOPath.Combine(folder, "MainPage.xaml"), Xaml.MainPage);

			var project = NewProject(sdkStyle);
			var itemGroup = NewElement("ItemGroup");
			var embeddedResource = NewElement("EmbeddedResource").WithAttribute("Include", @"A\B\MainPage.xaml");
			embeddedResource.Add(NewElement("Link").WithValue(@"Pages\MainPage.xaml"));
			itemGroup.Add(embeddedResource);
			project.Add(itemGroup);
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);
			Build(projectFile);

			AssertExists(IOPath.Combine(intermediateDirectory, "test.dll"), nonEmpty: true);
			AssertExists(IOPath.Combine(intermediateDirectory, "Pages", "MainPage.xaml.g.cs"), nonEmpty: true);
			AssertExists(IOPath.Combine(intermediateDirectory, "XamlC.stamp"));
		}

		const string BuildTasksPackageId = "Net4x.Xamarin.Forms.Build.Tasks";

		/// <summary>
		/// The one thing every other test in this class cannot see: whether the PACKAGE works.
		/// </summary>
		/// <remarks>
		/// Every case above points <c>_XFBuildTasksLocation</c> at this test assembly's own output
		/// directory, which holds the whole of <c>$(TargetDir)</c> - so the tasks always find
		/// Xamarin.Forms.Core.dll and Xamarin.Forms.Xaml.dll next to themselves, whatever the nupkg
		/// contains. XamlCTask and XamlGTask genuinely bind both at build time, so a package that
		/// ships neither them nor a NuGet dependency that could supply them fails with
		/// FileNotFoundException on the first .xaml file, and nothing in the tree noticed.
		///
		/// <para>This restores the produced package the way a consumer does - local feed, plain
		/// <c>PackageReference</c>, NuGet's own props/targets auto-import - and compiles a XAML file
		/// with it. The generated project deliberately gets EMPTY Directory.Build.[props|targets]:
		/// the repository's set <c>_XFBuildTasksLocation</c> to artifacts\build-tasks\ and import
		/// Xamarin.Forms.targets directly, either of which would hide exactly what is under test
		/// (and the second would fail XF001 for a double import).</para>
		///
		/// <para><c>RestorePackagesPath</c> is redirected into the temp directory rather than left
		/// on ~/.nuget/packages. PackageVersion is static at 5.0.0 here, so a globally cached
		/// extraction of an EARLIER build of the same version would be reused and this test would
		/// grade a stale package.</para>
		/// </remarks>
		[Fact]
		public void PackagedBuildTasksCompileXaml()
		{
			var packagesDirectory = IOPath.Combine(repoRoot, "Packages");
			var nupkg = Directory.Exists(packagesDirectory)
				? new DirectoryInfo(packagesDirectory)
					.GetFiles(BuildTasksPackageId + ".*.nupkg")
					.OrderByDescending(f => f.LastWriteTimeUtc)
					.FirstOrDefault()
				: null;

			if (nupkg == null)
				Assert.Fail($"No {BuildTasksPackageId}.*.nupkg in {packagesDirectory}. It is produced by " +
					"Xamarin.Forms.Build.Tasks (GeneratePackageOnBuild=True), so build the solution before " +
					"running this test.");

			var version = IOPath.GetFileNameWithoutExtension(nupkg.Name).Substring(BuildTasksPackageId.Length + 1);
			var restorePackagesPath = IOPath.Combine(tempDirectory, "packages");

			// The repository's Directory.Build.props/targets were copied in by the constructor for
			// every other test here; this one has to be a stranger to the tree.
			File.WriteAllText(IOPath.Combine(tempDirectory, "Directory.Build.props"), "<Project />");
			File.WriteAllText(IOPath.Combine(tempDirectory, "Directory.Build.targets"), "<Project />");

			// <clear /> so the local drop is the only place this package can come from, and
			// nuget.org for the implicit NETStandard.Library reference.
			File.WriteAllText(IOPath.Combine(tempDirectory, "NuGet.config"), $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""xf-local"" value=""{packagesDirectory}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" protocolVersion=""3"" />
  </packageSources>
</configuration>");

			var project = NewProject(sdkStyle: true);

			// NewProject hands every project the in-tree task drop. Removing it is the whole point:
			// the package's own build\Xamarin.Forms.targets defaults it to build\netstandard2.0\.
			project.Descendants()
				.Where(e => e.Name.LocalName == "_XFBuildTasksLocation")
				.Remove();

			var propertyGroup = NewElement("PropertyGroup");
			propertyGroup.Add(NewElement("RestorePackagesPath").WithValue(restorePackagesPath));
			project.Add(propertyGroup);

			var itemGroup = NewElement("ItemGroup");
			itemGroup.Add(NewElement("PackageReference")
				.WithAttribute("Include", BuildTasksPackageId)
				.WithAttribute("Version", version));
			project.Add(itemGroup);

			project.Add(AddFile("MainPage.xaml", "EmbeddedResource", Xaml.MainPage));

			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			Build(projectFile, "Restore");

			// Asserted before the build so a missing assembly is named outright, rather than
			// arriving as an MSB4062/FileNotFoundException buried in the log.
			var packagedTasks = IOPath.Combine(restorePackagesPath, BuildTasksPackageId.ToLowerInvariant(),
				version, "build", "netstandard2.0");
			AssertExists(IOPath.Combine(packagedTasks, "Xamarin.Forms.Build.Tasks.dll"), nonEmpty: true);
			AssertExists(IOPath.Combine(packagedTasks, "Xamarin.Forms.Core.dll"), nonEmpty: true);
			AssertExists(IOPath.Combine(packagedTasks, "Xamarin.Forms.Xaml.dll"), nonEmpty: true);

			Build(projectFile);

			AssertExists(IOPath.Combine(intermediateDirectory, "test.dll"), nonEmpty: true);
			AssertExists(IOPath.Combine(intermediateDirectory, "MainPage.xaml.g.cs"), nonEmpty: true);
			AssertExists(IOPath.Combine(intermediateDirectory, "XamlC.stamp"));
		}

		//https://github.com/dotnet/project-system/blob/master/docs/design-time-builds.md
		//https://daveaglick.com/posts/running-a-design-time-build-with-msbuild-apis
		[Theory]
		[InlineData(false)]
		public void DesignTimeBuild(bool sdkStyle)
		{
			var project = NewProject(sdkStyle);
			project.Add(AddFile(@"Pages\MainPage.xaml", "EmbeddedResource", Xaml.MainPage));
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);

			// "Compile" on every OS now. The old code asked for "CompileDesignTime" on Windows,
			// which is defined by the *full* MSBuild that ships with Visual Studio; the SDK's
			// MSBuild - which is what `dotnet msbuild` runs, and what this harness has driven
			// since MSBuildLocator was dropped - does not define it, so that branch failed with
			//     error MSB4057: The target "CompileDesignTime" does not exist in the project.
			// Nothing is lost: CompileDesignTime is Compile plus exactly the design-time
			// properties passed below, and those are what the test is actually asserting on.
			Build(projectFile, "Compile", additionalArgs: "/p:DesignTimeBuild=True /p:BuildingInsideVisualStudio=True /p:SkipCompilerExecution=True /p:ProvideCommandLineArgs=True");

			var assembly = IOPath.Combine(intermediateDirectory, "test.dll");
			var mainPageXamlG = IOPath.Combine(intermediateDirectory, "Pages", "MainPage.xaml.g.cs");
			var fooCssG = IOPath.Combine(intermediateDirectory, "Foo.css.g.cs");
			var xamlCStamp = IOPath.Combine(intermediateDirectory, "XamlC.stamp");

			//The assembly should not be compiled
			AssertDoesNotExist(assembly);
			AssertExists(mainPageXamlG, nonEmpty: true);
			AssertExists(fooCssG, nonEmpty: true);
			AssertDoesNotExist(xamlCStamp); //XamlC should be skipped

			var expectedXamlG = new FileInfo(mainPageXamlG).LastWriteTimeUtc;
			var expectedCssG = new FileInfo(fooCssG).LastWriteTimeUtc;

			//Build again, a full build
			Build(projectFile);
			AssertExists(assembly, nonEmpty: true);
			AssertExists(mainPageXamlG, nonEmpty: true);
			AssertExists(fooCssG, nonEmpty: true);
			AssertExists(xamlCStamp);

			var actualXamlG = new FileInfo(mainPageXamlG).LastWriteTimeUtc;
			var actualCssG = new FileInfo(fooCssG).LastWriteTimeUtc;
			Assert.Equal(expectedXamlG, actualXamlG);
			Assert.Equal(expectedCssG, actualCssG);
		}

		//I believe the designer might invoke this target manually
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void UpdateDesignTimeXaml(bool sdkStyle)
		{
			var project = NewProject(sdkStyle);
			project.Add(AddFile(@"Pages\MainPage.xaml", "EmbeddedResource", Xaml.MainPage));
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);
			Build(projectFile, "UpdateDesignTimeXaml");

			AssertExists(IOPath.Combine(intermediateDirectory, "Pages", "MainPage.xaml.g.cs"), nonEmpty: true);
			AssertDoesNotExist(IOPath.Combine(intermediateDirectory, "Foo.css.g.cs"));
			AssertDoesNotExist(IOPath.Combine(intermediateDirectory, "XamlC.stamp"));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void AddNewFile(bool sdkStyle)
		{
			var project = NewProject(sdkStyle);
			project.Add(AddFile("MainPage.xaml", "EmbeddedResource", Xaml.MainPage));
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);
			Build(projectFile);

			var mainPageXamlG = IOPath.Combine(intermediateDirectory, "MainPage.xaml.g.cs");
			var customViewXamlG = IOPath.Combine(intermediateDirectory, "CustomView.xaml.g.cs");
			var fooCssG = IOPath.Combine(intermediateDirectory, "Foo.css.g.cs");
			var xamlCStamp = IOPath.Combine(intermediateDirectory, "XamlC.stamp");
			AssertExists(mainPageXamlG, nonEmpty: true);
			AssertExists(xamlCStamp);

			var expectedXamlG = new FileInfo(mainPageXamlG).LastWriteTimeUtc;
			var expectedCssG = new FileInfo(fooCssG).LastWriteTimeUtc;
			var expectedXamlC = new FileInfo(xamlCStamp).LastWriteTimeUtc;

			//Build again, after adding a file, this triggers a full XamlG and XamlC -- *not* CssG
			project.Add(AddFile("CustomView.xaml", "EmbeddedResource", Xaml.CustomView));
			project.Save(projectFile);
			Build(projectFile);
			AssertExists(mainPageXamlG, nonEmpty: true);
			AssertExists(customViewXamlG, nonEmpty: true);
			AssertExists(xamlCStamp);

			var actualXamlG = new FileInfo(mainPageXamlG).LastWriteTimeUtc;
			var actualCssG = new FileInfo(fooCssG).LastWriteTimeUtc;
			var actualXamlC = new FileInfo(xamlCStamp).LastWriteTimeUtc;
			var actualNewFile = new FileInfo(customViewXamlG).LastAccessTimeUtc;
			Assert.NotEqual(expectedXamlG, actualXamlG);
			Assert.NotEqual(expectedXamlG, actualNewFile);
			Assert.Equal(expectedCssG, actualCssG);
			Assert.NotEqual(expectedXamlC, actualXamlC);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void TouchXamlFile(bool sdkStyle)
		{
			var project = NewProject(sdkStyle);
			project.Add(AddFile("MainPage.xaml", "EmbeddedResource", Xaml.MainPage));
			project.Add(AddFile("CustomView.xaml", "EmbeddedResource", Xaml.CustomView));
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);
			Build(projectFile);

			var mainPageXamlG = IOPath.Combine(intermediateDirectory, "MainPage.xaml.g.cs");
			var customViewXamlG = IOPath.Combine(intermediateDirectory, "CustomView.xaml.g.cs");
			var xamlCStamp = IOPath.Combine(intermediateDirectory, "XamlC.stamp");
			AssertExists(mainPageXamlG, nonEmpty: true);
			AssertExists(customViewXamlG, nonEmpty: true);
			AssertExists(xamlCStamp);

			var expectedMainPageXamlG = new FileInfo(mainPageXamlG).LastWriteTimeUtc;
			var expectedCustomViewXamlG = new FileInfo(customViewXamlG).LastWriteTimeUtc;
			var expectedXamlC = new FileInfo(xamlCStamp).LastWriteTimeUtc;

			//Build again, after modifying the timestamp on a Xaml file, should trigger a partial XamlG and full XamlC
			//https://github.com/pieroviano/xamarin-android/blob/61851599fb1999964bd200ec1c373b6e395933f3/src/Xamarin.Android.Build.Tasks/Utilities/MonoAndroidHelper.cs#L342
			File.SetLastWriteTimeUtc(customViewXamlG, expectedCustomViewXamlG.AddDays(1));
			File.SetLastAccessTimeUtc(customViewXamlG, expectedCustomViewXamlG.AddDays(1));
			Build(projectFile);
			AssertExists(mainPageXamlG, nonEmpty: true);
			AssertExists(customViewXamlG, nonEmpty: true);
			AssertExists(xamlCStamp);

			var actualMainPageXamlG = new FileInfo(mainPageXamlG).LastWriteTimeUtc;
			var actualCustomViewXamlG = new FileInfo(customViewXamlG).LastAccessTimeUtc;
			var actualXamlC = new FileInfo(xamlCStamp).LastWriteTimeUtc;
			Assert.Equal(expectedMainPageXamlG, actualMainPageXamlG);
			Assert.NotEqual(expectedMainPageXamlG, actualCustomViewXamlG);
			Assert.NotEqual(expectedXamlC, actualXamlC);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void RandomXml(bool sdkStyle)
		{
			var project = NewProject(sdkStyle);
			project.Add(AddFile("MainPage.xaml", "EmbeddedResource", "<xml></xml>"));
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);
			Build(projectFile);

			AssertExists(IOPath.Combine(intermediateDirectory, "test.dll"), nonEmpty: true);
			AssertExists(IOPath.Combine(intermediateDirectory, "MainPage.xaml.g.cs"));
			AssertExists(IOPath.Combine(intermediateDirectory, "XamlC.stamp"));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void InvalidXml(bool sdkStyle)
		{
			var project = NewProject(sdkStyle);
			project.Add(AddFile("MainPage.xaml", "EmbeddedResource", "notxmlatall"));
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);
			// ThrowsAny, not Throws: Assert.Throws<T> demands an exact type match, and the
			// assertion inside Build() raises a derived type (EqualException). NUnit's
			// Assert.Throws<AssertionException> matched derived types.
			Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Build(projectFile));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void RandomEmbeddedResource(bool sdkStyle)
		{
			var project = NewProject(sdkStyle);
			project.Add(AddFile("MainPage.xaml", "EmbeddedResource", Xaml.MainPage));
			project.Add(AddFile("MainPage.txt", "EmbeddedResource", "notxmlatall"));
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);
			Build(projectFile);

			AssertExists(IOPath.Combine(intermediateDirectory, "test.dll"), nonEmpty: true);
			AssertDoesNotExist(IOPath.Combine(intermediateDirectory, "MainPage.txt.g.cs"));
			AssertExists(IOPath.Combine(intermediateDirectory, "XamlC.stamp"));
		}

		/// <summary>
		/// XamlC must not run at all when the project contains no .xaml files.
		///
		/// This used to be asserted by scraping a /v:diagnostic log for
		/// `Target "XamlC" skipped`. MSBuild 18.x (.NET 10 SDK) no longer logs a target that is
		/// skipped because its Condition evaluated false - not at diagnostic verbosity and not
		/// into a binlog either (verified against 18.6.11 with a two-target repro project), so
		/// that string can never appear again and the assertion could only ever fail.
		///
		/// It is now asserted on the target's observable effect instead. XamlC's sole outputs
		/// are the rewritten assembly and the XamlC.stamp it Touch-es, so an absent stamp after
		/// a successful build means the target did not execute. The CssG output is checked in
		/// the same breath as a positive control: it proves Xamarin.Forms.targets really was
		/// imported and its targets really did run, so the missing stamp is XamlC being skipped
		/// and not the whole targets file being absent.
		/// </summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void NoXamlFiles(bool sdkStyle)
		{
			var project = NewProject(sdkStyle);
			var projectFile = IOPath.Combine(tempDirectory, "test.csproj");
			project.Save(projectFile);
			RestoreIfNeeded(projectFile, sdkStyle);
			Build(projectFile);

			AssertExists(IOPath.Combine(intermediateDirectory, "test.dll"), nonEmpty: true);
			//positive control: the Xamarin.Forms targets ran
			AssertExists(IOPath.Combine(intermediateDirectory, "Foo.css.g.cs"), nonEmpty: true);
			//XamlC should be skipped if there are no .xaml files
			AssertDoesNotExist(IOPath.Combine(intermediateDirectory, "XamlC.stamp"));
		}
	}
}
