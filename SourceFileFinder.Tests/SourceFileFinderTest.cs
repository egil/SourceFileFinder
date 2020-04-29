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

            Should.Throw<ArgumentNullException>(() => sut.Find(default!))
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
                .ShouldEndWith($@"SourceFileFinder.Tests{Path.DirectorySeparatorChar}Cases{Path.DirectorySeparatorChar}{target.Name}.cs");
        }

        [Fact(DisplayName = "Find(type), where type is partial with a method in each partial class, " +
                            " returns all source files")]
        public void Test110()
        {
            var target = typeof(PartialClassWithMethod);
            using var sut = CreateSut();

            var result = sut.Find(target);

            result.Count.ShouldBe(2);
            result.ShouldContain(file => file.EndsWith(@$"SourceFileFinder.Tests{Path.DirectorySeparatorChar}Cases{Path.DirectorySeparatorChar}{target.Name}.1.cs"));
            result.ShouldContain(file => file.EndsWith(@$"SourceFileFinder.Tests{Path.DirectorySeparatorChar}Cases{Path.DirectorySeparatorChar}{target.Name}.2.cs"));
        }

        // Tests TODO:
        // - Create test of classes with sequence points in documents (you can make one. just switch #line's back and forth a few times or just use #line hidden in the middle and then restore to non hidden)
		//   it may be required that you add #line somefakeline "somefakefile" too. I believe hidden lines create sequence points but this is still a single document
		//   "somefakefile" should be a full path.
        // - No portable PDB file was found for assembly
        // - Partial class without method in one or more files
        // - Multiple classes in same file
        // - Create test with assembly compiled using full and pdb-only format. Most likely need a windows pdb reader (https://github.com/dotnet/symreader)
        
        // none = no debug data
        // pdbonly = Windows PDB format in a .pdb file
        // full = Windows PDB format embedded in the .dll file (this is in the legacy csproj default template, I think)
        // portable = IL PDB format in a .pdb file (this is the .NET SDK default)
        // embedded = IL PDB format embedded in the .dll file
    }
}
