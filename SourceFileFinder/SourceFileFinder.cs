using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ReflectionHelpers
{

    /// <summary>
    /// Represents a helper utility that can find a source file for 
    /// a type or method that is a part of a compiled assembly.
    /// </summary>
    public class SourceFileFinder : IDisposable
    {
        private static readonly Guid CSharpLanguage = Guid.Parse("3f5162f8-07c6-11d3-9053-00c04fa302a1");
        private static readonly Guid FSharpLanguage = Guid.Parse("ab4f38c9-b6e6-43ba-be3b-58080b2ccce3");
        private static readonly Guid VBLanguage = Guid.Parse("3a12d0b8-c26c-11d0-b442-00a0244a1dd2");

        private readonly CSharpTypeLocator _csharpTypeLocator = new CSharpTypeLocator();

        private FileStream? _dllFileReader = null;
        private FileStream? _pdbFileReader = null;
        private PEReader? _pEReader = null;
        private MetadataReaderProvider? _metadataReaderProvider = null;
        private MetadataReader? _metadataReader = null;
        private MetadataReader? _pdbReader = null;

        /// <summary>
        /// Gets the MetadataReader for the <see cref="SearchAssembly"/>.
        /// </summary>
        protected MetadataReader MetadataReader
        {
            get
            {
                if (_metadataReader is null)
                    Initialize();

                return _metadataReader!; // BANG! because Initialized() should set _metadateReader.
            }
        }

        /// <summary>
        /// Gets the MetadataReader for the PDB for the <see cref="SearchAssembly"/>.
        /// </summary>
        protected MetadataReader PdbReader
        {
            get
            {
                if (_pdbReader is null)
                    Initialize();

                return _pdbReader!; // BANG! because Initialized() should set _pdbReader.
            }
        }

        /// <summary>
        /// Gets the assembly being used when trying to find source files.
        /// </summary>
        public Assembly SearchAssembly { get; }

        /// <summary>
        /// Creates an instance of the <see cref="SourceFileFinder"/>.
        /// </summary>
        /// <param name="searchAssembly">The assembly to use when searching for the source file.</param>
        public SourceFileFinder(Assembly searchAssembly)
        {
            SearchAssembly = searchAssembly ?? throw new ArgumentNullException(nameof(searchAssembly));
        }

        /// <summary>
        /// Will attempt to find the local source file(s) where the <paramref name="target"/> is declared. 
        /// </summary>
        /// <param name="target">Type whose source to attempt to find.</param>
        /// <returns>A list of files the <paramref name="target"/> type is defined in.</returns>
        public IReadOnlyList<string> Find(Type target)
        {
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (target.Assembly != SearchAssembly)
                throw new InvalidOperationException($"The type '{target.FullName}' does not belong to finder's search assembly '{SearchAssembly.FullName}'.");

            var result = new List<string>();

            var typeDefinition = GetTypeDefinition(target);

            FindFilesViaMethods(typeDefinition, result);

            // TODO: Detect if FindFilesViaDocuments is needed, e.g. if type is partial or result is empty.
            if (result.Count == 0)
                FindFilesViaDocuments(target, result);

            return result;
        }

        private TypeDefinition GetTypeDefinition(Type target)
        {
            var typeDefinitionHandle = (TypeDefinitionHandle)MetadataTokens.Handle(target.GetMetadataToken());
            return MetadataReader.GetTypeDefinition(typeDefinitionHandle);
        }

        private void FindFilesViaMethods(TypeDefinition typeDefinition, List<string> result)
        {

            foreach (var handle in typeDefinition.GetMethods())
            {
                if (handle.IsNil)
                    continue;

                var methodDebugInformation = PdbReader.GetMethodDebugInformation(handle);
                if (methodDebugInformation.Document.IsNil)
                    continue;

                var doc = PdbReader.GetDocument(methodDebugInformation.Document);
                if (doc.Name.IsNil)
                    continue;

                var filename = PdbReader.GetString(doc.Name);

                if (result.Contains(filename))
                    continue;

                if (!File.Exists(filename))
                    continue;

                result.Add(filename);
            }
        }

        private void FindFilesViaDocuments(Type target, List<string> result)
        {
            foreach (var handle in PdbReader.Documents)
            {
                if (handle.IsNil)
                    continue;

                var doc = PdbReader.GetDocument(handle);

                if (doc.Name.IsNil)
                    continue;

                if (doc.Language.IsNil)
                    continue;

                var language = PdbReader.GetGuid(doc.Language);
                var filename = PdbReader.GetString(doc.Name);

                if (result.Contains(filename))
                    continue;

                if (language == CSharpLanguage && _csharpTypeLocator.CsharpDocumentContainsType(filename, target))
                    result.Add(filename);

                if (language == VBLanguage)
                    throw new NotImplementedException("Support for Visual Basic not implemented yet.");
                if (language == FSharpLanguage)
                    throw new NotImplementedException("Support for F# not implemented yet.");
            }
        }

        private void Initialize()
        {
            if (_dllFileReader is null)
                _dllFileReader = File.OpenRead(SearchAssembly.Location);

            if (_pEReader is null)
                _pEReader = new PEReader(_dllFileReader);

            if (_metadataReader is null && _pdbReader is null)
            {
                bool pdbFound = false;
                try
                {
                    pdbFound = _pEReader.TryOpenAssociatedPortablePdb(SearchAssembly.Location,
                        pdbPath =>
                        {
                            _pdbFileReader = File.OpenRead(pdbPath);
                            return _pdbFileReader;
                        },
                        out var metadataReaderProvider,
                        out var pdbPath);


                    _metadataReaderProvider = metadataReaderProvider;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"An error occurred while searching for the PDB for the assembly '{SearchAssembly.FullName}' with path '{SearchAssembly.Location}'.", ex);
                }

                if (!pdbFound)
                    throw new InvalidOperationException($"No portable PDB was found for the assembly '{SearchAssembly.FullName}' with path '{SearchAssembly.Location}'.");

                _metadataReader = _pEReader.GetMetadataReader();
                _pdbReader = _metadataReaderProvider?.GetMetadataReader();
            }
        }


        /// <inheritdoc/>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Dispose should not throw... right?")]
        public void Dispose()
        {
            if (_pEReader is { })
            {
                _pEReader.Dispose();
                _pEReader = null;
            }

            if (_metadataReaderProvider is { })
            {
                _metadataReaderProvider.Dispose();
                _metadataReaderProvider = null;
            }

            try
            {
                // Remarks in docs says to dispose in a try/catch block:
                // https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=netcore-3.1#remarks
                if (_dllFileReader is { })
                {
                    _dllFileReader.Dispose();
                    _dllFileReader = null;
                }

                if (_pdbFileReader is { })
                {
                    _pdbFileReader.Dispose();
                    _pdbFileReader = null;
                }
            }
            catch
            { }
        }
    }
}
