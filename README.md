# Source File Finder
A small helper library, that allows you to find the source file of a type at runtime, based on the debug information included in the types assembly through the related portable PDB.

```csharp
var target = typeof(MyType);

using var finder = new SourceFileFinder(target.Assembly);

IEnumerable<string> fileNames = finder.Find(target);
```

## Try it
Download from nuget: https://www.nuget.org/packages/SourceFileFinder/

## Limitations / Issues

This project forces (via an included `.targets` file) the debug settings of consuming projects to be

```
<DebugSymbols>true</DebugSymbols>
<DebugType>embedded</DebugType>
```

This avoid issues with other `DebugType` settings (`none`, `full`, and `pdbonly`) that produce PDBs in the Windows format.


## Contributors
Big thanks to @jnm2 and @webczat in their help prototyping this library.
