
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Antlr.Runtime;
using Graphviz4Net.Graphs;
using Graphviz4Net.Dot;
using Graphviz4Net.Dot.AntlrParser;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Forms;

namespace SharpProjectSplitter.UI
{

    public class ProjectView
    {

        public ProjectView(Project project)
        {
            Project = project;
        }

        public Project Project { get; }
        public string Name => string.IsNullOrEmpty(Project.FolderName) ? "<Root>" : Project.FolderName;
        public string DisplayName => $"{Name} ({Project.Files.Length}) -> [{Project.Dependencies.Length}]";
    }

    public class EdgeView : Edge<ProjectView>, INotifyPropertyChanged
    {
        public EdgeView(ProjectView source, ProjectView destination, object destinationArrow = null, object sourceArrow = null, object destinationPort = null, object sourcePort = null, IDictionary<string, string> attributes = null) 
            : base(source, destination, destinationArrow, sourceArrow, destinationPort, sourcePort, attributes)
        {

        }

        private List<FileDependencyView> m_dependencies;

        public event PropertyChangedEventHandler PropertyChanged;

        public List<FileDependencyView> Dependencies
        {
            get
            {
                if(m_dependencies == null)
                    m_dependencies = BuildDependencies();
                return m_dependencies;
            }
        }


        public EdgeView WithLabel()
        {
            Label = Dependencies.Count.ToString();
            return this;
        }

        public SolidColorBrush EdgeColor => IsSelected ? UIColors.SelelectedEdge : UIColors.EdgeColor;

        private List<FileDependencyView> BuildDependencies()
        {
            List<FileDependencyView> result = new List<FileDependencyView>();
            foreach (var file in Source.Project.Files)
                result.AddRange(file.Dependencies.Where(d => d.AssignedProject == Destination.Project).Select(dep => new FileDependencyView(file, dep)));
            
            return result;
        }

        public bool IsExpanded => true;
        public string DestinationName => Source.Name;

        private bool m_selected = false;
        public bool IsSelected
        {
            get => m_selected;
            set
            {
                m_selected = value;
                RaisePropertyChanged(nameof(EdgeColor));
            }
        }

        private void RaisePropertyChanged(string property)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }

    public class FileDependencyView
    {
        public FileDependencyView(FileDependencies file, FileDependencies dep)
        {
            File = file;
            Dep = dep;
        }

        public FileDependencies File { get; }
        public FileDependencies Dep { get; }


        private List<DependencyView> m_dependencies;
        public List<DependencyView> Dependencies
        {
            get
            {
                if (m_dependencies == null)
                    m_dependencies = BuildDependencies();
                return m_dependencies;
            }
        }

        private List<DependencyView> BuildDependencies()
        {
            return File.KnownDependenciesTypesAndSyntaxNodes.Where(g => Dep.DeclaringTypes.Any(t => t.Name == g.Key)).Select(g => new DependencyView(g, File.Semantic)).ToList();
        }
        
        public string DepFileName => File.FileName;


        public bool IsExpanded => false;
    }

    public class DependencyView: INotifyPropertyChanged
    {
        public DependencyView(IGrouping<string, (Microsoft.CodeAnalysis.SyntaxNode, Microsoft.CodeAnalysis.ITypeSymbol)> group, Microsoft.CodeAnalysis.SemanticModel semantic)
        {
            Group = group;
            Semantic = semantic;
        }

        public IGrouping<string, (Microsoft.CodeAnalysis.SyntaxNode, Microsoft.CodeAnalysis.ITypeSymbol)> Group { get; }
        public Microsoft.CodeAnalysis.SemanticModel Semantic { get; }

        private List<InnerFileRef> m_references;

        public event PropertyChangedEventHandler PropertyChanged;

        public List<InnerFileRef> References
        {
            get
            {
                if (m_references == null)
                    m_references = BuildReferences();
                return m_references;
            }
        }

        private List<InnerFileRef> BuildReferences()
        {
            var result = Group.Select(r => new InnerFileRef(r.Item1, r.Item2, Semantic)).ToList();
            Dispatcher.CurrentDispatcher.BeginInvoke((Action)(() => { RaisePropertyChanged(nameof(IsExpanded)); }));
            return result;
        }
        public bool IsExpanded => true;
        public string DependencyTypeName => $"{Group.Key} ({Group.First().Item2.DeclaringSyntaxReferences.First().SyntaxTree.FilePath})";


        private void RaisePropertyChanged(string property)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }

    public class InnerFileRef
    {
        public InnerFileRef(Microsoft.CodeAnalysis.SyntaxNode node, Microsoft.CodeAnalysis.ITypeSymbol type, Microsoft.CodeAnalysis.SemanticModel semantic)
        {
            Node = node;
            Type = type;
            Semantic = semantic;
        }

        public Microsoft.CodeAnalysis.SyntaxNode Node { get; }
        public Microsoft.CodeAnalysis.ITypeSymbol Type { get; }
        public Microsoft.CodeAnalysis.SemanticModel Semantic { get; }

