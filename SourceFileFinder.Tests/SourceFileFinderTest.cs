using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using ReflectionHelpers.Cases;
using ReflectionHelpers.Cases.SubCases;
using Shouldly;
using Xunit;
using Xunit.Sdk;
using static ReflectionHelpers.Cases.OuterClass;
using static ReflectionHelpers.Cases.SubCases.OuterClass;

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


        [Theory(DisplayName = "Find(type) can find source file for class")]
        [InlineData(typeof(EmptyClass))]
        [InlineData(typeof(ClassWithLineHidden))]
        [InlineData(typeof(EmptyClassInNestedNamespace))]
        [InlineData(typeof(NestedEmptyClass))]
        [InlineData(typeof(NestedEmptyClassInNestedNamespace))]
        [InlineData(typeof(ClassWithoutNamespace))]
        [InlineData(typeof(EmptyClassWithoutNamespace))]        
        [InlineData(typeof(ClassWithCtor))]
        [InlineData(typeof(PublicMethodClass))]
        [InlineData(typeof(OverriddenPublicMethodClass))]
        [InlineData(typeof(ProtectedMethodClass))]
        [InlineData(typeof(OverriddenProtectedMethodClass))]
        [InlineData(typeof(PrivateMethodClass))]
        public void FindsFiles(Type target)
        {
            using var sut = CreateSut();

            var result = sut.Find(target);

            result.Single()
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
            result.ShouldContain(file => file.EndsWith(@$"SourceFileFinder.Tests\Cases\{target.Name}.1.cs"));
            result.ShouldContain(file => file.EndsWith(@$"SourceFileFinder.Tests\Cases\{target.Name}.2.cs"));
        }

        // No portable PDB file was found for assembly
        // test with pdbonly and Deterministic  builds
    }
}
