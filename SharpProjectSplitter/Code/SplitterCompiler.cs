using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SharpProjectSplitter
{
    public class SplitterCompiler
    {
        public static SplitterCompiler Instance;
        private static readonly IEnumerable<string> DefaultNamespaces =
            new[]
            {
            "System",
            "System.IO",
            "System.Net",
            "System.Linq",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Collections.Generic"
            };

        private static string runtimePath = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.1\{0}.dll";

        private static readonly IEnumerable<MetadataReference> DefaultReferences =
            new[]
            {
            MetadataReference.CreateFromFile(string.Format(runtimePath, "mscorlib")),
            MetadataReference.CreateFromFile(string.Format(runtimePath, "System")),
            MetadataReference.CreateFromFile(string.Format(runtimePath, "System.Core"))
            };

        private static readonly CSharpCompilationOptions DefaultCompilationOptions =
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOverflowChecks(true).WithOptimizationLevel(OptimizationLevel.Release)
                    .WithUsings(DefaultNamespaces);

        public static SyntaxTree Parse(string text, string filename = "", CSharpParseOptions options = null)
        {
            var stringText = SourceText.From(text, Encoding.UTF8);
            return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
        }

        static IEnumerable<string> GetFilesFromCsproj(string csprojFile)
        {
            var elements = XElement.Load(csprojFile).DescendantNodes().OfType<XElement>().ToArray();
            var compileElements = elements.Where(e => e.Name.LocalName == "Compile").ToArray();
            var paths = compileElements.Select(e => e.Attribute("Include").Value);
            return paths.Where(f => f.EndsWith(".cs"));
        }

        static IEnumerable<string> GetDefinesFromCsproj(string csprojFile)
        {
            var elements = XElement.Load(csprojFile).DescendantNodes().OfType<XElement>().ToArray();
            var compileElements = elements.Where(e => e.Name.LocalName == "DefineConstants").ToArray();
            var defines = compileElements.SelectMany(e => e.Value.Split(';'));
            return defines;
        }

        public static List<FileDependencies> AnalyzeAllFiles(string csprojPath)
        {
            List<FileDependencies> fileRefs = new List<FileDependencies>();
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();

            string workdingDir = System.IO.Path.GetDirectoryName(csprojPath);
            var syntaxTreeOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);
            syntaxTreeOptions.WithPreprocessorSymbols(GetDefinesFromCsproj(csprojPath));
            Parallel.ForEach(GetFilesFromCsproj(csprojPath), (string file) =>
           {
               var source = File.ReadAllText(System.IO.Path.Combine(workdingDir, file));
               var parsedSyntaxTree = Parse(source, file, syntaxTreeOptions);
               lock (syntaxTrees)
                   syntaxTrees.Add(parsedSyntaxTree);
           });


            var compilation
                = CSharpCompilation.Create("Test.dll", syntaxTrees.ToArray(), DefaultReferences, DefaultCompilationOptions);
            
            try
            {
                Parallel.ForEach(compilation.SyntaxTrees, (SyntaxTree st) =>
               {
                   var semantic = compilation.GetSemanticModel(st, true);
                   List<(SyntaxNode, ITypeSymbol)> referencesTypes = new List<(SyntaxNode, ITypeSymbol)>();
                   foreach (var node in st.GetRoot().DescendantNodes())
                   {
                       ITypeSymbol typeSymbol = semantic.GetTypeInfo(node).Type;
                       if (typeSymbol != null)
                       {
                           referencesTypes.Add((node, typeSymbol));
                       }
                   }
                   lock (fileRefs)
                       fileRefs.Add(new FileDependencies(st, semantic, referencesTypes));
               });

                return fileRefs;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        public static SplittedProject Split(List<FileDependencies> m_files, List<string> m_manuallySplittedFolders, object m_deptAccepted)
        {
            throw new NotImplementedException();
        }

        public static SplittedProject Split(List<FileDependencies> fileRefs, List<string> explicitlySplittedFolders)
        {
            var dirGroupedFiles = fileRefs.GroupBy(f => f.DirectoryPath).ToDictionary(g => g.Key, g => g.ToArray());
            SplittedProject splittedProject = new SplittedProject(dirGroupedFiles, explicitlySplittedFolders);
            splittedProject.AnalyzeAndSplit();
            return splittedProject;
        }
    }
}
