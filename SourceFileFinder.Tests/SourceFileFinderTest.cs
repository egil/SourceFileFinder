using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using ReflectionHelpers.Cases;
using Shouldly;
using Xunit;
using Xunit.Sdk;

namespace ReflectionHelpers
{
    public class SourceFileFinderTest
    {
        private static readonly Assembly ThisAssembly = typeof(SourceFileFinderTest).Assembly;

        private static SourceFileFinder CreateSut(Assembly? assemblyToSearch = null)
            => new SourceFileFinder(assemblyToSearch ?? ThisAssembly);

        [Fact(DisplayName = "Find(type) returns false when source file does not exist on the local file system")]
        public void Test001()
        {
            var target = typeof(XunitTestCase);
            using var sut = CreateSut(target.Assembly);

            var result = sut.Find(typeof(XunitTestCase));

            result.ShouldBeEmpty();
        }

        [Fact(DisplayName = "Find(null) throws when method or type passed to it is null")]
        public void Test003()
        {
            using var sut = CreateSut();

            Should.Throw<ArgumentNullException>(() => sut.Find(default(Type)!))
                .ParamName.ShouldNotBeEmpty();
        }

        [Fact(DisplayName = "The search assembly is available through the SearchAssembly property")]
        public void Test004()
        {
            using var sut = CreateSut(ThisAssembly);

            sut.SearchAssembly.ShouldBe(ThisAssembly);
        }

        [Fact(DisplayName = "SourceFileFinder ctor throws when searchAssembly is missing")]
        public void Test005()
        {
            Should.Throw<ArgumentNullException>(() => new SourceFileFinder(null!))
                .ParamName.ShouldNotBeEmpty();
        }

        [Fact(DisplayName = "Find(type) throws when type passed to it does not belong to SerachAssembly")]
        public void Test006()
        {
            var target = typeof(XunitTestCase);
            using var sut = CreateSut();

            Should.Throw<InvalidOperationException>(() => sut.Find(target));
        }

        // No portable PDB file was found for assembly
        // The method's DeclaringType property is null

        [Theory(DisplayName = "Find(type) can find source file for class with one or more methods")]
        [InlineData(typeof(PublicMethodClass))]
        [InlineData(typeof(OverriddenPublicMethodClass))]
        [InlineData(typeof(ProtectedMethodClass))]
        [InlineData(typeof(OverriddenProtectedMethodClass))]
        [InlineData(typeof(PrivateMethodClass))]
        public void Test100(Type target)
        {
            using var sut = CreateSut();

            var result = sut.Find(target);

            result.Single()
                .FullName
                .ShouldEndWith($@"SourceFileFinder.Tests\Cases\{target.Name}.cs");
        }

        [Fact(DisplayName = "Find(type), where type is partial with a method in each partial class, " +
                            " returns all source files")]
        public void Test110()
        {
            var target = typeof(PartialClassWithMethod);
            using var sut = CreateSut();

            var result = sut.Find(target);

            result.Count.ShouldBe(2);
            result.ShouldContain(file => file.FullName.EndsWith(@$"SourceFileFinder.Tests\Cases\{target.Name}.1.cs"));
            result.ShouldContain(file => file.FullName.EndsWith(@$"SourceFileFinder.Tests\Cases\{target.Name}.2.cs"));
        }

        // Find(type), where type has no methods, finds source file, when there are no two types in the assembly with same name
    }
}
