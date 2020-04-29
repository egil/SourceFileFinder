# Source File Finder
Allows you to find the source file for a type at runtime, if that file is available on the local file system.

```csharp
var target = typeof(MyType);

using var finder = new SourceFileFinder(target.Assembly);

IReadOnlyList<string> fileNames = finder.Find(target);
```

Big thanks to @jnm2 and @webczat in their help prototyping this library.