        public int LineNumber => Node.SyntaxTree.GetLineSpan(Node.Span).StartLinePosition.Line;
        public Microsoft.CodeAnalysis.Text.TextSpan Span => Node.Span;
        public string FilePath => Node.SyntaxTree.FilePath;
        public string FileRef => $"{Type.Name} ({LineNumber})";
        public bool IsExpanded => false;
    }

    public class DiamondArrow
    {
    }

    public class Arrow
    {        
    }


    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public static MainWindowViewModel Instance { get; private set; }
        public MainWindowViewModel()
        {
            Instance = this;
            var graph = new Graph<ProjectView>();
            this.Graph = graph;
        }

        public IGraph Graph { get; private set; }

        private LayoutEngine _layoutEngine = LayoutEngine.Dot;
        public LayoutEngine LayoutEngine
        {
            get { return _layoutEngine; }
            set
            {
                _layoutEngine = value;
                this.RaisePropertyChanged("LayoutEngine");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string property)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        string m_lastCsProjFileSelected = null;
        internal async Task<bool> LoadCsProj()
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "csproj files (*.csproj)|*.csproj|All files (*.*)|*.*";
            openFileDialog.FileName = m_lastCsProjFileSelected;

            var dialogResult = openFileDialog.ShowDialog();
            if (dialogResult == DialogResult.OK && !string.IsNullOrEmpty(openFileDialog.FileName))
            {
                m_lastCsProjFileSelected = openFileDialog.FileName;
                await Task.Run(async () =>
                {
                    await SplitProject(m_lastCsProjFileSelected);
                });
                return true;
            }
            return false;
        }

        string m_lastFolderSelected = null;
        internal async Task<bool> LoadFolder()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = m_lastFolderSelected;
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    m_lastFolderSelected = fbd.SelectedPath;
                    await Task.Run(async () =>
                    {
                        await SplitProject(m_lastFolderSelected);
                    });
                    return true;
                }
            }
            return false;
        }

        public ObservableCollection<EdgeView> SelectedEdge { get; } = new ObservableCollection<EdgeView>();
        public Visibility IsEdgeSelectedVisibility => SelectedEdge.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        public CodeViewer CodeViewer { get; internal set; }

        internal void SelectEdge(EdgeView edgeView)
        {
            if (SelectedEdge.Count > 0)
            {
                SelectedEdge[0].IsSelected = false;
                SelectedEdge.Clear();
            }
            SelectedEdge.Add(edgeView);
            edgeView.IsSelected = true;
            RaisePropertyChanged(nameof(SelectedEdge));
            RaisePropertyChanged(nameof(IsEdgeSelectedVisibility));
        }

        private List<string> m_manuallySplittedFolders = new List<string>();
        private List<FileDependencies> m_files;

        private string loadedCsProjOrFolder = null;
        internal Task SplitProject(string csprojFileOrFolder)
        {
            loadedCsProjOrFolder = csprojFileOrFolder;
            m_manuallySplittedFolders = new List<string>();
            return Task.Run(async () =>
            {
                m_files = SplitterCompiler.AnalyzeAllFiles(csprojFileOrFolder);
                await SplitProject();
            });
        }

        internal async Task SplitProject(ProjectView projectView)
        {
            m_manuallySplittedFolders.Add(projectView.Name);
            await Task.Run(async () =>
            {
                await SplitProject();
            });
        }

        internal Task ReloadOnlyOneProject(ProjectView projectView)
        {
            m_manuallySplittedFolders = new List<string>();
            return Task.Run(async () =>
            {
                m_files = SplitterCompiler.AnalyzeAllFiles(loadedCsProjOrFolder, projectView.Project.FolderName);
                await SplitProject();
            });
        }

        internal Task SplitProject()
        {
            SplittedProject splittedProject = SplitterCompiler.Split(m_files, m_manuallySplittedFolders);

            var graph = new Graph<ProjectView>();

            Dictionary<string, ProjectView> projects = new Dictionary<string, ProjectView>();
            foreach (var proj in splittedProject.SplittedProjects)
            {
                var projv = new ProjectView(proj);
                projects.Add(projv.Name, projv);
                graph.AddVertex(projv);
            }

            foreach (var proj in splittedProject.SplittedProjects)
            {
                var projv = projects[string.IsNullOrEmpty(proj.FolderName) ? "<Root>" : proj.FolderName];
                foreach (var dep in proj.Dependencies)
                    graph.AddEdge(new EdgeView(projv, projects[string.IsNullOrEmpty(dep.FolderName) ? "<Root>" : dep.FolderName], new Arrow()).WithLabel());
            }

            this.Graph = graph;
            RaisePropertyChanged(nameof(Graph));
            return Task.CompletedTask;
        }

        public Visibility CodeOpenedVisibility => string.IsNullOrEmpty(CodeViewer?.OpenedFileName) ? Visibility.Visible : Visibility.Collapsed;
        internal void ViewFileRefCode(InnerFileRef fileRef)
        {
            CodeViewer.SetText(fileRef.Node.SyntaxTree, fileRef.Semantic, fileRef.Node);
            RaisePropertyChanged(nameof(CodeOpenedVisibility));
        }
    }
}
