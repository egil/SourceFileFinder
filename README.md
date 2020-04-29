# Source File Finder
Allows you to find the source file for a type at runtime, if that file is available on the local file system.

```csharp
var target = typeof(MyType);

using var finder = new SourceFileFinder(target.Assembly);

IReadOnlyList<string> fileNames = finder.Find(target);
```

## Limitations / Issues
Finding files for assemblies compiled with following flags is currently not possible:
- `<DebugType>none</DebugType>`
- `<DebugType>full</DebugType>`
- `<DebugType>pdbonly</DebugType>`

`<DebugType>portable</DebugType>`, the default for new projects, and `<DebugType>embedded</DebugType>` works.

## Contributors
Big thanks to @jnm2 and @webczat in their help prototyping this library.
