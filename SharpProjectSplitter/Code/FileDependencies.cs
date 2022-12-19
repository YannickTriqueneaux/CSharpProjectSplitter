using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace SharpProjectSplitter
{
    public class FileDependencies
    {
        public FileDependencies(SyntaxTree st, SemanticModel semantic, List<(SyntaxNode, ITypeSymbol)> dependingTypes)
        {
            St = st;
            Semantic = semantic;
            ReferencedTypes = dependingTypes.Select(kv => kv.Item2).ToArray();
            FileName = st.FilePath;
            DirectoryPath = System.IO.Path.GetDirectoryName(st.FilePath);

            var deps = dependingTypes.Where(t => !t.Item2.DeclaringSyntaxReferences.IsEmpty && !string.IsNullOrEmpty(t.Item2.Name)).GroupBy(t => t.Item2.Name).ToArray();
            KnownDependenciesTypes = deps.Select(t => t.First().Item2).ToArray();
            KnownDependenciesTypesAndSyntaxNodes = deps;
        }

        public override string ToString()
        {
            return FileName;
        }

        public SyntaxTree St { get; }
        public SemanticModel Semantic { get; }


        public ITypeSymbol[] ReferencedTypes { get; }
        public string FileName { get; }
        public List<string> ReferencesTypeNames { get; }
        public string DirectoryPath { get; }
        public ITypeSymbol[] KnownDependenciesTypes { get; }
        public IGrouping<string, (SyntaxNode, ITypeSymbol)>[] KnownDependenciesTypesAndSyntaxNodes { get; }
        public FileDependencies[] Dependencies { get; private set; }
        public FileDependencies[] References { get; private set; }
        public ITypeSymbol[] DeclaringTypes { get; private set; }

        public void Pass1(Dictionary<string, FileDependencies[]> allFilesReferences)
        {
            var dependencies = new List<FileDependencies>(KnownDependenciesTypes.Length);
            foreach(var refFilePath in KnownDependenciesTypes.SelectMany(t => t.DeclaringSyntaxReferences).Select(decl => decl.SyntaxTree.FilePath).Where(f => f != FileName))
            {
                string refFolder = System.IO.Path.GetDirectoryName(refFilePath);
                FileDependencies refFile = allFilesReferences[refFolder].First(f => f.FileName == refFilePath);
                dependencies.Add(refFile);
            }
            Dependencies = dependencies.ToArray();
        }

        internal void Pass2(Dictionary<string, FileDependencies[]> folders)
        {
            IEnumerable<ITypeSymbol> declaringTypes = Enumerable.Empty<ITypeSymbol>();
            var references = new List<FileDependencies>(32);
            foreach (var folder in folders)
                foreach (var file in folder.Value)
                    if(file != this)
                    {
                        if (file.Dependencies.Any(d => d == this))
                        {
                            declaringTypes = declaringTypes.Concat(file.KnownDependenciesTypes.Where(t => t.DeclaringSyntaxReferences.Any(r => r.SyntaxTree.FilePath == this.FileName)));
                            references.Add(file);
                        }
                    }
            DeclaringTypes = declaringTypes.ToArray();
            References = references.ToArray();
        }

        public Project AssignedProject { get; set; }
    }
}