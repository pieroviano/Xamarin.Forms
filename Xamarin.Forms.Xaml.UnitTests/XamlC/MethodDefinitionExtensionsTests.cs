using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Xunit;
using Xamarin.Forms.Build.Tasks;
using Xamarin.Forms.Xaml.UnitTests;

namespace Xamarin.Forms.XamlcUnitTests
{
	public class MethodDefinitionExtensionsTests
	{
		public class NonGenericClass
		{
			public object Property { get; set; }
		}

		public class GenericClass<T, U, V>
		: IDisposable{
			public object NonGeneric() => default(object);
			public T GenericT() => default(T);
			public U GenericU() => default(U);
			public V GenericV() => default(V);
			public IEnumerable<T> EnumerableT() => default(IEnumerable<T>);
			public KeyValuePair<V, U> KvpVU() => default(KeyValuePair<V, U>);
		}

		ModuleDefinition module;

		public GenericClass()
{
			module = ModuleDefinition.CreateModule("foo", new ModuleParameters()
			{
				AssemblyResolver = new MockAssemblyResolver(),
				Kind = ModuleKind.Dll,
			});
		}

		public void Dispose()
{
			module?.Dispose();
		}

		[Fact]
		public void ResolveGenericReturnType()
		{
			var type = module.ImportReference(typeof(GenericClass<bool, string, int>));

			var getter = type.GetMethods(md => md.Name == "NonGeneric", module).Single();
			var returnType = getter.Item1.ResolveGenericReturnType(getter.Item2, module);
			Assert.Equal("System.Object", returnType.FullName);

			getter = type.GetMethods(md => md.Name == "GenericT", module).Single();
			returnType = getter.Item1.ResolveGenericReturnType(getter.Item2, module);
			Assert.Equal("System.Boolean", returnType.FullName);

			getter = type.GetMethods(md => md.Name == "GenericU", module).Single();
			returnType = getter.Item1.ResolveGenericReturnType(getter.Item2, module);
			Assert.Equal("System.String", returnType.FullName);

			getter = type.GetMethods(md => md.Name == "GenericV", module).Single();
			returnType = getter.Item1.ResolveGenericReturnType(getter.Item2, module);
			Assert.Equal("System.Int32", returnType.FullName);
		}
	}
}