# Source File Finder
Allows you to find the source file for a type at runtime, if that file is available on the local file system.

```csharp
var target = typeof(MyType);

using var finder = new SourceFileFinder(target.Assembly);

IReadOnlyList<string> fileNames = finder.Find(target);
```

## Try it
Download from nuget: https://www.nuget.org/packages/SourceFileFinder/

## Limitations / Issues
Finding files for assemblies compiled with following flags is currently not possible, as they produce PDBs in the Windows format:
- `<DebugType>none</DebugType>`
- `<DebugType>full</DebugType>`
- `<DebugType>pdbonly</DebugType>`

`<DebugType>portable</DebugType>`, the default for new projects, and `<DebugType>embedded</DebugType>` works.

## Contributors
Big thanks to @jnm2 and @webczat in their help prototyping this library.
