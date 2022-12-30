
namespace SharpProjectSplitter.UI
{
    using Graphviz4Net.Graphs;
    using Graphviz4Net.WPF.ViewModels;
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    public partial class MainWindow : Window
    {
        private MainWindowViewModel viewModel;

        public MainWindow()
        {
            this.viewModel = new MainWindowViewModel();
            this.DataContext = viewModel;
            InitializeComponent();
            viewModel.CodeViewer = CodeViewer;
            LoadingCtrl.Visibility = Visibility.Hidden;
        }

        private async void LoadCsProj_Click(object sender, RoutedEventArgs e)
        {
            Load(true);
        }

        private async void LoadFolderContent_Click(object sender, RoutedEventArgs e)
        {
            Load(false);
        }

        private async void Load(bool csprojOrFolder)
        {
            LoadProjCtrl.Visibility = Visibility.Collapsed;
            LoadCsprojBtn.Visibility = Visibility.Collapsed;
            LoadFolderBtn.Visibility = Visibility.Collapsed;
            var task = csprojOrFolder ? this.viewModel.LoadCsProj() : this.viewModel.LoadFolder();
            WithLoadingCtrl(task);
            bool loaded = await task;

            if(!loaded)
                LoadProjCtrl.Visibility = Visibility.Visible;
            LoadCsprojBtn.Visibility = Visibility.Visible;
            LoadFolderBtn.Visibility = Visibility.Visible;
            
        }


        public ICommand OnProjectClicked
        {
            get { return new OnClickedCommandImpl(this); }
        }

        public ICommand OnEdgeClicked
        {
            get { return new OnClickedCommandImpl(this); }
        }

        public ICommand OnFileRefClicked => new OnClickedCommandImpl(this);


        public class OnClickedCommandImpl : ICommand
        {
            private MainWindow m_window;
            private static PropertyInfo GetSubProp = typeof(System.Windows.Input.CanExecuteChangedEventManager).GetNestedType("HandlerSink", BindingFlags.NonPublic)?.GetProperty("Handler");
            public OnClickedCommandImpl(MainWindow window)
            {
                this.m_window = window;
            }

            public void Execute(object parameter)
            {
                var target = (GetSubProp.GetValue(CanExecuteChanged.Target) as EventHandler<EventArgs>)?.Target;
                if(target is Button button)
                {
                    switch(button.DataContext)
                    {
                        case ProjectView projectView:
                            m_window.WithLoadingCtrl( m_window.viewModel.SplitProject(projectView)); break;
                        case EdgeViewModel edgeView:
                            m_window.viewModel.SelectEdge(edgeView.Edge as EdgeView); break;
                        case InnerFileRef fileRef:
                            m_window.viewModel.ViewFileRefCode(fileRef); break;
                    }
                }

            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            viewModel.ViewFileRefCode((sender as TreeViewItem).DataContext as InnerFileRef); 
        }


        public async void WithLoadingCtrl(Task task)
        {
            LoadingCtrl.Visibility = Visibility.Visible;
            await task;
            LoadingCtrl.Visibility = Visibility.Collapsed;
        }
    }
}
