using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Xunit;
using Xamarin.Forms.Build.Tasks;
using IOPath = System.IO.Path;

namespace Xamarin.Forms.Xaml.UnitTests
{
	public class CecilExtensionsTests : IAssemblyResolver, IDisposable{
		const string testNamespace = "Xamarin.Forms.Xaml.UnitTests";
		AssemblyDefinition assembly;
		readonly List<AssemblyDefinition> assemblies = new List<AssemblyDefinition>();
		readonly ReaderParameters readerParameters;

		// The former [SetUp] body is merged in here: the class already had a constructor, and
		// xUnit's per-test instance means the constructor IS the setup step.
		public CecilExtensionsTests()
		{
			readerParameters = new ReaderParameters
			{
				AssemblyResolver = this,
			};

			assembly = AssemblyDefinition.ReadAssembly(GetType().Assembly.Location, readerParameters);
			assemblies.Add(assembly);
		}

		public AssemblyDefinition Resolve(AssemblyNameReference name)
		{
			var path = IOPath.Combine(IOPath.GetDirectoryName(GetType().Assembly.Location), name.Name + ".dll");
			var assembly = AssemblyDefinition.ReadAssembly(path, readerParameters);
			assemblies.Add(assembly);
			return assembly;
		}

		public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
		{
			var path = IOPath.Combine(IOPath.GetDirectoryName(GetType().Assembly.Location), name.Name + ".dll");
			var assembly = AssemblyDefinition.ReadAssembly(path, parameters);
			assemblies.Add(assembly);
			return assembly;
		}

		public void Dispose()
{
			foreach (var assembly in assemblies)
			{
				assembly.Dispose();
			}
			assemblies.Clear();
		}

		EmbeddedResource GetResource(string name)
		{
			var resourceName = $"{testNamespace}.{name}.xaml";
			foreach (EmbeddedResource res in assembly.MainModule.Resources)
			{
				if (res.Name == resourceName)
					return res;
			}
			throw new InvalidOperationException($"Resource '{resourceName}' not found in assembly '{assembly.Name.Name}'.");
		}

		static string[] IsXamlTrueSource = new[]
		{
			"IsCompiledDefault",
			"X2006Namespace",
			"X2009Primitives",
		};

		public static IEnumerable<object[]> IsXamlTrueData => IsXamlTrueSource.Select(x => new object[] { x });

		[Theory]
		[MemberData(nameof(IsXamlTrueData))]
		public void IsXamlTrue(string name)
		{
			var resource = GetResource(name);
			Assert.True(resource.IsXaml(assembly.MainModule, out string className), $"IsXaml should return true for '{name}'.");
			Assert.Equal(className, $"{testNamespace}.{name}"); // Test cases x:Class matches the file name
		}

		static string[] IsXamlFalseSource = new[]
		{
			"Validation.MissingXClass",
			"Validation.NotXaml",
		};

		public static IEnumerable<object[]> IsXamlFalseData => IsXamlFalseSource.Select(x => new object[] { x });

		[Theory]
		[MemberData(nameof(IsXamlFalseData))]
		public void IsXamlFalse(string name)
		{
			var resource = GetResource(name);
			Assert.False(resource.IsXaml(assembly.MainModule, out _), $"IsXaml should return false for '{name}'.");
		}
	}
}
