using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReflectionHelpers
{
    internal class CSharpTypeLocator
    {
        private readonly Dictionary<string, CompilationUnitSyntax> _syntaxTreeCache = new Dictionary<string, CompilationUnitSyntax>();

        public bool CsharpDocumentContainsType(string filename, Type target)
        {
            if (!File.Exists(filename))
                return false;

            var root = GetCombilationRoot(filename);

            var classDeclarations = root.DescendantNodesAndSelf(descendIntoTrivia: false)
                .Where(x => x.IsKind(SyntaxKind.ClassDeclaration))
                .OfType<ClassDeclarationSyntax>()
                .Where(x => x.Identifier.Text.Equals(target.Name, StringComparison.Ordinal))
                .ToList();

            if (target.Namespace is null)
                return classDeclarations.Any();
            else
                return classDeclarations.Any(classDeclaration =>
                {
                    var fullname = GetFullName(classDeclaration);
                    return fullname.Equals(target.FullName, StringComparison.Ordinal);
                });
        }

        private CompilationUnitSyntax GetCombilationRoot(string filename)
        {
            if (_syntaxTreeCache.TryGetValue(filename, out var result))
                return result;
            else
            {
                var programText = File.ReadAllText(filename);
                var tree = CSharpSyntaxTree.ParseText(programText);
                var root = tree.GetCompilationUnitRoot();
                _syntaxTreeCache.Add(filename, root);
                return root;
            }
        }

        private static string GetFullName(ClassDeclarationSyntax source)
        {
            const string NESTED_CLASS_DELIMITER = "+";
            const string NAMESPACE_CLASS_DELIMITER = ".";

            if (source is null)
                throw new ArgumentNullException(nameof(source));

            //var items = new List<string>();
            var result = new StringBuilder();
            result.Append(source.Identifier.ValueText);

            var parent = source.Parent;

            while (parent.IsKind(SyntaxKind.ClassDeclaration))
            {
                if (parent is ClassDeclarationSyntax parentClass)
                {
                    result.Insert(0, NESTED_CLASS_DELIMITER);
                    result.Insert(0, parentClass.Identifier.ValueText);
                }
                parent = parent.Parent;
            }

            while (parent.IsKind(SyntaxKind.NamespaceDeclaration))
            {
                if (parent is NamespaceDeclarationSyntax namespaceDeclaration)
                {
                    result.Insert(0, NAMESPACE_CLASS_DELIMITER);
                    result.Insert(0, namespaceDeclaration.Name.ToString());
                }
                parent = parent.Parent;
            }

            return result.ToString();
        }
    }
}
