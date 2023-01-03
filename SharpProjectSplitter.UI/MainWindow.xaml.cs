
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
            var task = csprojOrFolder ? this.viewModel.LoadCsProj() : this.viewModel.LoadFolder();
            WithLoadingCtrl(task);
            bool loaded = await task;
            if (!loaded)
                LoadProjCtrl.Visibility = Visibility.Visible;
        }


        public ICommand OnProjectClicked
        {
            get { return new OnClickedCommandImpl(this, false); }
        }

        public ICommand OnProjectRightClicked
        {
            get { return new OnClickedCommandImpl(this, isRightClick: true); }
        }

        public ICommand OnEdgeClicked
        {
            get { return new OnClickedCommandImpl(this, false); }
        }

        public class OnClickedCommandImpl : ICommand
        {
            private MainWindow m_window;

            public bool IsRightClick { get; }

            public OnClickedCommandImpl(MainWindow window, bool isRightClick)
            {
                this.m_window = window;
                IsRightClick = isRightClick;
            }

            public void Execute(object parameter)
            {
                switch (parameter)
                {
                    case ProjectView projectView:
                        m_window.WithLoadingCtrl(IsRightClick ?
                            m_window.viewModel.ReloadOnlyOneProject(projectView) :
                            m_window.viewModel.SplitProject(projectView)
                            );
                        break;
                    case EdgeViewModel edgeView:
                        m_window.viewModel.SelectEdge(edgeView.Edge as EdgeView); break;
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
            LoadProjCtrl.Visibility = Visibility.Collapsed;
            LoadCsprojBtn.Visibility = Visibility.Collapsed;
            LoadFolderBtn.Visibility = Visibility.Collapsed;
            LoadingCtrl.Visibility = Visibility.Visible;
            await task;
            LoadingCtrl.Visibility = Visibility.Collapsed;
            LoadCsprojBtn.Visibility = Visibility.Visible;
            LoadFolderBtn.Visibility = Visibility.Visible;
        }
    }
}
