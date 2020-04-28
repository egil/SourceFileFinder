using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
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
        private bool _isDisposed = false;
        private FileStream? _fileReader = null;
        private PEReader? _pEReader = null;
        private MetadataReaderProvider? _metadataReaderProvider = null;
        private MetadataReader? _metadataReader = null;
        private MetadataReader? _pdbReader = null;

        protected MetadataReader MetadataReader
        {
            get
            {
                if (_metadataReader is null)
                    Initialized();

                return _metadataReader!; // BANG! because Initialized() should set _metadateReader.
            }
        }
        protected MetadataReader PdbReader
        {
            get
            {
                if (_pdbReader is null)
                    Initialized();

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
        public IReadOnlyList<FileInfo> Find(Type target)
        {
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (target.Assembly != SearchAssembly)
                throw new InvalidOperationException($"The type '{target.FullName}' does not belong to finder's search assembly '{SearchAssembly.FullName}'.");

            var result = new List<FileInfo>();

            GetByMethods(target, result);

            return result;
        }

        private void GetByMethods(Type target, List<FileInfo> result)
        {
            var typeDefinitionHandle = (TypeDefinitionHandle)MetadataTokens.Handle(target.GetMetadataToken());
            var typeDefinition = MetadataReader.GetTypeDefinition(typeDefinitionHandle);

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

                if (File.Exists(filename) && !result.Any(x => x.FullName == filename))
                {
                    result.Add(new FileInfo(filename));
                }
            }
        }

        private void Initialized()
        {
            if (_fileReader is null)
                _fileReader = File.OpenRead(SearchAssembly.Location);

            if (_pEReader is null)
                _pEReader = new PEReader(_fileReader);

            if (_metadataReader is null && _pdbReader is null)
            {
                var pdbFound = _pEReader.TryOpenAssociatedPortablePdb(SearchAssembly.Location,
                    pdb => File.OpenRead(pdb),
                    out var metadataReaderProvider,
                    out var pdbPath);

                if (!pdbFound)
                    throw new InvalidOperationException($"No portable PDB file was found for the assembly '{SearchAssembly.FullName}' with path '{SearchAssembly.Location}'");

                _metadataReaderProvider = metadataReaderProvider;
                _metadataReader = _pEReader.GetMetadataReader();
                _pdbReader = _metadataReaderProvider.GetMetadataReader();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Override this to dispose additional resources when deriving from this class.
        /// </summary>
        /// <param name="disposing">True to dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // TODO: Remarks in docs says to dispose in a try/catch block,
                    // but what exception should catch and what do do?
                    // https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=netcore-3.1#remarks
                    if (_fileReader is { })
                        _fileReader.Dispose();

                    if (_pEReader is { })
                        _pEReader.Dispose();

                    if (_metadataReaderProvider is { })
                        _metadataReaderProvider.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}
